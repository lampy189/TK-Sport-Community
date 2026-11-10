using System.Data.Common;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace be_da.Authorization;

// Set every key on every checkout from the pool, including anonymous/background requests.
public static class SportCenterConnection
{
    public const string VenueClaim = "tksc:venue_id";
    private static readonly HttpContextAccessor Accessor = new();
    public static async Task OpenAsync(SqlConnection connection, CancellationToken ct = default)
    {
        await connection.OpenAsync(ct);
        await InitializeAsync(connection, ct);
    }
    public static async Task InitializeAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection is not SqlConnection) return;
        var http = Accessor.HttpContext;
        var role = http is null ? "service" : http.User.Identity?.IsAuthenticated == true
            ? http.User.FindFirstValue(ClaimTypes.Role) ?? "denied" : "public";
        var venue = http?.Items["SportCenterId"] as long?;
        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'tksc_role',@value=@role; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=@venue;";
        var rp = command.CreateParameter(); rp.ParameterName = "@role"; rp.Value = role; command.Parameters.Add(rp);
        var vp = command.CreateParameter(); vp.ParameterName = "@venue"; vp.DbType = System.Data.DbType.Int64; vp.Value = (object?)venue ?? DBNull.Value; command.Parameters.Add(vp);
        await command.ExecuteNonQueryAsync(ct);
    }
}

public sealed class SportCenterConnectionInterceptor : DbConnectionInterceptor
{
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        => SportCenterConnection.InitializeAsync(connection, cancellationToken);
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => SportCenterConnection.InitializeAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
}

public sealed class SportCenterMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (context.User.Identity?.IsAuthenticated == true && long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            long? value = long.TryParse(context.User.FindFirstValue(SportCenterConnection.VenueClaim), out var assigned) ? assigned : null;
            if (value is long venueId) context.Items["SportCenterId"] = venueId;
            var role = context.User.FindFirstValue(ClaimTypes.Role);
            if (new[] { "staff", "manager", "referee" }.Contains(role?.ToLowerInvariant()) && value is not long
                && !context.Request.Path.StartsWithSegments("/api/auth") && !context.Request.Path.StartsWithSegments("/api/v1/sport-centers"))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { code = "sport_center_required", message = "Tài khoản chưa được phân công Sport Center. Liên hệ Owner." });
                return;
            }
        }
        await next(context);
    }
}
