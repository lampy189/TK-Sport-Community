using System.Data;
using be_da.Application.Interfaces.External;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Exceptions;

namespace be_da.Infrastructure.Persistence;

// Application-owned scope. Never accept a center supplied by a request over the signed-in scope.
public static class CenterAccess
{
    public static bool IsPersonnel(IDataAccessContext? scope) => scope?.Role.ToLowerInvariant() is "staff" or "manager" or "referee";
    public static long? PersonnelVenue(IDataAccessContext? scope) => IsPersonnel(scope) ? scope!.VenueId ?? -1L : null;
    public static long? BrowsingVenue(IDataAccessContext? scope) => PersonnelVenue(scope) ?? scope?.RestrictedVenueId;
    public static void Bind(SqlCommand command, IDataAccessContext? scope, bool browsing = false)
        => command.Parameters.Add("@accessVenue", SqlDbType.BigInt).Value = (object?)(browsing ? BrowsingVenue(scope) : PersonnelVenue(scope)) ?? DBNull.Value;

    public static async Task RequireAsync(SqlConnection connection, SqlTransaction? transaction,
        IDataAccessContext? scope, string resource, long id, CancellationToken ct)
    {
        var venue = PersonnelVenue(scope);
        if (venue is null) return;
        var sql = resource switch
        {
            "product" => "SELECT COUNT(*) FROM dbo.products WITH (HOLDLOCK) WHERE id=@id AND venue_id=@venue",
            "session" => "SELECT COUNT(*) FROM dbo.sessions s WITH (HOLDLOCK) JOIN dbo.courts c WITH (HOLDLOCK) ON c.id=s.court_id WHERE s.id=@id AND c.venue_id=@venue",
            _ => throw new ArgumentOutOfRangeException(nameof(resource))
        };
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@venue", venue);
        if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) != 1)
            throw new PaymentException(403, "Không có quyền truy cập dữ liệu của cơ sở này.");
    }
}
