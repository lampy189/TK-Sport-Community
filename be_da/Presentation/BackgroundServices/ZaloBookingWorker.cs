using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Infrastructure.Services;
using be_da.Infrastructure.ExternalServices;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using TKSC.Application.Interfaces.Services;

namespace be_da.Presentation.BackgroundServices;

public sealed class ZaloBookingWorker(IServiceScopeFactory scopes,IOptions<ZaloOptions> options,ILogger<ZaloBookingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if(!options.Value.Enabled || !options.Value.WorkerEnabled) return;
        while(!stoppingToken.IsCancellationRequested)
        {
            try { await TickAsync(stoppingToken); }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested) {break;}
            catch(Exception ex) {logger.LogWarning("Zalo worker chưa hoàn thành vòng xử lý ({Type}).",ex.GetType().Name);}
            try {await Task.Delay(TimeSpan.FromSeconds(3),stoppingToken);} catch(OperationCanceledException){break;}
        }
    }
    public async Task TickAsync(CancellationToken ct)
    {
        using var scope=scopes.CreateScope();
        var store=scope.ServiceProvider.GetRequiredService<SqlZaloStore>();
        await using var c=await store.OpenAsync(ct);
        // One SQL-owned runner per OA, across all application instances. Connection disposal releases the lock.
        await using var gate=SqlZaloStore.Command(c,"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Session',@LockTimeout=0; SELECT @r",("@key","TKSC:ZaloRunner:"+options.Value.OaId));
        if(Convert.ToInt32(await gate.ExecuteScalarAsync(ct))<0) return;
        try
        {
            var work=await store.NextAsync(c,options.Value.OaId,ct);
            if(work is not null)
            {
                try { await scope.ServiceProvider.GetRequiredService<IZaloBookingProcessor>().ProcessAsync(work,ct); }
                catch(OperationCanceledException) when(ct.IsCancellationRequested){throw;}
                catch(Exception ex)
                {
                    logger.LogWarning("Zalo inbox {Id} chưa xử lý được ({Type}).",work.Id,ex.GetType().Name);
                    await using var retry=SqlZaloStore.Command(c,"UPDATE dbo.zalo_inbox SET state=CASE WHEN attempts>=5 THEN 'failed' ELSE 'processing' END,lease_until=DATEADD(second,30,SYSDATETIMEOFFSET()) WHERE id=@id",("@id",work.Id));
                    await retry.ExecuteNonQueryAsync(ct);
                    await using var failed=SqlZaloStore.Command(c,"SELECT attempts FROM dbo.zalo_inbox WHERE id=@id",("@id",work.Id));
                    if(Convert.ToInt32(await failed.ExecuteScalarAsync(ct))>=5)
                        await store.CompleteAsync(c,work,await store.StateAsync(c,work.CustomerId,ct),new("Hệ thống chưa xử lý được yêu cầu này. Bạn hãy nhắn nhân viên sân để kiểm tra; nếu đã chuyển tiền thì không chuyển thêm."),ct);
                }
            }
            var payments=scope.ServiceProvider.GetRequiredService<IPaymentService>();
            await QueueStatusesAsync(c,payments,ct);
            await SendAsync(c,scope.ServiceProvider.GetRequiredService<ZaloOaClient>(),payments,ct);
        }
        finally
        {
            // Closing a pooled SqlConnection alone does not document release of session application locks.
            await using var release=SqlZaloStore.Command(c,"EXEC sys.sp_releaseapplock @Resource=@key,@LockOwner='Session'",("@key","TKSC:ZaloRunner:"+options.Value.OaId));
            await release.ExecuteNonQueryAsync(CancellationToken.None);
        }
    }
    private async Task QueueStatusesAsync(SqlConnection c,IPaymentService payments,CancellationToken ct)
    {
        var items=new List<(long Customer,long User,long Order,string State,DateTimeOffset? LastInbound)>();
        await using(var cmd=SqlZaloStore.Command(c,"""
            SELECT TOP(20) z.customer_id,c.user_id,o.id,
              CASE WHEN LOWER(o.status)='pending' THEN 'expired' ELSE LOWER(o.status) END AS state,
              (SELECT MAX(i.received_at) FROM dbo.zalo_inbox i WHERE i.oa_id=c.oa_id AND i.sender_id=c.sender_id)
            FROM dbo.zalo_booking_orders z JOIN dbo.zalo_customers c ON c.id=z.customer_id
            JOIN dbo.social_booking_orders o ON o.id=z.order_id
            WHERE c.oa_id=@oa AND c.user_id IS NOT NULL
              AND (LOWER(o.status)<>'pending' OR o.expires_at<=SYSDATETIMEOFFSET())
              AND NOT EXISTS(SELECT 1 FROM dbo.zalo_outbox b WHERE b.customer_id=c.id
                AND b.deduplication_key=CONCAT('status:',o.id,':',CASE WHEN LOWER(o.status)='pending' THEN 'expired' ELSE LOWER(o.status) END))
            ORDER BY o.id;
            """,("@oa",options.Value.OaId)))
        {
            await using var r=await cmd.ExecuteReaderAsync(ct);
            while(await r.ReadAsync(ct)) items.Add((r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetString(3),r.IsDBNull(4)?null:r.GetFieldValue<DateTimeOffset>(4)));
        }
        foreach(var item in items)
        {
            var order=await payments.DisplayAsync(item.Order,item.User);
            var payload=new ZaloOutgoing(ZaloBookingService.StatusText(order),order.Id);
            await using var cmd=SqlZaloStore.Command(c,"""
                IF NOT EXISTS(SELECT 1 FROM dbo.zalo_outbox WHERE customer_id=@customer AND deduplication_key=@key)
                  INSERT dbo.zalo_outbox(customer_id,deduplication_key,order_id,payload_json,expires_at)
                    VALUES(@customer,@key,@order,@payload,@expiry)
                """,("@customer",item.Customer),("@key",$"status:{item.Order}:{item.State}"),("@order",item.Order),
                ("@payload",JsonSerializer.Serialize(payload)),("@expiry",item.LastInbound?.AddHours(47)??DateTimeOffset.UtcNow));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
    private async Task SendAsync(SqlConnection c,ZaloOaClient client,IPaymentService payments,CancellationToken ct)
    {
        // A previous crash after dispatch is an unknown outcome, never an automatic duplicate send.
        await using(var cleanup=SqlZaloStore.Command(c,"""
            UPDATE b SET state='unknown' FROM dbo.zalo_outbox b JOIN dbo.zalo_customers c ON c.id=b.customer_id
              WHERE c.oa_id=@oa AND b.state='sending';
            UPDATE b SET state='expired' FROM dbo.zalo_outbox b JOIN dbo.zalo_customers c ON c.id=b.customer_id
              WHERE c.oa_id=@oa AND b.state='pending' AND b.expires_at<=SYSDATETIMEOFFSET();
            """,("@oa",options.Value.OaId))) await cleanup.ExecuteNonQueryAsync(ct);
        var messages=new List<(long Id,string Sender,long? User,ZaloOutgoing Payload)>();
        await using(var cmd=SqlZaloStore.Command(c,"""
            SELECT TOP(10) b.id,c.sender_id,c.user_id,b.payload_json
              FROM dbo.zalo_outbox b JOIN dbo.zalo_customers c ON c.id=b.customer_id
              WHERE c.oa_id=@oa AND b.state='pending' ORDER BY b.id
            """,("@oa",options.Value.OaId)))
        {
            await using var r=await cmd.ExecuteReaderAsync(ct);
            while(await r.ReadAsync(ct)) messages.Add((r.GetInt64(0),r.GetString(1),r.IsDBNull(2)?null:r.GetInt64(2),JsonSerializer.Deserialize<ZaloOutgoing>(r.GetString(3))!));
        }
        foreach(var item in messages)
        {
            if(item.Payload.Checkout)
            {
                var order=await payments.DisplayAsync(item.Payload.OrderId!.Value,item.User!.Value);
                if(!order.Status.Equals("pending",StringComparison.OrdinalIgnoreCase) || order.ExpiresAt<=DateTimeOffset.UtcNow)
                {await MarkAsync(c,item.Id,"expired",null,ct);continue;}
            }
            string? attachment=null;
            // Upload does not send a message: an upload failure is safe to retry on the next tick.
            try {if(item.Payload.QrCode is not null) attachment=await client.UploadQrAsync(item.Payload.QrCode,ct);}
            catch(Exception ex) when(ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {logger.LogWarning("Zalo QR {Id} upload thất bại ({Type}).",item.Id,ex.GetType().Name);await MarkAsync(c,item.Id,"failed",null,ct);continue;}
            await MarkAsync(c,item.Id,"sending",null,ct);
            try
            {
                var id=attachment is null ? await client.SendTextAsync(item.Sender,item.Payload.Text,ct)
                    : await client.SendImageAsync(item.Sender,item.Payload.Text,attachment,ct);
                await MarkAsync(c,item.Id,"sent",id,ct);
            }
            catch(Exception ex) when(ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                logger.LogWarning("Zalo outbox {Id} chưa xác định kết quả gửi ({Type}).",item.Id,ex.GetType().Name);
                await MarkAsync(c,item.Id,"unknown",null,ct);
            }
        }
    }
    private static async Task MarkAsync(SqlConnection c,long id,string state,string? provider,CancellationToken ct)
    {
        await using var cmd=SqlZaloStore.Command(c,"UPDATE dbo.zalo_outbox SET state=@state,provider_message_id=COALESCE(@provider,provider_message_id),attempts=attempts+CASE WHEN @state='sending' THEN 1 ELSE 0 END WHERE id=@id",("@state",state),("@provider",provider),("@id",id));
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
