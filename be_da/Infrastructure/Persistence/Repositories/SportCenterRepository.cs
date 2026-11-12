using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Infrastructure.Persistence;
using be_da.Application.Interfaces.Repositories;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class SportCenterRepository(IConfiguration configuration, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : ISportCenterRepository
{
    private SqlConnection Connection() => new(configuration.GetConnectionString("Sep490"));
    public async Task<object> ListAsync(CancellationToken ct)
    {
        await using var connection = Connection();
        await SportCenterConnection.OpenAsync(connection, ct, dataScope);
        await using var command = new SqlCommand("SELECT id,name,is_active FROM dbo.venues WHERE @owner=1 OR id=@venue ORDER BY name", connection);
        command.Parameters.AddWithValue("@owner",dataScope?.Role=="Owner");
        command.Parameters.AddWithValue("@venue",(object?)dataScope?.RestrictedVenueId??DBNull.Value);
        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { id = reader.GetInt64(0), name = reader.IsDBNull(1) ? "Sport Center" : reader.GetString(1), active = !reader.IsDBNull(2) && reader.GetBoolean(2) });
        return rows;
    }

    public async Task<object> PersonnelAsync(CancellationToken ct)
    {
        await using var connection = Connection();
        await SportCenterConnection.OpenAsync(connection, ct, dataScope);
        await using var command = new SqlCommand("SELECT u.id,u.full_name,u.email,r.code,u.venue_id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code IN ('Staff','Manager','Referee') AND u.deleted_at IS NULL ORDER BY u.full_name", connection);
        var rows = new List<object>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            rows.Add(new { id = reader.GetInt64(0), name = reader.GetString(1), email = reader.GetString(2), role = reader.GetString(3), venueId = reader.IsDBNull(4) ? (long? )null : reader.GetInt64(4) });
        return rows;
    }

    public async Task<bool> AssignAsync(long id, long venueId, long actor, CancellationToken ct)
    {
        if(dataScope is null) return false;
        await new OperationsRepository(configuration,dataScope).PersonnelAsync(actor,id,new(venueId),ct);
        return true;
    }
}
