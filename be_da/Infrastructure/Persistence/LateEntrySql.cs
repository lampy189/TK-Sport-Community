using System.Data;
using Microsoft.Data.SqlClient;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Domain.Payments;

namespace be_da.Infrastructure.Persistence;

public static class LateEntrySql
{
    // These checks are shared by holds and payment revalidation, under the session lock.
    public const string PlayerEligible = """
        SELECT CASE WHEN EXISTS (
          SELECT 1 FROM dbo.players p JOIN dbo.users u ON u.id=p.user_id
          JOIN dbo.sessions s ON s.id=@session JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
          WHERE p.id=@player AND LOWER(u.status)='active' AND u.deleted_at IS NULL
          AND (s.band IS NULL OR LTRIM(RTRIM(s.band))='' OR EXISTS(SELECT 1 FROM dbo.session_exceptions x
                 WHERE x.player_id=p.id AND x.session_id=s.id AND x.approved_by IS NOT NULL) OR EXISTS (
            SELECT 1 FROM dbo.player_sport_profiles sp WHERE sp.player_id=p.id AND sp.sport_id=s.sport_id
            AND sp.current_score IS NOT NULL AND LOWER(sp.rating_status) NOT IN ('pendingsurvey','unrated')
            AND COALESCE(NULLIF(UPPER(LTRIM(RTRIM(sp.current_band))),''),
              CASE WHEN sp.current_score<20 THEN 'B1' WHEN sp.current_score<40 THEN 'B2'
                   WHEN sp.current_score<60 THEN 'B3' WHEN sp.current_score<80 THEN 'B4' ELSE 'B5' END)=UPPER(LTRIM(RTRIM(s.band)))))
          AND NOT EXISTS (
            SELECT 1 FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
            JOIN dbo.sessions other ON other.id=t.session_id JOIN dbo.time_slots ot ON ot.id=other.time_slot_id
            WHERE t.player_id=p.id AND t.session_id<>s.id AND other.session_date=s.session_date
            AND LOWER(other.status) NOT IN ('cancelled','completed','suspended')
            AND (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
                 (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))
            AND ot.start_time<ts.end_time AND ot.end_time>CONVERT(time,SWITCHOFFSET(@now,'+07:00'))
          )
        ) THEN 1 ELSE 0 END
        """;
    public const string OccupiedPredicate = """
        (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
         (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))
        """;
    public static async Task RequireSchema(SqlConnection c, SqlTransaction? tx, CancellationToken ct)
    {
        await using var cmd=new SqlCommand("""
          SELECT CASE WHEN OBJECT_ID('dbo.late_entry_offers','U') IS NOT NULL
            AND OBJECT_ID('dbo.late_entry_tickets','U') IS NOT NULL
            AND COL_LENGTH('dbo.users','venue_id') IS NOT NULL
            AND OBJECT_ID('dbo.revenue_entries','U') IS NOT NULL
            AND EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID('dbo.TR_payments_revenue') AND is_disabled=0)
            AND EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation' AND is_enabled=1)
          THEN 1 ELSE 0 END
          """,c,tx);
        if(Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))!=1)
            throw new PaymentException(503,"Vé giờ lẻ chưa được kích hoạt. Cần nâng cấp Sport Center 010–012 và 014-late-entry-sales.sql.");
    }
    public static async Task<bool> Eligible(SqlConnection c,SqlTransaction? tx,long session,long player,DateTimeOffset now,CancellationToken ct)
    {
        await using var cmd=new SqlCommand(PlayerEligible,c,tx);
        cmd.Parameters.AddWithValue("@session",session);cmd.Parameters.AddWithValue("@player",player);cmd.Parameters.AddWithValue("@now",now);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==1;
    }
    public static async Task ValidatePayment(SqlConnection c,SqlTransaction tx,Ticket ticket,DateTimeOffset now,CancellationToken ct=default)
    {
        if(!LateEntryRules.IsLate(ticket.TicketCode))return;
        await using var cmd=new SqlCommand($"""
          SELECT lt.ends_at,s.session_date,ts.start_time,ts.end_time,s.status,s.capacity,
            (SELECT COUNT(*) FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
             WHERE t.session_id=s.id AND {OccupiedPredicate})
          FROM dbo.late_entry_tickets lt JOIN dbo.sessions s ON s.id=lt.session_id
          JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
          JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
          WHERE lt.ticket_id=@ticket AND v.is_active=1
          """,c,tx);
        cmd.Parameters.AddWithValue("@ticket",ticket.Id);cmd.Parameters.AddWithValue("@now",now);
        bool valid=false;
        await using(var reader=await cmd.ExecuteReaderAsync(ct))
        {
            if(await reader.ReadAsync(ct))
            {
                var end=reader.GetFieldValue<DateTimeOffset>(0);ticket.LateEntryEndsAt=end;
                var date=DateOnly.FromDateTime(reader.GetDateTime(1));
                valid=LateEntryRules.Running(LateEntryRules.End(date,TimeOnly.FromTimeSpan(reader.GetTimeSpan(2))),end,reader.GetString(4),now)
                  && end==LateEntryRules.End(date,TimeOnly.FromTimeSpan(reader.GetTimeSpan(3)))
                  && reader.GetInt32(6)<=Math.Min((int)reader.GetInt16(5),8);
            }
        }
        ticket.LateEntryEligible=valid && await Eligible(c,tx,ticket.SessionId,ticket.PlayerId,now,ct);
    }
}
