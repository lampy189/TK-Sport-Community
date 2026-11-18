using System.Data;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class OwnerPolicyRepository(IConfiguration configuration, IDataAccessContext scope) : IOwnerPolicyRepository
{
    public async Task<object> OptionsAsync(CancellationToken ct)
    {
        await using var c = Connection();
        await SportCenterConnection.OpenAsync(c, ct, scope);
        if (!Owner && !scope.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            throw new TKSC.Domain.Exceptions.PaymentException(403, "Không có quyền xem dữ liệu chính sách.");
        return new {
            venues = await WorkflowSql.Rows(c, null, "SELECT id,name FROM dbo.venues WHERE is_active=1 AND (@owner=1 OR id=@venue) ORDER BY name", ct, ("@owner", Owner), ("@venue", scope.VenueId)),
            products = await WorkflowSql.Rows(c, null, "SELECT id,name,venue_id AS venueId FROM dbo.products WHERE is_active=1 AND (@owner=1 OR venue_id=@venue) ORDER BY name", ct, ("@owner", Owner), ("@venue", scope.VenueId)),
            sessions = await WorkflowSql.Rows(c, null, "SELECT s.id,CONCAT(CONVERT(varchar(10),s.session_date,103),' ',CONVERT(varchar(5),ts.start_time,108),' - ',cr.name) AS name,cr.venue_id AS venueId FROM dbo.sessions s JOIN dbo.courts cr ON cr.id=s.court_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id WHERE s.session_date>=CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00')) AND LOWER(s.status) IN ('draft','open','published','closed') AND (@owner=1 OR cr.venue_id=@venue) ORDER BY s.session_date,ts.start_time", ct, ("@owner", Owner), ("@venue", scope.VenueId))
        };
    }
    private SqlConnection Connection() => new(configuration.GetConnectionString("Sep490"));
    private bool Owner => scope.Role.Equals("owner", StringComparison.OrdinalIgnoreCase);
    private bool CanRead(long venue) => Owner || scope.Role.Equals("manager", StringComparison.OrdinalIgnoreCase) && scope.VenueId == venue;
    private static OperationResult Conflict(string message) => OperationResult.Conflict(new { message });
    public async Task<OperationResult> SubmitAsync(SubmitPolicyRequest r, long actor, CancellationToken ct)
    {
        if (!CanRead(r.VenueId)) return OperationResult.Denied();
        await using var c = Connection();
        await SportCenterConnection.OpenAsync(c, ct, scope);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var cmd = new SqlCommand("""
            DECLARE @lock int;
            EXEC @lock=sys.sp_getapplock @Resource='TKSC:owner-policy',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @lock<0 THROW 51000,'Policy is busy.',1;
            IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=@venue) BEGIN SELECT CAST(-1 AS bigint); RETURN; END;
            IF @kind='product' AND NOT EXISTS(SELECT 1 FROM dbo.products WHERE id=@target AND venue_id=@venue) BEGIN SELECT CAST(-1 AS bigint); RETURN; END;
            IF @kind='session' AND NOT EXISTS(SELECT 1 FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id WHERE s.id=@target AND c.venue_id=@venue) BEGIN SELECT CAST(-1 AS bigint); RETURN; END;
            IF EXISTS(SELECT 1 FROM dbo.owner_policy_versions WHERE venue_id=@venue AND code=@code AND (kind<>@kind OR ISNULL(target_id,0)<>ISNULL(@target,0))) BEGIN SELECT CAST(-2 AS bigint); RETURN; END;
            INSERT dbo.owner_policy_versions(code,version,name,value,kind,target_id,price_vnd,venue_id,effective_from,effective_to,submitted_by,submit_reason)
            SELECT @code,ISNULL(MAX(version),0)+1,@name,@value,@kind,@target,@price,@venue,@start,@end,@actor,@reason
            FROM dbo.owner_policy_versions WHERE venue_id=@venue AND code=@code;
            SELECT CAST(SCOPE_IDENTITY() AS bigint);
            """, c, tx);
        cmd.Parameters.AddWithValue("@code", r.Code); cmd.Parameters.AddWithValue("@name", r.Name);
        cmd.Parameters.AddWithValue("@value", r.Value); cmd.Parameters.AddWithValue("@kind", r.Kind);
        cmd.Parameters.Add("@target", SqlDbType.BigInt).Value = (object?)r.TargetId ?? DBNull.Value;
        cmd.Parameters.Add("@price", SqlDbType.Int).Value = (object?)r.PriceVnd ?? DBNull.Value;
        cmd.Parameters.AddWithValue("@venue", r.VenueId); cmd.Parameters.AddWithValue("@start", r.EffectiveFrom);
        cmd.Parameters.Add("@end", SqlDbType.DateTimeOffset).Value = (object?)r.EffectiveTo ?? DBNull.Value;
        cmd.Parameters.AddWithValue("@actor", actor); cmd.Parameters.AddWithValue("@reason", r.Reason);
        var id = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        await tx.CommitAsync(ct);
        return id > 0 ? OperationResult.Created(new { id, status="submitted" }) : Conflict("Không tìm thấy đối tượng tại cơ sở hoặc mã phiên bản đã thuộc đối tượng khác.");
    }
    public async Task<OperationResult> DecideAsync(long id, DecidePolicyRequest r, long actor, CancellationToken ct)
    {
        if (!Owner) return OperationResult.Denied();
        await using var c = Connection();
        await SportCenterConnection.OpenAsync(c, ct, scope);
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await using var cmd = new SqlCommand("""
            DECLARE @lock int;
            EXEC @lock=sys.sp_getapplock @Resource='TKSC:owner-policy',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
            IF @lock<0 THROW 51000,'Policy is busy.',1;
            IF NOT EXISTS(SELECT 1 FROM dbo.owner_policy_versions WHERE id=@id) BEGIN SELECT 1; RETURN; END;
            IF EXISTS(SELECT 1 FROM dbo.owner_policy_versions WHERE id=@id AND (status<>'submitted' OR submitted_by=@actor)) BEGIN SELECT 2; RETURN; END;
            IF @approve=1 AND EXISTS(SELECT 1 FROM dbo.owner_policy_versions WHERE id=@id AND effective_from<SYSDATETIMEOFFSET()) BEGIN SELECT 3; RETURN; END;
            IF @approve=1 AND EXISTS(
              SELECT 1 FROM dbo.owner_policy_versions n JOIN dbo.owner_policy_versions o ON o.venue_id=n.venue_id
              AND ((n.kind='policy' AND o.kind='policy' AND o.code=n.code) OR (n.kind<>'policy' AND o.kind=n.kind AND o.target_id=n.target_id))
              WHERE n.id=@id AND o.status='approved' AND (o.effective_to IS NULL OR o.effective_to>n.effective_from)
              AND (n.effective_to IS NULL OR n.effective_to>o.effective_from)
              AND o.effective_from>=n.effective_from) BEGIN SELECT 4; RETURN; END;
            -- Close only an older active/scheduled version; keep its content and decision history.
            IF @approve=1 INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,old_value,new_value)
              SELECT @actor,'policy_superseded','OwnerPolicyVersion',o.id,
                COALESCE(CONVERT(nvarchar(50),o.effective_to,127),'open-ended'),CONVERT(nvarchar(50),n.effective_from,127)
              FROM dbo.owner_policy_versions o JOIN dbo.owner_policy_versions n ON n.venue_id=o.venue_id
              AND ((n.kind='policy' AND o.kind='policy' AND o.code=n.code) OR (n.kind<>'policy' AND o.kind=n.kind AND o.target_id=n.target_id))
              WHERE n.id=@id AND o.status='approved' AND o.effective_from<n.effective_from
              AND (o.effective_to IS NULL OR o.effective_to>n.effective_from);
            IF @approve=1 UPDATE o SET effective_to=n.effective_from
              FROM dbo.owner_policy_versions o JOIN dbo.owner_policy_versions n ON n.venue_id=o.venue_id
              AND ((n.kind='policy' AND o.kind='policy' AND o.code=n.code) OR (n.kind<>'policy' AND o.kind=n.kind AND o.target_id=n.target_id))
              WHERE n.id=@id AND o.status='approved' AND o.effective_from<n.effective_from
              AND (o.effective_to IS NULL OR o.effective_to>n.effective_from);
            UPDATE dbo.owner_policy_versions SET status=CASE WHEN @approve=1 THEN 'approved' ELSE 'rejected' END,
                decided_by=@actor,decided_at=SYSDATETIMEOFFSET(),decision_reason=@reason WHERE id=@id;
            INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value)
            VALUES(@actor,CASE WHEN @approve=1 THEN 'policy_approved' ELSE 'policy_rejected' END,'OwnerPolicyVersion',@id,@reason);
            SELECT 0;
            """, c, tx);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@actor", actor);
        cmd.Parameters.AddWithValue("@approve", r.Approve); cmd.Parameters.AddWithValue("@reason", r.Reason);
        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        await tx.CommitAsync(ct);
        return result switch { 0 => OperationResult.Success(new { id, status=r.Approve ? "approved" : "rejected" }),
            1 => OperationResult.Missing(null), 2 => Conflict("Yêu cầu đã xử lý hoặc người gửi không được tự duyệt."),
            3 => Conflict("Ngày hiệu lực đã qua. Gửi phiên bản mới với ngày hiệu lực mới."),
            _ => Conflict("Khoảng hiệu lực trùng phiên bản đã duyệt trong tương lai.") };
    }
    public async Task<IReadOnlyList<PolicyVersion>> HistoryAsync(string? code, long? venueId, CancellationToken ct)
    {
        if (!Owner && (scope.VenueId is null || !CanRead(scope.VenueId.Value) || venueId.HasValue && venueId!=scope.VenueId)) return [];
        venueId = Owner ? venueId : scope.VenueId;
        await using var c=Connection(); await SportCenterConnection.OpenAsync(c,ct,scope);
        await using var cmd=new SqlCommand("SELECT id,code,version,name,value,kind,target_id,price_vnd,venue_id,status,effective_from,effective_to,submitted_by,submitted_at,submit_reason,decided_by,decided_at,decision_reason FROM dbo.owner_policy_versions WHERE (@code IS NULL OR code=@code) AND (@venue IS NULL OR venue_id=@venue) ORDER BY id DESC",c);
        cmd.Parameters.Add("@code",SqlDbType.VarChar,100).Value=(object?)code??DBNull.Value;
        cmd.Parameters.Add("@venue",SqlDbType.BigInt).Value=(object?)venueId??DBNull.Value;
        var rows=new List<PolicyVersion>(); await using var rd=await cmd.ExecuteReaderAsync(ct);
        while(await rd.ReadAsync(ct)) rows.Add(new(rd.GetInt64(0),rd.GetString(1),rd.GetInt32(2),rd.GetString(3),rd.GetString(4),rd.GetString(5),rd.IsDBNull(6)?null:rd.GetInt64(6),rd.IsDBNull(7)?null:rd.GetInt32(7),rd.GetInt64(8),rd.GetString(9),rd.GetDateTimeOffset(10),rd.IsDBNull(11)?null:rd.GetDateTimeOffset(11),rd.GetInt64(12),rd.GetDateTimeOffset(13),rd.GetString(14),rd.IsDBNull(15)?null:rd.GetInt64(15),rd.IsDBNull(16)?null:rd.GetDateTimeOffset(16),rd.IsDBNull(17)?null:rd.GetString(17)));
        return rows;
    }
}
