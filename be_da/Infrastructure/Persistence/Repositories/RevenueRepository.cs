using Microsoft.Data.SqlClient;
using be_da.Infrastructure.Persistence;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class RevenueRepository(IConfiguration configuration, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : IRevenueRepository
{
    public async Task<RevenueSnapshot?> ReadAsync(DateOnly from, DateOnly to, long? venueId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection, ct, dataScope);
        if (venueId.HasValue)
        {
            await using var exists = new SqlCommand("SELECT COUNT(*) FROM dbo.venues WHERE id=@venue", connection);
            exists.Parameters.AddWithValue("@venue", venueId.Value);
            if ((int)(await exists.ExecuteScalarAsync(ct))! == 0)
                return null;
        }

        await using var command = new SqlCommand("""
            SELECT CONVERT(date,SWITCHOFFSET(recognized_at,'+07:00')) day,
            SUM(CASE WHEN entry_type='social_receipt' THEN amount_vnd ELSE CONVERT(bigint,0) END),
            SUM(CASE WHEN entry_type='goods_receipt' THEN amount_vnd ELSE CONVERT(bigint,0) END),
            SUM(CASE WHEN entry_type='refund' THEN amount_vnd ELSE CONVERT(bigint,0) END)
            FROM dbo.revenue_entries WHERE recognized_at>=@start AND recognized_at<@end
            AND (@venue IS NULL OR venue_id=@venue)
            GROUP BY CONVERT(date,SWITCHOFFSET(recognized_at,'+07:00'));
            SELECT COUNT_BIG(*) FROM dbo.payments p WHERE LOWER(p.status)='paid'
            AND NOT EXISTS(SELECT 1 FROM dbo.revenue_entries e WHERE e.payment_id=p.id AND e.refund_id IS NULL)
            AND (@venue IS NULL OR EXISTS(SELECT 1 FROM dbo.orders o WHERE o.id=p.goods_order_id AND o.venue_id=@venue)
             OR EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.courts c ON c.id=s.court_id WHERE t.order_id=p.order_id AND c.venue_id=@venue));
            """, connection);
        command.Parameters.AddWithValue("@start", new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)));
        command.Parameters.AddWithValue("@end", new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)));
        command.Parameters.Add("@venue", System.Data.SqlDbType.BigInt).Value = (object? )venueId ?? DBNull.Value;
        var rows = new Dictionary<DateOnly, RevenueDay>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var day = DateOnly.FromDateTime(reader.GetDateTime(0));
            rows[day] = new(day, reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3));
        }

        await reader.NextResultAsync(ct);
        await reader.ReadAsync(ct);
        var unposted = reader.GetInt64(0);
        return new(rows, unposted);
    }
}
