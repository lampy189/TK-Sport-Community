using System.Data;
using System.Security.Claims;
using be_da.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace be_da.Controllers;

[ApiController, Authorize, Route("api/v1/sport-centers")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SportCentersController(IConfiguration configuration, IAntiforgery antiforgery) : ControllerBase
{
    private SqlConnection Connection() => new(configuration.GetConnectionString("Sep490"));
    [HttpGet]
    [Authorize(Roles = "Owner,Manager,Staff,Referee,owner,manager,staff,referee")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        await using var connection = Connection();
        await SportCenterConnection.OpenAsync(connection, ct);
        await using var command = new SqlCommand("SELECT id,name,is_active FROM dbo.venues ORDER BY name", connection);
        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) rows.Add(new { id=reader.GetInt64(0), name=reader.IsDBNull(1)?"Sport Center":reader.GetString(1), active=!reader.IsDBNull(2)&&reader.GetBoolean(2) });
        return Ok(new { canViewAll=User.IsInRole("Owner")||User.IsInRole("owner"), assignedVenueId=HttpContext.Items["SportCenterId"], centers=rows });
    }

    [HttpGet("personnel"), Authorize(Roles="Owner,owner")]
    public async Task<IActionResult> Personnel(CancellationToken ct)
    {
        await using var connection=Connection(); await SportCenterConnection.OpenAsync(connection,ct);
        await using var command=new SqlCommand("SELECT u.id,u.full_name,u.email,r.code,u.venue_id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code IN ('Staff','Manager','Referee') AND u.deleted_at IS NULL ORDER BY u.full_name",connection);
        var rows=new List<object>(); await using var reader=await command.ExecuteReaderAsync(ct);
        while(await reader.ReadAsync(ct)) rows.Add(new{id=reader.GetInt64(0),name=reader.GetString(1),email=reader.GetString(2),role=reader.GetString(3),venueId=reader.IsDBNull(4)?(long?)null:reader.GetInt64(4)});
        return Ok(rows);
    }
    public sealed record AssignRequest(long VenueId);
    [HttpPut("personnel/{id:long}"), Authorize(Roles="Owner,owner")]
    public async Task<IActionResult> Assign(long id, AssignRequest request, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch(AntiforgeryValidationException) { return BadRequest(new{code="invalid_csrf_token"}); }
        await using var connection=Connection(); await SportCenterConnection.OpenAsync(connection,ct);
        await using var tx=(SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using var command=new SqlCommand("""
            IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=@venue AND is_active=1) SELECT 0;
            ELSE BEGIN
              DECLARE @old bigint=(SELECT venue_id FROM dbo.users WITH(UPDLOCK,HOLDLOCK) WHERE id=@id);
              UPDATE u SET venue_id=@venue FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
              WHERE u.id=@id AND u.deleted_at IS NULL AND r.code IN ('Staff','Manager','Referee');
              DECLARE @changed int=@@ROWCOUNT;
              IF @changed=1 INSERT dbo.account_audit_logs(user_id,actor_id,action,old_value,new_value)
              VALUES(@id,@actor,'SportCenterAssigned',CONVERT(varchar(30),@old),CONVERT(varchar(30),@venue));
              SELECT @changed;
            END
            """,connection,tx);
        command.Parameters.AddWithValue("@id",id);command.Parameters.AddWithValue("@venue",request.VenueId);
        command.Parameters.AddWithValue("@actor",long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        var changed=(int)(await command.ExecuteScalarAsync(ct))!; await tx.CommitAsync(ct);
        return changed==1?NoContent():BadRequest(new{message="Nhân sự hoặc Sport Center không hợp lệ."});
    }
}
