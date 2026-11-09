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
        ISNULL(f.max_tickets,0)-ISNULL(a.late_count,0),ISNULL(f.price_vnd,0),ISNULL(f.is_open,0),ISNULL(f.revision,0),s.status
      FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id JOIN dbo.venues v ON v.id=c.venue_id
      JOIN dbo.sports sp ON sp.id=s.sport_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
      LEFT JOIN dbo.late_entry_offers f ON f.session_id=s.id
      OUTER APPLY(
        SELECT SUM(CASE WHEN LOWER(t.status)='checkedin' THEN 1 ELSE 0 END) playing,
          SUM(CASE WHEN LOWER(t.status) IN ('confirmed','valid','paid') THEN 1 ELSE 0 END) confirmed,
          SUM(CASE WHEN LOWER(t.status) IN ('pending','reserved') THEN 1 ELSE 0 END) holds,
          SUM(CASE WHEN lt.ticket_id IS NOT NULL THEN 1 ELSE 0 END) late_count
        FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
        LEFT JOIN dbo.late_entry_tickets lt ON lt.ticket_id=t.id
        WHERE t.session_id=s.id AND (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
          (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now))
      ) a
      WHERE v.is_active=1 AND (@venue IS NULL OR c.venue_id=@venue) AND (@session IS NULL OR s.id=@session)
      """;
    private async Task<List<(LateEntryOffer Offer,string Status)>> Read(SqlConnection c,SqlTransaction? tx,long? venue,long? session,CancellationToken ct)
    {
        await using var cmd=new SqlCommand(Select,c,tx);
        cmd.Parameters.AddWithValue("@now",clock.GetUtcNow());
        cmd.Parameters.Add("@venue",SqlDbType.BigInt).Value=(object?)venue??DBNull.Value;
        cmd.Parameters.Add("@session",SqlDbType.BigInt).Value=(object?)session??DBNull.Value;
        var result=new List<(LateEntryOffer,string)>();
        await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))
        {
            var date=DateOnly.FromDateTime(r.GetDateTime(6));var capacity=Math.Min((int)r.GetInt16(9),8);
            var playing=r.GetInt32(10);var confirmed=r.GetInt32(11);var holds=r.GetInt32(12);
            var available=Math.Max(0,capacity-playing-confirmed-holds);
            result.Add((new(r.GetInt64(0),r.GetInt64(1),r.GetString(2),r.GetString(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),
                LateEntryRules.End(date,TimeOnly.FromTimeSpan(r.GetTimeSpan(7))),LateEntryRules.End(date,TimeOnly.FromTimeSpan(r.GetTimeSpan(8))),
                capacity,playing,confirmed,holds,available,Math.Min(available,Math.Max(0,r.GetInt32(13))),r.GetInt32(14),r.GetBoolean(15),r.GetInt64(16)),r.GetString(17)));
        }
        return result;
    }
    public async Task<IReadOnlyList<LateEntryOffer>> List(long? venue,long? session,bool manage,CancellationToken ct)
    {
        await using var c=Connection();await SportCenterConnection.OpenAsync(c,ct);
        try{await LateEntrySql.RequireSchema(c,null,ct);}catch(PaymentException e) when(e.StatusCode==503&&!manage){return [];}
        var now=clock.GetUtcNow();var rows=await Read(c,null,venue,session,ct);
        return rows.Where(x=>LateEntryRules.Running(x.Offer.StartsAt,x.Offer.EndsAt,x.Status,now)
          && (manage || x.Offer.IsOpen&&x.Offer.Remaining>0)).Select(x=>x.Offer).ToList();
    }
    public async Task<LateEntryOffer> Publish(long id,long venue,long actor,PublishLateEntryRequest input,bool stop,CancellationToken ct)
    {
        await using var c=Connection();await SportCenterConnection.OpenAsync(c,ct);await LateEntrySql.RequireSchema(c,null,ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        await using(var lockCmd=new SqlCommand("SELECT s.id FROM dbo.sessions s WITH(UPDLOCK,HOLDLOCK) JOIN dbo.courts c ON c.id=s.court_id WHERE s.id=@id AND c.venue_id=@venue",c,tx))
        {
            lockCmd.Parameters.AddWithValue("@id",id);lockCmd.Parameters.AddWithValue("@venue",venue);
            if(await lockCmd.ExecuteScalarAsync(ct) is null)throw new PaymentException(404,"Không tìm thấy buổi trong cơ sở được phân công.");
        }
        var row=(await Read(c,tx,venue,id,ct)).SingleOrDefault();var offer=row.Offer;
        if(offer is null)throw new PaymentException(404,"Không tìm thấy buổi đang hoạt động.");
        if(input.ExpectedRevision!=offer.Revision)throw new PaymentException(409,"Đợt rao vừa thay đổi. Tải lại trước khi cập nhật.");
        if(!stop)LateEntryRules.CheckSale(offer.StartsAt,offer.EndsAt,row.Status,clock.GetUtcNow(),input.Seats,offer.Available,input.PriceVnd);
        if(stop&&offer.Revision==0)throw new PaymentException(409,"Buổi này chưa có đợt mở bán.");
        await using var cmd=new SqlCommand("""
          DECLARE @sold int=(SELECT COUNT(*) FROM dbo.late_entry_tickets lt JOIN dbo.tickets t ON t.id=lt.ticket_id
            JOIN dbo.social_booking_orders o ON o.id=t.order_id WHERE lt.session_id=@id
            AND (LOWER(t.status) IN ('confirmed','checkedin','valid','paid') OR
             (LOWER(t.status) IN ('pending','reserved') AND o.expires_at>@now)));
          IF EXISTS(SELECT 1 FROM dbo.late_entry_offers WHERE session_id=@id)
            UPDATE dbo.late_entry_offers SET max_tickets=CASE WHEN @stop=1 THEN max_tickets ELSE @sold+@seats END,
              price_vnd=CASE WHEN @stop=1 THEN price_vnd ELSE @price END,is_open=CASE WHEN @stop=1 THEN 0 ELSE 1 END,
              revision=revision+1,updated_by=@actor,updated_at=@now WHERE session_id=@id;
          ELSE INSERT dbo.late_entry_offers(session_id,max_tickets,price_vnd,is_open,revision,updated_by,updated_at)
            VALUES(@id,@seats,@price,1,1,@actor,@now);
          INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value)
            VALUES(@actor,CASE WHEN @stop=1 THEN 'late_entry_stopped' ELSE 'late_entry_published' END,'Session',@id,@detail);
          """,c,tx);
        cmd.Parameters.AddWithValue("@id",id);cmd.Parameters.AddWithValue("@actor",actor);cmd.Parameters.AddWithValue("@now",clock.GetUtcNow());
        cmd.Parameters.AddWithValue("@stop",stop);cmd.Parameters.AddWithValue("@seats",input.Seats);cmd.Parameters.AddWithValue("@price",input.PriceVnd);
        cmd.Parameters.AddWithValue("@detail",$"seats={input.Seats};price={input.PriceVnd};ends={offer.EndsAt:O}");
        await cmd.ExecuteNonQueryAsync(ct);var updated=(await Read(c,tx,venue,id,ct)).Single().Offer;
        await tx.CommitAsync(ct);return updated;
    }
}
