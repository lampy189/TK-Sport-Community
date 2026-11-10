using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Sessions;
using be_da.Authorization;
using TKSC.Application.DTOs;
using TKSC.Domain.Payments;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlLateEntryStore(IConfiguration config,TimeProvider clock) : be_da.Application.Abstractions.ILateEntryStore
{
    private SqlConnection Connection()=>new(config.GetConnectionString("Sep490"));
    private const string Select = """
      SELECT s.id,c.venue_id,v.name,c.name,sp.name,s.band,s.session_date,ts.start_time,ts.end_time,
        s.capacity,ISNULL(a.playing,0),ISNULL(a.confirmed,0),ISNULL(a.holds,0),
        s.price_vnd,s.status
      FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
      JOIN dbo.sports sp ON sp.id=s.sport_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
      OUTER APPLY(
        SELECT SUM(CASE WHEN LOWER(t.status)='checkedin' THEN 1 ELSE 0 END) playing,
          SUM(CASE WHEN LOWER(t.status) IN ('confirmed','valid','paid') THEN 1 ELSE 0 END) confirmed,
          SUM(CASE WHEN LOWER(t.status) IN ('pending','reserved') THEN 1 ELSE 0 END) holds
        FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
        WHERE t.session_id=s.id AND (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
          (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))
      ) a
      WHERE v.is_active=1 AND (@venue IS NULL OR c.venue_id=@venue) AND (@session IS NULL OR s.id=@session)
      """;
    private async Task<List<LateEntryOffer>> Read(SqlConnection c,long? venue,long? session,bool manage,CancellationToken ct)
    {
        var now=clock.GetUtcNow();
        await using var cmd=new SqlCommand(Select,c);
        cmd.Parameters.AddWithValue("@now",now);
        cmd.Parameters.Add("@venue",SqlDbType.BigInt).Value=(object?)venue??DBNull.Value;
        cmd.Parameters.Add("@session",SqlDbType.BigInt).Value=(object?)session??DBNull.Value;
        var result=new List<LateEntryOffer>();
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))
        {
            var date=DateOnly.FromDateTime(r.GetDateTime(6));var capacity=Math.Min((int)r.GetInt16(9),8);
            var playing=r.GetInt32(10);var confirmed=r.GetInt32(11);var holds=r.GetInt32(12);
            var available=Math.Max(0,capacity-playing-confirmed-holds);
            var start=LateEntryRules.End(date,TimeOnly.FromTimeSpan(r.GetTimeSpan(7)));
            var end=LateEntryRules.End(date,TimeOnly.FromTimeSpan(r.GetTimeSpan(8)));
            var basePrice=r.GetInt32(13);
            var quote=LateEntryRules.Quote(start,end,r.GetString(14),now,basePrice);
            if(quote is null || (!manage&&available==0))continue;
            result.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetString(2),r.GetString(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),
                start,end,capacity,playing,confirmed,holds,available,available,quote.PriceVnd,available>0,quote.Revision,basePrice,quote.PricePercent));
        }
        return result;
    }
    public async Task<IReadOnlyList<LateEntryOffer>> List(long? venue,long? session,bool manage,CancellationToken ct)
    {
        await using var c=Connection();await SportCenterConnection.OpenAsync(c,ct);
        try{await LateEntrySql.RequireSchema(c,null,ct);}catch(PaymentException e) when(e.StatusCode==503&&!manage){return [];}
        return await Read(c,venue,session,manage,ct);
    }
}
