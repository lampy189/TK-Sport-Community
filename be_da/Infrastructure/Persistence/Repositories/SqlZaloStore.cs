using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be_da.Application.DTOs;
using Microsoft.Data.SqlClient;

namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class SqlZaloStore(IConfiguration config)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var c = new SqlConnection(config.GetConnectionString("Sep490"));
        try { await SportCenterConnection.OpenAsync(c, ct); return c; }
        catch { await c.DisposeAsync(); throw; }
    }
    public static SqlCommand Command(SqlConnection c, string sql, params (string, object?)[] values)
    {
        var cmd = new SqlCommand(sql, c) { CommandTimeout = 30 };
        foreach (var (key, value) in values) cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
        return cmd;
    }
    public async Task EnqueueAsync(ZaloInboundText message, CancellationToken ct)
    {
        await using var c = await OpenAsync(ct);
        await using var cmd = Command(c, """
            SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; BEGIN TRAN;
            IF EXISTS(SELECT 1 FROM dbo.zalo_inbox WITH(UPDLOCK,HOLDLOCK) WHERE oa_id=@oa AND message_id=@msg)
            BEGIN COMMIT; RETURN; END;
            IF (SELECT COUNT(*) FROM dbo.zalo_inbox WHERE oa_id=@oa AND sender_id=@sender AND received_at>DATEADD(minute,-1,SYSDATETIMEOFFSET()))>=12
                THROW 51001,'Zalo sender rate limit',1;
            IF (SELECT COUNT(*) FROM dbo.zalo_inbox WHERE state IN ('pending','processing'))>=1000
                THROW 51001,'Zalo queue full',1;
            IF NOT EXISTS(SELECT 1 FROM dbo.zalo_customers WITH(UPDLOCK,HOLDLOCK) WHERE oa_id=@oa AND sender_id=@sender)
                INSERT dbo.zalo_customers(oa_id,sender_id) VALUES(@oa,@sender);
            INSERT dbo.zalo_inbox(oa_id,sender_id,message_id,message_text,event_timestamp)
                VALUES(@oa,@sender,@msg,@text,@time);
            COMMIT;
            """, ("@oa",message.OaId),("@sender",message.SenderId),("@msg",message.MessageId),("@text",message.Text),("@time",message.Timestamp));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<ZaloWork?> NextAsync(SqlConnection c, string oa, CancellationToken ct)
    {
        await using var cmd = Command(c, """
            SELECT TOP(1) i.id,c.id,i.sender_id,i.message_id,i.message_text,i.received_at
            FROM dbo.zalo_inbox i JOIN dbo.zalo_customers c ON c.oa_id=i.oa_id AND c.sender_id=i.sender_id
            WHERE i.oa_id=@oa AND (i.state='pending' OR (i.state='processing' AND i.lease_until<SYSDATETIMEOFFSET()))
                AND i.attempts<5
                AND NOT EXISTS(SELECT 1 FROM dbo.zalo_inbox earlier WHERE earlier.oa_id=i.oa_id AND earlier.sender_id=i.sender_id
                    AND earlier.id<i.id AND earlier.state IN ('pending','processing'))
            ORDER BY i.id;
            """,("@oa",oa));
        ZaloWork? work;
        await using (var r = await cmd.ExecuteReaderAsync(ct))
            work = await r.ReadAsync(ct) ? new(r.GetInt64(0),r.GetInt64(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetFieldValue<DateTimeOffset>(5)) : null;
        if (work is null) return null;
        await using var claim = Command(c,"UPDATE dbo.zalo_inbox SET state='processing',attempts=attempts+1,lease_until=DATEADD(minute,3,SYSDATETIMEOFFSET()) WHERE id=@id",("@id",work.Id));
        await claim.ExecuteNonQueryAsync(ct);
        return work;
    }
    public async Task<ZaloConversation> StateAsync(SqlConnection c, long customer, CancellationToken ct)
    {
        await using var cmd = Command(c,"SELECT state_json FROM dbo.zalo_conversations WHERE customer_id=@id AND expires_at>SYSDATETIMEOFFSET()",("@id",customer));
        var value = await cmd.ExecuteScalarAsync(ct) as string;
        return value is null ? new() : JsonSerializer.Deserialize<ZaloConversation>(value) ?? new();
    }
    public async Task CompleteAsync(SqlConnection c, ZaloWork work, ZaloConversation state, ZaloOutgoing reply, CancellationToken ct)
    {
        state.LastReply = reply.Text;
        await using var cmd = Command(c,"""
            SET XACT_ABORT ON; BEGIN TRAN;
            UPDATE dbo.zalo_conversations SET state_json=@json,expires_at=DATEADD(hour,24,SYSDATETIMEOFFSET()),updated_at=SYSDATETIMEOFFSET() WHERE customer_id=@customer;
            IF @@ROWCOUNT=0 INSERT dbo.zalo_conversations(customer_id,state_json,expires_at) VALUES(@customer,@json,DATEADD(hour,24,SYSDATETIMEOFFSET()));
            IF NOT EXISTS(SELECT 1 FROM dbo.zalo_outbox WHERE customer_id=@customer AND deduplication_key=@key)
                INSERT dbo.zalo_outbox(customer_id,deduplication_key,order_id,payload_json,expires_at) VALUES(@customer,@key,@order,@payload,DATEADD(hour,47,@received));
            UPDATE dbo.zalo_inbox SET state='completed',lease_until=NULL WHERE id=@id;
            COMMIT;
            """,("@json",JsonSerializer.Serialize(state)),("@customer",work.CustomerId),("@key",$"inbox:{work.Id}"),
            ("@order",reply.OrderId),("@payload",JsonSerializer.Serialize(reply)),("@id",work.Id),("@received",work.ReceivedAt));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<long?> UserAsync(SqlConnection c, long customer, CancellationToken ct)
    {
        await using var cmd = Command(c,"SELECT user_id FROM dbo.zalo_customers WHERE id=@id",("@id",customer));
        var value = await cmd.ExecuteScalarAsync(ct); return value is null or DBNull ? null : Convert.ToInt64(value);
    }
    public async Task<long?> ExistingOrderAsync(SqlConnection c, long customer, string? message, CancellationToken ct)
    {
        await using var cmd = Command(c,"""
            SELECT TOP(1) z.order_id FROM dbo.zalo_booking_orders z JOIN dbo.social_booking_orders o ON o.id=z.order_id
            WHERE z.customer_id=@id AND (@msg IS NULL OR z.confirmation_message_id=@msg) ORDER BY z.order_id DESC
            """,("@id",customer),("@msg",message));
        var value = await cmd.ExecuteScalarAsync(ct); return value is null or DBNull ? null : Convert.ToInt64(value);
    }
    // Only provision a channel identity after explicit booking confirmation. No website login is issued.
    public async Task<long> EnsurePlayerAsync(SqlConnection c, ZaloWork work, string name, long sport, int score, CancellationToken ct)
    {
        if (score is <0 or >100 || string.IsNullOrWhiteSpace(name) || name.Length>100) throw new ArgumentException("Hồ sơ tự khai không hợp lệ.");
        var alias = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"zalo:{work.CustomerId}"))).ToLowerInvariant()+"@zalo.invalid";
        await using var cmd = Command(c,"""
            SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; BEGIN TRAN;
            DECLARE @user bigint=(SELECT user_id FROM dbo.zalo_customers WITH(UPDLOCK,HOLDLOCK) WHERE id=@customer);
            IF @user IS NULL BEGIN
                DECLARE @role bigint=(SELECT id FROM dbo.roles WHERE LOWER(code)='player');
                IF @role IS NULL THROW 51002,'Player role missing',1;
                INSERT dbo.users(role_id,email,full_name,status) VALUES(@role,@alias,@name,'Active');
                SET @user=SCOPE_IDENTITY();
                INSERT dbo.players(user_id) VALUES(@user);
                UPDATE dbo.zalo_customers SET user_id=@user,display_name=@name WHERE id=@customer;
            END;
            IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE id=@user AND LOWER(status)='active' AND deleted_at IS NULL)
                THROW 51002,'Channel identity unavailable',1;
            DECLARE @player bigint=(SELECT id FROM dbo.players WHERE user_id=@user);
            IF NOT EXISTS(SELECT 1 FROM dbo.player_sport_profiles WITH(UPDLOCK,HOLDLOCK) WHERE player_id=@player AND sport_id=@sport)
                INSERT dbo.player_sport_profiles(player_id,sport_id,current_score,current_band,rating_status)
                    VALUES(@player,@sport,@score,@band,'ZaloSelfDeclared');
            IF NOT EXISTS(SELECT 1 FROM dbo.zalo_skill_declarations WHERE customer_id=@customer AND sport_id=@sport AND source_message_id=@msg)
                INSERT dbo.zalo_skill_declarations(customer_id,sport_id,declared_level,mapped_band,source_message_id)
                    VALUES(@customer,@sport,@declaration,@band,@msg);
            COMMIT; SELECT @user;
            """,("@customer",work.CustomerId),("@alias",alias),("@name",name),("@sport",sport),
            ("@score",score),("@band",Band(score)),("@declaration",$"Tự khai {score}/100; không phải đánh giá trọng tài"),("@msg",work.MessageId));
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
    }
    public static string Band(int score) => score switch { <20=>"B1",<40=>"B2",<60=>"B3",<80=>"B4",_=>"B5" };
}
