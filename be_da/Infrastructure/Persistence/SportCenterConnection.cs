using be_da.Application.Interfaces.External;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace be_da.Infrastructure.Persistence;
// Set every key on every checkout from the pool, including anonymous/background requests.
public static class SportCenterConnection
{
    public const string VenueClaim = "tksc:venue_id";
    public static async Task OpenAsync(SqlConnection connection, CancellationToken ct = default, IDataAccessContext? scope = null)
    {
        await connection.OpenAsync(ct);
        await InitializeAsync(connection, ct, scope);
    }

    public static async Task InitializeAsync(DbConnection connection, CancellationToken ct, IDataAccessContext? scope = null)
    {
        if (connection is not SqlConnection)
            return;
        var role = scope?.Role ?? "service";
        var venue = scope?.VenueId;
        await using var command = connection.CreateCommand();
        command.CommandText = "EXEC sys.sp_set_session_context @key=N'tksc_role',@value=@role; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=@venue;";
        var rp = command.CreateParameter();
        rp.ParameterName = "@role";
        rp.Value = role;
        command.Parameters.Add(rp);
        var vp = command.CreateParameter();
        vp.ParameterName = "@venue";
        vp.DbType = System.Data.DbType.Int64;
        vp.Value = (object? )venue ?? DBNull.Value;
        command.Parameters.Add(vp);
        await command.ExecuteNonQueryAsync(ct);
    }
}

public sealed class SportCenterConnectionInterceptor(IDataAccessContext scope) : DbConnectionInterceptor
{
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default) => SportCenterConnection.InitializeAsync(connection, cancellationToken, scope);
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => SportCenterConnection.InitializeAsync(connection, CancellationToken.None, scope).GetAwaiter().GetResult();
}
