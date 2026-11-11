using System.Data;
using be_da.Application.Abstractions;
using be_da.Application.Revenue;
using be_da.Authorization;
using Microsoft.Data.SqlClient;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlRevenueStore(string connectionString) : IRevenueStore
{
    public async Task<RevenueReportData> GetDailyAsync(
        DateOnly from,
        DateOnly to,
        long? venueId,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await SportCenterConnection.OpenAsync(connection, cancellationToken);

        if (venueId.HasValue)
        {
            await using var venueCommand = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.venues WHERE id=@venue", connection);
            venueCommand.Parameters.AddWithValue("@venue", venueId.Value);
            if ((int)(await venueCommand.ExecuteScalarAsync(cancellationToken))! == 0)
                return new(false, Array.Empty<RevenueEntry>(), 0);
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
        command.Parameters.AddWithValue("@start",
            new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)));
        command.Parameters.AddWithValue("@end",
            new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)));
        command.Parameters.Add("@venue", SqlDbType.BigInt).Value = (object?)venueId ?? DBNull.Value;

        var entries = new List<RevenueEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new RevenueEntry(
                DateOnly.FromDateTime(reader.GetDateTime(0)),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3)));
        }

        await reader.NextResultAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new(true, entries, reader.GetInt64(0));
    }
}
