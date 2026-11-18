using System.Data;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.External;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Exceptions;
using static be_da.Infrastructure.Persistence.Repositories.WorkflowSql;
namespace be_da.Infrastructure.Persistence.Repositories;

public sealed class OperationsRepository(IConfiguration config,IDataAccessContext scope):IOperationsRepository
{
    // Technical operators manage named accounts across centers through approved requests.
    // Use the existing trusted service DB identity only within this repository; keep the
    // original scope for authorization and record the real actor in every audit event.
    private sealed class TechnicalAccessScope:IDataAccessContext
    {
        public string Role=>"service";
        public long? VenueId=>null;
        public long? RestrictedVenueId=>null;
    }
    private async Task<SqlConnection> Open(CancellationToken ct,bool accountAccess=false)
    {
        var c=new SqlConnection(config.GetConnectionString("Sep490"));
        try { await SportCenterConnection.OpenAsync(c,ct,scope.Role=="SystemAdmin"||accountAccess&&scope.Role=="Manager"?new TechnicalAccessScope():scope); return c; }
        catch { await c.DisposeAsync(); throw; }
    }
    private static void Require(bool allowed,string message,int status=409) { if(!allowed) throw new PaymentException(status,message); }
    private void InScope(long venue)=>Require(scope.Role=="Owner"||scope.RestrictedVenueId==venue,"Cơ sở nằm ngoài phạm vi được phân công.",403);
    private static Task<int> Audit(SqlConnection c,SqlTransaction tx,long actor,string action,string entity,long id,object? before,object? after,CancellationToken ct)=>
        Execute(c,tx,"INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,old_value,new_value) VALUES(@actor,@action,@entity,@id,@old,@new)",ct,
            ("@actor",actor),("@action",action),("@entity",entity),("@id",id),("@old",before is null?null:JsonSerializer.Serialize(before)),("@new",JsonSerializer.Serialize(after)));
    private static string Future=>"(s.session_date>CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00')) OR (s.session_date=CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00')) AND EXISTS(SELECT 1 FROM dbo.time_slots fts WHERE fts.id=s.time_slot_id AND fts.end_time>CONVERT(time,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00'))))) AND LOWER(COALESCE(s.status,'')) NOT IN ('cancelled','canceled','completed','closed','ended')";
    public async Task<object> SetupAsync(CancellationToken ct)
    {
        await using var c=await Open(ct);
        var owner=scope.Role=="Owner";
        return new {
            venues=await Rows(c,null,"SELECT id,name,address,is_active AS active FROM dbo.venues WHERE @owner=1 OR id=@venue ORDER BY name",ct,("@owner",owner),("@venue",scope.RestrictedVenueId)),
            courts=await Rows(c,null,"SELECT cr.id,cr.venue_id AS venueId,cr.sport_id AS sportId,cr.name,cr.is_active AS active,(SELECT COUNT(*) FROM dbo.sessions s WHERE s.court_id=cr.id AND "+Future+") AS futureSessions FROM dbo.courts cr WHERE @owner=1 OR cr.venue_id=@venue ORDER BY cr.name",ct,("@owner",owner),("@venue",scope.RestrictedVenueId)),
            sports=await Rows(c,null,"SELECT id,name FROM dbo.sports ORDER BY name",ct),
            people=owner?await Rows(c,null,"SELECT u.id,u.full_name AS name,u.email,r.code AS role,u.venue_id AS venueId,u.status FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.deleted_at IS NULL AND r.code IN ('Manager','Staff','Referee') ORDER BY u.full_name",ct):[],
            history=await Rows(c,null,"SELECT TOP(100) a.id,a.action,a.entity_type AS entityType,a.entity_id AS entityId,u.full_name AS actor,a.old_value AS oldValue,a.new_value AS newValue FROM dbo.audit_logs a LEFT JOIN dbo.users u ON u.id=a.actor_id WHERE @owner=1 AND a.entity_type IN ('Venue','Court','Personnel') ORDER BY a.id DESC",ct,("@owner",owner))
        };
    }
    public async Task<OperationResult> SaveVenueAsync(long actor,long? id,VenueEdit request,CancellationToken ct)
    {
        Require(scope.Role=="Owner","Chỉ Owner được quản lý cơ sở.",403);
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var before=id.HasValue?await Rows(c,tx,"SELECT id,name,address,is_active FROM dbo.venues WITH(UPDLOCK,HOLDLOCK) WHERE id=@id",ct,("@id",id)):[];
        Require(!id.HasValue||before.Count==1,"Không tìm thấy cơ sở.",404);
        if(id.HasValue&&!request.Active)
        {
            var future=await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) JOIN dbo.courts cr ON cr.id=s.court_id WHERE cr.venue_id=@id AND "+Future,ct,("@id",id));
            var pending=await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.orders o WITH(UPDLOCK,HOLDLOCK) WHERE venue_id=@id AND (LOWER(status) IN ('pending','paid','confirmed','processing') OR EXISTS(SELECT 1 FROM dbo.payments p WHERE p.goods_order_id=o.id AND LOWER(p.status) IN ('pending','processing')))",ct,("@id",id));
            Require(future==0&&pending==0,$"Cơ sở còn {future} lịch chưa kết thúc và {pending} đơn hàng đang xử lý. Hoàn tất hoặc điều phối trước khi ngừng hoạt động.");
        }
        var target=await Scalar(c,tx,id.HasValue?"UPDATE dbo.venues SET name=@name,address=@address,is_active=@active WHERE id=@id; SELECT @id;":"INSERT dbo.venues(name,address,is_active) VALUES(@name,@address,@active); SELECT CONVERT(bigint,SCOPE_IDENTITY());",ct,("@id",id),("@name",request.Name),("@address",request.Address),("@active",request.Active));
        await Audit(c,tx,actor,id.HasValue?"venue_updated":"venue_created","Venue",target,before.FirstOrDefault(),new { request,at=DateTimeOffset.UtcNow },ct);
        await tx.CommitAsync(ct);
        return OperationResult.Success(new { id=target });
    }
    public async Task<OperationResult> SaveCourtAsync(long actor,long? id,CourtEdit request,CancellationToken ct)
    {
        InScope(request.VenueId);
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.venues WITH(UPDLOCK,HOLDLOCK) WHERE id=@venue AND (is_active=1 OR @active=0)",ct,("@venue",request.VenueId),("@active",request.Active))==1,"Cơ sở không hoạt động.");
        Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.sports WHERE id=@sport",ct,("@sport",request.SportId))==1,"Môn chơi không hợp lệ.",400);
        var before=id.HasValue?await Rows(c,tx,"SELECT id,venue_id,sport_id,name,is_active FROM dbo.courts WITH(UPDLOCK,HOLDLOCK) WHERE id=@id",ct,("@id",id)):[];
        Require(!id.HasValue||before.Count==1,"Không tìm thấy sân.",404);
        if(id.HasValue)
        {
            InScope(Convert.ToInt64(before[0]["venue_id"]));
            Require(Convert.ToInt64(before[0]["venue_id"])==request.VenueId,"Không điều chuyển sân sang cơ sở khác; tạo sân mới để giữ lịch sử.");
            if(Convert.ToInt64(before[0]["sport_id"])!=request.SportId)
                Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.sessions WITH(UPDLOCK,HOLDLOCK) WHERE court_id=@id",ct,("@id",id))==0,"Sân đã có lịch sử buổi chơi; không đổi môn.");
            if(!request.Active) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) WHERE s.court_id=@id AND "+Future,ct,("@id",id))==0,"Sân còn lịch chưa kết thúc. Điều phối hoặc đóng lịch trước khi ngừng hoạt động.");
        }
        var target=await Scalar(c,tx,id.HasValue?"UPDATE dbo.courts SET name=@name,sport_id=@sport,is_active=@active WHERE id=@id; SELECT @id;":"INSERT dbo.courts(venue_id,sport_id,name,is_active) VALUES(@venue,@sport,@name,@active); SELECT CONVERT(bigint,SCOPE_IDENTITY());",ct,("@id",id),("@venue",request.VenueId),("@sport",request.SportId),("@name",request.Name),("@active",request.Active));
        await Audit(c,tx,actor,id.HasValue?"court_updated":"court_created","Court",target,before.FirstOrDefault(),new { request,at=DateTimeOffset.UtcNow },ct);
        await tx.CommitAsync(ct);
        return OperationResult.Success(new { id=target });
    }
    public async Task<OperationResult> PersonnelAsync(long actor,long id,PersonnelEdit request,CancellationToken ct)
    {
        Require(scope.Role=="Owner","Chỉ Owner được điều chuyển nhân sự.",403);
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var rows=await Rows(c,tx,"SELECT u.id,u.venue_id,r.code FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND u.deleted_at IS NULL AND r.code IN ('Staff','Referee','Manager')",ct,("@id",id));
        Require(rows.Count==1,"Không tìm thấy nhân sự.",404);
        var old=rows[0];
        if(Equals(old["venue_id"],request.VenueId)) return OperationResult.Completed();
        if(request.VenueId.HasValue) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.venues WITH(UPDLOCK,HOLDLOCK) WHERE id=@venue AND is_active=1",ct,("@venue",request.VenueId))==1,"Cơ sở mới không hoạt động.");
        await ResolveAssignments(c,tx,id,request.ReplacementUserId,old["code"]!.ToString()!,old["venue_id"],actor,ct);
        await Execute(c,tx,"UPDATE dbo.users SET venue_id=@venue WHERE id=@id; INSERT dbo.account_audit_logs(user_id,actor_id,action,old_value,new_value) VALUES(@id,@actor,'SportCenterAssigned',CONVERT(varchar(30),@old),CONVERT(varchar(30),@venue));",ct,("@id",id),("@actor",actor),("@old",old["venue_id"]),("@venue",request.VenueId));
        await Audit(c,tx,actor,"personnel_assigned","Personnel",id,old,new { request,at=DateTimeOffset.UtcNow },ct);
        await tx.CommitAsync(ct);
        return OperationResult.Completed();
    }
    private static async Task ResolveAssignments(SqlConnection c,SqlTransaction tx,long user,long? replacement,string role,object? oldVenue,long actor,CancellationToken ct)
    {
        var referee=role=="Referee";
        var table=referee?"session_referees":"session_staff";
        var key=referee?"referee_id":"staff_id";
        var future=await Rows(c,tx,$"SELECT a.id,s.id AS sessionId,s.session_date,ts.start_time,ts.end_time FROM dbo.{table} a WITH(UPDLOCK,HOLDLOCK) JOIN dbo.sessions s WITH(UPDLOCK,HOLDLOCK) ON s.id=a.session_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id WHERE a.{key}=@user AND "+Future,ct,("@user",user));
        if(future.Count==0) return;
        Require(replacement.HasValue&&replacement!=user,$"Nhân sự còn {future.Count} lịch chưa kết thúc. Chọn người thay thế cùng vai trò tại cơ sở cũ.");
        Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@replacement AND u.venue_id=@venue AND r.code=@role AND LOWER(u.status)='active' AND u.deleted_at IS NULL",ct,("@replacement",replacement),("@venue",oldVenue),("@role",role))==1,"Người thay thế phải hoạt động tại cơ sở cũ và có cùng vai trò.");
        foreach(var item in future)
        {
            var conflicts=await Scalar(c,tx,$"SELECT COUNT(*) FROM dbo.{table} a WITH(UPDLOCK,HOLDLOCK) JOIN dbo.sessions s ON s.id=a.session_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id WHERE a.{key}=@replacement AND s.session_date=@date AND ts.start_time<@end AND ts.end_time>@start AND LOWER(COALESCE(s.status,'')) NOT IN ('cancelled','canceled','completed','closed','ended')",ct,("@replacement",replacement),("@date",item["session_date"]),("@start",item["start_time"]),("@end",item["end_time"]));
            Require(conflicts==0,"Người thay thế bị trùng lịch; chưa thay đổi phân công.");
            await Execute(c,tx,$"UPDATE dbo.{table} SET {key}=@replacement"+(referee?",assigned_by=@actor":"")+" WHERE id=@id",ct,("@id",item["id"]),("@replacement",replacement),("@actor",actor));
            await Audit(c,tx,actor,"future_assignment_transferred","Personnel",user,item,new { replacement,at=DateTimeOffset.UtcNow },ct);
        }
    }

    private sealed record AccessSnapshot(AccessProposal Request,long OldRoleId,long? OldVenue,string? OldStatus,DateTimeOffset At);
    public async Task<object> AccessAsync(long actor,string role,CancellationToken ct)
    {
        await using var c=await Open(ct,true);
        var manager=role=="Manager";
        return new {
            users=await Rows(c,null,"SELECT u.id,u.full_name AS name,u.email,r.code AS role,u.venue_id AS venueId,u.status FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.deleted_at IS NULL AND (@manager=0 OR u.venue_id=@venue OR EXISTS(SELECT 1 FROM dbo.approvals a JOIN dbo.audit_logs l ON l.entity_id=a.id AND l.entity_type='AccessRequest' AND l.action='access_requested' WHERE a.entity_type='account_access' AND a.entity_id=u.id AND TRY_CONVERT(bigint,JSON_VALUE(l.new_value,'$.Request.VenueId'))=@venue AND JSON_VALUE(l.new_value,'$.Request.Role') IN ('Staff','Referee'))) ORDER BY u.full_name",ct,("@manager",manager),("@venue",scope.RestrictedVenueId)),
            venues=await Rows(c,null,"SELECT id,name FROM dbo.venues WHERE is_active=1 AND (@manager=0 OR id=@venue)",ct,("@manager",manager),("@venue",scope.RestrictedVenueId)),
            requests=await Rows(c,null,"""
                SELECT a.id,a.entity_id AS targetUserId,a.requested_by AS requestedBy,a.approved_by AS approvedBy,a.status,u.full_name AS target,ru.full_name AS requester,l.new_value AS payload
                FROM dbo.approvals a JOIN dbo.users u ON u.id=a.entity_id JOIN dbo.users ru ON ru.id=a.requested_by
                CROSS APPLY(SELECT TOP(1) new_value FROM dbo.audit_logs WHERE entity_type='AccessRequest' AND entity_id=a.id AND action='access_requested' ORDER BY id) l
                WHERE a.entity_type='account_access' AND (@manager=0 OR (TRY_CONVERT(bigint,JSON_VALUE(l.new_value,'$.Request.VenueId'))=@venue AND JSON_VALUE(l.new_value,'$.Request.Role') IN ('Staff','Referee')))
                ORDER BY a.id DESC
                """,ct,("@manager",manager),("@venue",scope.RestrictedVenueId))
        };
    }
    public async Task<OperationResult> ProposeAsync(long actor,string role,AccessProposal request,CancellationToken ct)
    {
        if(role=="Manager") Require(request.Role is "Staff" or "Referee" && request.VenueId==scope.RestrictedVenueId,"Manager chỉ đề xuất Staff/Referee tại cơ sở của mình.",403);
        Require(request.Action!="handover"||role is "Owner" or "SystemAdmin","Bàn giao quản trị cần Owner hoặc System Admin lập yêu cầu.",403);
        Require(request.Role=="SystemAdmin"?request.VenueId is null:request.VenueId is >0,"Chọn cơ sở cho nhân sự; tài khoản quản trị kỹ thuật không gắn vai trò nghiệp vụ.",400);
        await using var c=await Open(ct,true);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var users=await Rows(c,tx,"SELECT u.role_id,u.venue_id,u.status,r.code FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND u.deleted_at IS NULL",ct,("@id",request.TargetUserId));
        Require(users.Count==1,"Không tìm thấy tài khoản đích.",404);
        Require(users[0]["code"]?.ToString()!="Owner","Không thay đổi tài khoản Owner qua luồng này.",403);
        if(role=="Manager") Require(users[0]["code"]?.ToString() is "Player" or "Staff" or "Referee" && (users[0]["code"]?.ToString()=="Player"||Equals(users[0]["venue_id"],scope.RestrictedVenueId)),"Manager không thay đổi quyền quản trị hoặc nhân sự cơ sở khác.",403);
        Require(users[0]["code"]?.ToString()!="SystemAdmin"||request.Role=="SystemAdmin","Dùng tài khoản riêng cho vai trò nghiệp vụ; không chuyển tài khoản quản trị sang nhân sự.");
        Require(request.Role!="SystemAdmin"||users[0]["code"]?.ToString() is "Player" or "SystemAdmin","Dùng tài khoản riêng cho quản trị kỹ thuật; không chuyển tài khoản nhân sự nghiệp vụ thành System Admin.");
        if(request.Action=="revoke") Require(users[0]["code"]?.ToString()==request.Role,"Vai trò yêu cầu thu hồi không khớp tài khoản hiện tại.");
        if(request.VenueId.HasValue) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.venues WHERE id=@venue AND is_active=1",ct,("@venue",request.VenueId))==1,"Cơ sở không hoạt động.");
        if(request.OutgoingUserId.HasValue) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND r.code='SystemAdmin' AND LOWER(u.status)='active' AND u.deleted_at IS NULL",ct,("@id",request.OutgoingUserId))==1,"Tài khoản bàn giao hiện tại không hợp lệ.");
        Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.approvals WITH(UPDLOCK,HOLDLOCK) WHERE entity_type='account_access' AND entity_id=@id AND status IN ('pending','approved','provisioned','verified')",ct,("@id",request.TargetUserId))==0,"Tài khoản đã có yêu cầu đang xử lý.");
        var id=await Scalar(c,tx,"INSERT dbo.approvals(entity_type,entity_id,requested_by,status) VALUES('account_access',@id,@actor,'pending'); SELECT CONVERT(bigint,SCOPE_IDENTITY());",ct,("@id",request.TargetUserId),("@actor",actor));
        await Audit(c,tx,actor,"access_requested","AccessRequest",id,null,new AccessSnapshot(request,Convert.ToInt64(users[0]["role_id"]),users[0]["venue_id"] as long?,users[0]["status"]?.ToString(),DateTimeOffset.UtcNow),ct);
        await Execute(c,tx,"INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status) SELECT u.id,'web',N'Có đề xuất quyền tài khoản cần phê duyệt','access_request',@request,'unread' FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.deleted_at IS NULL AND LOWER(u.status)='active' AND u.id<>@actor AND (r.code='Owner' OR (@role IN ('Staff','Referee') AND r.code='Manager' AND u.venue_id=@venue))",ct,("@request",id),("@actor",actor),("@role",request.Role),("@venue",request.VenueId));
        await tx.CommitAsync(ct);
        return OperationResult.Created(new { id });
    }
    private static async Task<(Dictionary<string,object?> Row,AccessSnapshot Snapshot)> AccessRow(SqlConnection c,SqlTransaction tx,long id,CancellationToken ct)
    {
        var rows=await Rows(c,tx,"""
            SELECT a.*,l.new_value FROM dbo.approvals a WITH(UPDLOCK,HOLDLOCK)
            CROSS APPLY(SELECT TOP(1) new_value FROM dbo.audit_logs WHERE entity_type='AccessRequest' AND entity_id=a.id AND action='access_requested' ORDER BY id) l
            WHERE a.id=@id AND a.entity_type='account_access'
            """,ct,("@id",id));
        Require(rows.Count==1,"Không tìm thấy yêu cầu.",404);
        return (rows[0],JsonSerializer.Deserialize<AccessSnapshot>((string)rows[0]["new_value"]!)!);
    }
    private static async Task<bool> SamePerson(SqlConnection c,SqlTransaction tx,long left,long right,CancellationToken ct)=>left==right||await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users a JOIN dbo.users b ON a.email=b.email OR (a.google_subject IS NOT NULL AND a.google_subject=b.google_subject) WHERE a.id=@a AND b.id=@b",ct,("@a",left),("@b",right))>0;
    public async Task<OperationResult> DecideAsync(long actor,string role,long id,ApprovalDecision request,CancellationToken ct)
    {
        await using var c=await Open(ct,true);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var (row,snapshot)=await AccessRow(c,tx,id,ct);
        Require(role=="Owner"||role=="Manager"&&snapshot.Request.Role is "Staff" or "Referee"&&snapshot.Request.VenueId==scope.RestrictedVenueId,"Không có thẩm quyền phê duyệt yêu cầu này.",403);
        Require(!await SamePerson(c,tx,actor,Convert.ToInt64(row["requested_by"]),ct)&&!await SamePerson(c,tx,actor,snapshot.Request.TargetUserId,ct),"Không được tự duyệt hoặc duyệt quyền cho chính mình.",403);
        Require(row["status"]?.ToString()=="pending"||!request.Approve&&row["status"]?.ToString()=="approved","Yêu cầu đã được triển khai hoặc không còn chờ quyết định.");
        await Execute(c,tx,"UPDATE dbo.approvals SET status=@status,approved_by=@actor WHERE id=@id",ct,("@id",id),("@actor",actor),("@status",request.Approve?"approved":"rejected"));
        await Audit(c,tx,actor,request.Approve?"access_approved":"access_rejected","AccessRequest",id,null,new { request.Reason,at=DateTimeOffset.UtcNow },ct);
        await Execute(c,tx,"INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status) SELECT u.id,'web',@title,'access_request',@request,'unread' FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.deleted_at IS NULL AND LOWER(u.status)='active' AND (u.id=@requester OR (@approved=1 AND r.code='SystemAdmin'))",ct,("@title",request.Approve?"Đề xuất quyền đã duyệt, chờ triển khai":"Đề xuất quyền đã bị từ chối"),("@request",id),("@requester",row["requested_by"]),("@approved",request.Approve));
        await tx.CommitAsync(ct);
        return OperationResult.Completed();
    }
    public async Task<OperationResult> ExecuteAsync(long actor,long id,CancellationToken ct)
    {
        Require(scope.Role=="SystemAdmin","Chỉ System Admin triển khai quyền đã duyệt.",403);
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var (row,snapshot)=await AccessRow(c,tx,id,ct);
        if(row["status"]?.ToString()=="executed") return OperationResult.Completed();
        var r=snapshot.Request;
        var handoverFinish=r.Action=="handover"&&row["status"]?.ToString()=="verified";
        Require(row["approved_by"] is not null&&(row["status"]?.ToString()=="approved"||handoverFinish),"Yêu cầu chưa được duyệt hoặc người nhận chưa xác nhận bàn giao.");
        Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@approver AND LOWER(u.status)='active' AND u.deleted_at IS NULL AND (r.code='Owner' OR (r.code='Manager' AND @role IN ('Staff','Referee') AND u.venue_id=@venue))",ct,("@approver",row["approved_by"]),("@role",r.Role),("@venue",r.VenueId))==1,"Người phê duyệt không còn thẩm quyền. Cần phê duyệt lại.");
        var target=handoverFinish?r.OutgoingUserId!.Value:r.TargetUserId;
        var users=await Rows(c,tx,"SELECT u.role_id,u.venue_id,u.status,r.code FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND u.deleted_at IS NULL",ct,("@id",target));
        Require(users.Count==1,"Tài khoản không còn tồn tại.",404);
        if(handoverFinish) Require(users[0]["code"]?.ToString()=="SystemAdmin"&&string.Equals(users[0]["status"]?.ToString(),"active",StringComparison.OrdinalIgnoreCase),"Quyền tài khoản bàn giao đã thay đổi. Cần kiểm tra lại trước khi thu hồi.");
        if(!handoverFinish) Require(Convert.ToInt64(users[0]["role_id"])==snapshot.OldRoleId&&Equals(users[0]["venue_id"],snapshot.OldVenue)&&users[0]["status"]?.ToString()==snapshot.OldStatus,"Quyền tài khoản đã thay đổi sau khi đề xuất. Cần một yêu cầu mới.");
        if(r.VenueId.HasValue) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.venues WITH(UPDLOCK,HOLDLOCK) WHERE id=@venue AND is_active=1",ct,("@venue",r.VenueId))==1,"Cơ sở đã ngừng hoạt động.");
        var revoke=r.Action=="revoke"||handoverFinish;
        if(users[0]["code"]?.ToString() is "Staff" or "Referee" or "Manager")
            await ResolveAssignments(c,tx,target,null,users[0]["code"]!.ToString()!,users[0]["venue_id"],actor,ct);
        if(revoke&&users[0]["code"]?.ToString()=="SystemAdmin")
            Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users u WITH(UPDLOCK,HOLDLOCK) JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='SystemAdmin' AND LOWER(u.status)='active' AND u.deleted_at IS NULL AND u.id<>@id",ct,("@id",target))>0,"Không được thu hồi quản trị viên hoạt động cuối cùng.");
        if(handoverFinish) Require(await Scalar(c,tx,"SELECT COUNT(*) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND r.code='SystemAdmin' AND LOWER(u.status)='active' AND u.deleted_at IS NULL",ct,("@id",r.TargetUserId))==1,"Người nhận bàn giao không còn quyền quản trị hoạt động.");
        await Execute(c,tx,revoke?"UPDATE dbo.users SET status='Inactive' WHERE id=@target":"UPDATE dbo.users SET role_id=(SELECT id FROM dbo.roles WHERE code=@role),venue_id=@venue,status='Active' WHERE id=@target",ct,("@target",target),("@role",r.Role),("@venue",r.VenueId));
        var status=r.Action=="handover"&&!handoverFinish?"provisioned":"executed";
        await Execute(c,tx,"UPDATE dbo.approvals SET status=@status WHERE id=@id",ct,("@id",id),("@status",status));
        await Audit(c,tx,actor,"access_"+status,"AccessRequest",id,users[0],new { target,r.Role,r.VenueId,revoke,at=DateTimeOffset.UtcNow },ct);
        await Execute(c,tx,"INSERT dbo.account_audit_logs(user_id,actor_id,action,old_value,new_value) VALUES(@target,@actor,'ApprovedAccessImplemented',@old,@new)",ct,("@target",target),("@actor",actor),("@old",JsonSerializer.Serialize(users[0])),("@new",JsonSerializer.Serialize(new { requestId=id,status })));
        await tx.CommitAsync(ct);
        return OperationResult.Completed();
    }
    public async Task<OperationResult> VerifyAsync(long actor,long id,CancellationToken ct)
    {
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var (row,snapshot)=await AccessRow(c,tx,id,ct);
        Require(snapshot.Request.Action=="handover"&&snapshot.Request.TargetUserId==actor,"Chỉ người nhận bàn giao được xác nhận kiểm tra.",403);
        if(row["status"]?.ToString()=="verified") return OperationResult.Completed();
        Require(row["status"]?.ToString()=="provisioned","Tài khoản nhận bàn giao chưa được cấp quyền.");
        await Execute(c,tx,"UPDATE dbo.approvals SET status='verified' WHERE id=@id",ct,("@id",id));
        await Audit(c,tx,actor,"handover_verified","AccessRequest",id,null,new { at=DateTimeOffset.UtcNow },ct);
        await tx.CommitAsync(ct);
        return OperationResult.Completed();
    }
    public async Task<object> OverviewAsync(long actor,CancellationToken ct)
    {
        await using var c=await Open(ct);
        var technical=scope.Role=="SystemAdmin";
        var global=scope.Role=="Owner";
        return new {
            sessions=technical?[]:await Rows(c,null,"SELECT LOWER(s.status) AS status,COUNT(*) AS count FROM dbo.sessions s JOIN dbo.courts cr ON cr.id=s.court_id WHERE (@global=1 OR cr.venue_id=@venue) AND s.session_date=CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00')) GROUP BY LOWER(s.status)",ct,("@global",global),("@venue",scope.RestrictedVenueId)),
            tickets=technical?[]:await Rows(c,null,"SELECT LOWER(t.status) AS status,COUNT(*) AS count FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.courts cr ON cr.id=s.court_id WHERE (@global=1 OR cr.venue_id=@venue) AND s.session_date=CONVERT(date,SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00')) GROUP BY LOWER(t.status)",ct,("@global",global),("@venue",scope.RestrictedVenueId)),
            feedback=await Rows(c,null,"SELECT TOP(50) a.id,u.full_name AS actor,a.new_value AS detail FROM dbo.audit_logs a JOIN dbo.users u ON u.id=a.actor_id WHERE a.action='trial_feedback' AND (@global=1 OR a.actor_id=@actor OR TRY_CONVERT(bigint,JSON_VALUE(a.new_value,'$.venueId'))=@venue) ORDER BY a.id DESC",ct,("@global",global),("@actor",actor),("@venue",scope.RestrictedVenueId))
        };
    }
    public async Task<OperationResult> FeedbackAsync(long actor,TrialFeedback request,CancellationToken ct)
    {
        await using var c=await Open(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await Audit(c,tx,actor,"trial_feedback","TrialFeedback",actor,null,new { request.Content,venueId=scope.RestrictedVenueId,at=DateTimeOffset.UtcNow },ct);
        await tx.CommitAsync(ct);
        return OperationResult.Created(new { saved=true });
    }
}
