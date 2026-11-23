using TKSC.Domain.Exceptions;
using be_da.Application.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Infrastructure.Persistence;
using TKSC.Application.DTOs;
using TKSC.Domain.Payments;

namespace be_da.Infrastructure.Persistence.Repositories;
public sealed class SqlLateEntryStore(IConfiguration config, TimeProvider clock, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : be_da.Application.Interfaces.Repositories.ILateEntryStore
{
    private SqlConnection Connection() => new(config.GetConnectionString("Sep490"));
    private const string Select = """
      SELECT s.id,c.venue_id,v.name,c.name,sp.name,s.band,s.session_date,ts.start_time,ts.end_time,
        s.capacity,ISNULL(a.playing,0),ISNULL(a.confirmed,0),ISNULL(a.holds,0),
        dbo.OwnerEffectivePrice('session',s.id,s.price_vnd,SYSDATETIMEOFFSET()),s.status, offers.is_open
      FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
      JOIN dbo.sports sp ON sp.id=s.sport_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
      LEFT JOIN dbo.late_entry_offers offers ON offers.session_id=s.id
      OUTER APPLY(
        SELECT SUM(CASE WHEN LOWER(t.status)='checkedin' THEN 1 ELSE 0 END) playing,
          SUM(CASE WHEN LOWER(t.status) IN ('confirmed','valid','paid') THEN 1 ELSE 0 END) confirmed,
          SUM(CASE WHEN LOWER(t.status) IN ('pending','reserved') THEN 1 ELSE 0 END) holds
        FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
        WHERE t.session_id=s.id AND (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
          (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))
      ) a
      WHERE v.is_active=1 AND (@venue IS NULL OR c.venue_id=@venue) AND (@session IS NULL OR s.id=@session) AND (@accessVenue IS NULL OR c.venue_id=@accessVenue)
      """;
    private async Task<List<LateEntryOffer>> Read(SqlConnection c, long? venue, long? session, bool manage, CancellationToken ct, SqlTransaction? tx = null)
    {
        var now = clock.GetUtcNow();
        await using var cmd = new SqlCommand(Select, c, tx);
        CenterAccess.Bind(cmd, dataScope, browsing: true);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.Parameters.Add("@venue", SqlDbType.BigInt).Value = (object? )venue ?? DBNull.Value;
        cmd.Parameters.Add("@session", SqlDbType.BigInt).Value = (object? )session ?? DBNull.Value;
        var result = new List<LateEntryOffer>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var date = DateOnly.FromDateTime(r.GetDateTime(6));
            var capacity = Math.Min((int)r.GetInt16(9), 8);
            var playing = r.GetInt32(10);
            var confirmed = r.GetInt32(11);
            var holds = r.GetInt32(12);
            var available = Math.Max(0, capacity - playing - confirmed - holds);
            var start = LateEntryRules.End(date, TimeOnly.FromTimeSpan(r.GetTimeSpan(7)));
            var end = LateEntryRules.End(date, TimeOnly.FromTimeSpan(r.GetTimeSpan(8)));
            var basePrice = r.GetInt32(13);
            var quote = LateEntryRules.Quote(start, end, r.GetString(14), now, basePrice);
            var paused = !r.IsDBNull(15) && !r.GetBoolean(15);
            if (quote is null || (!manage && (available == 0 || paused)))
                continue;
            result.Add(new(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5), start, end, capacity, playing, confirmed, holds, available, paused ? 0 : available, quote.PriceVnd, !paused && available > 0, quote.Revision, basePrice, quote.PricePercent, paused));
        }

        return result;
    }

    public async Task SetPaused(long? venue, long session, long actor, bool paused, CancellationToken ct)
    {
        await using var c = Connection();
        await SportCenterConnection.OpenAsync(c, ct, dataScope);
        if (venue is null && !await LateEntrySql.IsStandaloneSchema(c, ct))
            throw new PaymentException(403, "Tài khoản chưa được phân công cơ sở.");
        await using var tx = (SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await LateEntrySql.RequireSchema(c, tx, ct);
        // Take the same session lock as registration before reading or changing the offer.
        var locked = await WorkflowSql.Scalar(c, tx, """
            SELECT s.id FROM dbo.sessions s WITH (UPDLOCK,HOLDLOCK) JOIN dbo.courts c ON c.id=s.court_id
            WHERE s.id=@session AND (@venue IS NULL OR c.venue_id=@venue)
            """, ct, ("@session", session), ("@venue", (object?)venue ?? DBNull.Value));
        if (locked == 0)
            throw new PaymentException(404, "Không tìm thấy buổi chơi.");
        var offer = (await Read(c, venue, session, true, ct, tx)).SingleOrDefault();
        if (offer is null || offer.Capacity < 1)
            throw new PaymentException(409, "Buổi không còn đủ điều kiện bán vé giờ lẻ.");
        if (paused != offer.IsPaused)
        {
            await using var update = WorkflowSql.Command(c, tx, """
                UPDATE dbo.late_entry_offers SET is_open=@open, updated_by=@actor, updated_at=@now
                WHERE session_id=@session;
                IF @@ROWCOUNT=0
                    INSERT dbo.late_entry_offers(session_id,max_tickets,price_vnd,is_open,revision,updated_by,updated_at)
                    VALUES(@session,@capacity,@price,@open,@revision,@actor,@now);
                """, ("@session", session), ("@capacity", offer.Capacity), ("@price", offer.PriceVnd),
                ("@open", !paused), ("@revision", offer.Revision), ("@actor", actor), ("@now", clock.GetUtcNow()));
            await update.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<LateEntryOffer>> List(long? venue, long? session, bool manage, CancellationToken ct)
    {
        await using var c = Connection();
        await SportCenterConnection.OpenAsync(c, ct, dataScope);
        if (manage && venue is null && !await LateEntrySql.IsStandaloneSchema(c, ct))
            throw new PaymentException(403, "Tài khoản chưa được phân công cơ sở.");
        try
        {
            await LateEntrySql.RequireSchema(c, null, ct);
        }
        catch (PaymentException e)when (e.StatusCode == 503 && !manage)
        {
            return[];
        }

        return await Read(c, venue, session, manage, ct);
    }
}
