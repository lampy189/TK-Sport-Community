using System.Data;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.External;
using Microsoft.Data.SqlClient;
namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class InvitationRepository(IConfiguration configuration, IDataAccessContext scope) : IInvitationRepository
{
    private async Task<SqlConnection> Open(CancellationToken ct)
    {
        var c = new SqlConnection(configuration.GetConnectionString("Sep490"));
        try { await SportCenterConnection.OpenAsync(c, ct, scope); return c; }
        catch { await c.DisposeAsync(); throw; }
    }
    public async Task<IReadOnlyList<SessionInvitation>> ListAsync(long userId, CancellationToken ct)
    {
        await using var c = await Open(ct);
        await WorkflowSql.RequireInvitations(c, null, ct);
        await using var cmd = WorkflowSql.Command(c, null, """
            SELECT i.id,s.id,v.id,v.name,cr.name,s.session_date,ts.start_time,sp.name,COALESCE(t.amount_vnd,s.price_vnd),
              iu.id,iu.full_name,eu.id,eu.full_name,COALESCE(i.payer_user_id,eu.id),
              CASE WHEN LOWER(i.status)='pending' AND (i.expires_at IS NULL OR i.expires_at<=SYSDATETIMEOFFSET() OR LOWER(s.status) NOT IN ('open','published') OR v.is_active<>1 OR cr.is_active<>1) THEN 'expired' ELSE LOWER(i.status) END,
              COALESCE(i.expires_at,TODATETIMEOFFSET(CAST(s.session_date AS datetime2),'+07:00')),i.order_id,i.ticket_id,o.status,o.expires_at
            FROM dbo.invitations i JOIN dbo.players ip ON ip.id=i.inviter_id JOIN dbo.users iu ON iu.id=ip.user_id
            JOIN dbo.players ep ON ep.id=i.invitee_id JOIN dbo.users eu ON eu.id=ep.user_id
            JOIN dbo.sessions s ON s.id=i.session_id JOIN dbo.courts cr ON cr.id=s.court_id JOIN dbo.venues v ON v.id=cr.venue_id
            JOIN dbo.time_slots ts ON ts.id=s.time_slot_id JOIN dbo.sports sp ON sp.id=s.sport_id LEFT JOIN dbo.social_booking_orders o ON o.id=i.order_id LEFT JOIN dbo.tickets t ON t.id=i.ticket_id
            WHERE iu.id=@user OR eu.id=@user ORDER BY i.id DESC;
            """, ("@user", userId));
        var rows = new List<SessionInvitation>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetString(3),r.GetString(4),
            DateOnly.FromDateTime(r.GetDateTime(5)),TimeOnly.FromTimeSpan(r.GetTimeSpan(6)),r.GetString(7),r.GetInt32(8),
            r.GetInt64(9),r.GetString(10),r.GetInt64(11),r.GetString(12),r.GetInt64(13),r.IsDBNull(14)?"expired":r.GetString(14),
            r.GetFieldValue<DateTimeOffset>(15),r.IsDBNull(16)?null:r.GetInt64(16),r.IsDBNull(17)?null:r.GetInt64(17),
            r.IsDBNull(18)?null:r.GetString(18),r.IsDBNull(19)?null:r.GetFieldValue<DateTimeOffset>(19)));
        return rows;
    }
    public async Task<IReadOnlyList<InvitationCandidate>> CandidatesAsync(long userId, long sessionId, CancellationToken ct)
    {
        await using var c = await Open(ct);
        var visible = await WorkflowSql.Scalar(c,null,"SELECT COUNT(*) FROM dbo.sessions s JOIN dbo.courts cr ON cr.id=s.court_id WHERE s.id=@session AND (@venue IS NULL OR cr.venue_id=@venue)",ct,("@session",sessionId),("@venue",scope.RestrictedVenueId));
        if (visible==0) return [];
        var friends = await WorkflowSql.Rows(c,null,"""
            SELECT p.id,u.full_name FROM dbo.friendships f JOIN dbo.players me ON me.user_id=@user
            JOIN dbo.players p ON p.id=CASE WHEN f.requester_id=me.id THEN f.addressee_id ELSE f.requester_id END
            JOIN dbo.users u ON u.id=p.user_id WHERE (f.requester_id=me.id OR f.addressee_id=me.id) AND LOWER(f.status)='accepted' ORDER BY u.full_name
            """,ct,("@user",userId));
        var result = new List<InvitationCandidate>();
        foreach(var friend in friends)
        {
            var id=Convert.ToInt64(friend["id"]);
            var reason=await WorkflowSql.Eligibility(c,null,sessionId,id,ct);
            result.Add(new(id,(string)friend["full_name"]!,reason is null,reason));
        }
        return result;
    }
    public async Task<OperationResult> SendAsync(long userId, long sessionId, InviteSessionRequest request, CancellationToken ct)
    {
        await using var c = await Open(ct);
        await WorkflowSql.RequireInvitations(c,null,ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var session=await WorkflowSql.Scalar(c,tx,"SELECT s.id FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) JOIN dbo.courts cr ON cr.id=s.court_id WHERE s.id=@id AND (@venue IS NULL OR cr.venue_id=@venue)",ct,("@id",sessionId),("@venue",scope.RestrictedVenueId));
        if(session==0) return OperationResult.Missing(new { message="Không tìm thấy buổi tại cơ sở đang chọn." });
        var inviter=await WorkflowSql.Scalar(c,tx,"SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id=p.user_id WHERE u.id=@user AND u.deleted_at IS NULL AND LOWER(u.status)='active'",ct,("@user",userId));
        var friend=await WorkflowSql.Scalar(c,tx,"SELECT COUNT(*) FROM dbo.friendships WHERE LOWER(status)='accepted' AND ((requester_id=@me AND addressee_id=@friend) OR (addressee_id=@me AND requester_id=@friend))",ct,("@me",inviter),("@friend",request.FriendPlayerId));
        if(inviter==0 || friend==0 || inviter==request.FriendPlayerId) return OperationResult.Denied(new { message="Chỉ được mời bạn bè đã kết nối." });
        var existing=await WorkflowSql.Rows(c,tx,"SELECT TOP(1) id,inviter_id FROM dbo.invitations WITH(UPDLOCK,HOLDLOCK) WHERE session_id=@session AND invitee_id=@friend AND (LOWER(status)='accepted' OR (LOWER(status)='pending' AND expires_at>SYSDATETIMEOFFSET())) ORDER BY id DESC",ct,("@session",sessionId),("@friend",request.FriendPlayerId));
        if(existing.Count>0) return Convert.ToInt64(existing[0]["inviter_id"])==inviter ? OperationResult.Success(new { id=Convert.ToInt64(existing[0]["id"]),existing=true }) : OperationResult.Conflict(new { message="Người chơi đã có lời mời cho buổi này." });
        var reason=await WorkflowSql.Eligibility(c,tx,sessionId,request.FriendPlayerId,ct);
        if(reason is not null) return OperationResult.Invalid(new { message=reason });
        var id=await WorkflowSql.Scalar(c,tx,"""
            DECLARE @start datetimeoffset=(SELECT TODATETIMEOFFSET(DATEADD(second,DATEDIFF(second,CAST('00:00' AS time),ts.start_time),CAST(s.session_date AS datetime2)),'+07:00') FROM dbo.sessions s JOIN dbo.time_slots ts ON ts.id=s.time_slot_id WHERE s.id=@session);
            DECLARE @recipient bigint=(SELECT user_id FROM dbo.players WHERE id=@friend);
            INSERT dbo.invitations(inviter_id,invitee_id,session_id,status,payer_user_id,created_at,expires_at)
            VALUES(@me,@friend,@session,'pending',CASE WHEN @pay=1 THEN @user ELSE @recipient END,SYSDATETIMEOFFSET(),CASE WHEN @start<DATEADD(hour,24,SYSDATETIMEOFFSET()) THEN @start ELSE DATEADD(hour,24,SYSDATETIMEOFFSET()) END);
            DECLARE @id bigint=SCOPE_IDENTITY();
            INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status) VALUES(@recipient,'web',N'Bạn có lời mời tham gia buổi chơi','session_invitation',@id,'unread');
            SELECT @id;
            """,ct,("@session",sessionId),("@me",inviter),("@friend",request.FriendPlayerId),("@user",userId),("@pay",request.PaymentChoice=="inviter"));
        await tx.CommitAsync(ct);
        return OperationResult.Created(new { id });
    }
    public async Task<OperationResult> DeclineAsync(long userId,long id,CancellationToken ct)
    {
        await using var c=await Open(ct);
        await WorkflowSql.RequireInvitations(c,null,ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var rows=await WorkflowSql.Rows(c,tx,"SELECT i.status,i.expires_at FROM dbo.invitations i WITH(UPDLOCK,HOLDLOCK) JOIN dbo.players p ON p.id=i.invitee_id WHERE i.id=@id AND p.user_id=@user",ct,("@id",id),("@user",userId));
        if(rows.Count==0) return OperationResult.Missing(new { message="Không tìm thấy lời mời." });
        if(string.Equals(rows[0]["status"]?.ToString(),"declined",StringComparison.OrdinalIgnoreCase)) return OperationResult.Completed();
        if(!string.Equals(rows[0]["status"]?.ToString(),"pending",StringComparison.OrdinalIgnoreCase) || rows[0]["expires_at"] is not DateTimeOffset expiry || expiry<=DateTimeOffset.UtcNow) return OperationResult.Conflict(new { message="Lời mời đã xử lý hoặc hết hạn." });
        await WorkflowSql.Execute(c,tx,"""
            UPDATE dbo.invitations SET status='declined',responded_at=SYSDATETIMEOFFSET() WHERE id=@id;
            INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status)
            SELECT p.user_id,'web',N'Lời mời buổi chơi đã bị từ chối','session_invitation',i.id,'unread' FROM dbo.invitations i JOIN dbo.players p ON p.id=i.inviter_id WHERE i.id=@id;
            """,ct,("@id",id));
        await tx.CommitAsync(ct);
        return OperationResult.Completed();
    }
}
