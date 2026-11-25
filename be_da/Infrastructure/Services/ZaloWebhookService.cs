using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace be_da.Infrastructure.Services;

public sealed class ZaloWebhookService(ZaloWebhookVerifier verifier,SqlZaloStore store,IOptions<ZaloOptions> options,TimeProvider clock) : IZaloWebhookService
{
    public async Task<ZaloReceiveResult> ReceiveAsync(string body,string signature,CancellationToken ct)
    {
        if(!options.Value.Enabled) return ZaloReceiveResult.Disabled;
        var message=verifier.VerifyText(body,signature);
        if(message is null) return ZaloReceiveResult.InvalidSignature;
        var now=clock.GetUtcNow().ToUnixTimeMilliseconds();
        if(message.Timestamp<now-TimeSpan.FromHours(24).TotalMilliseconds || message.Timestamp>now+TimeSpan.FromMinutes(5).TotalMilliseconds)
            return ZaloReceiveResult.Stale;
        try {await store.EnqueueAsync(message,ct);}
        catch(SqlException ex) when(ex.Number==51001) {return ZaloReceiveResult.RateLimited;}
        return ZaloReceiveResult.Accepted;
    }
    public async Task<ZaloRuntimeStatus> StatusAsync(CancellationToken ct)
    {
        await using var c=await store.OpenAsync(ct);
        await using var cmd=SqlZaloStore.Command(c,"""
            SELECT 'inbox',state,COUNT(*) FROM dbo.zalo_inbox WHERE oa_id=@oa GROUP BY state
            UNION ALL SELECT 'outbox',b.state,COUNT(*) FROM dbo.zalo_outbox b JOIN dbo.zalo_customers c ON c.id=b.customer_id WHERE c.oa_id=@oa GROUP BY b.state
            """,("@oa",options.Value.OaId));
        var counts=new List<ZaloQueueCount>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) counts.Add(new(r.GetString(0),r.GetString(1),r.GetInt32(2)));
        return new(options.Value.Enabled,options.Value.WorkerEnabled,counts);
    }
}
