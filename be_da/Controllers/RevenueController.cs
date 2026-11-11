using be_da.Application.Abstractions;
using be_da.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace be_da.Controllers;

[ApiController, Authorize(Roles="Owner,Manager,Staff,owner,manager,staff")]
[Route("api/v1/revenue"), ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class RevenueController(IRevenueStore store, IConfiguration configuration) : ControllerBase
{
    public sealed record Day(DateOnly Date,long Social,long Goods,long Refunds) { public long Net=>Social+Goods-Refunds; }
    public static bool ValidRange(DateOnly from,DateOnly to)=>from!=default && to<DateOnly.MaxValue && to>=from && to.DayNumber-from.DayNumber<=365;
    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] DateOnly from,[FromQuery] DateOnly to,[FromQuery] long? venueId,CancellationToken ct)
    {
        if(!ValidRange(from,to)) return BadRequest(new{message="Chọn khoảng ngày hợp lệ, tối đa 366 ngày."});
        var owner=User.IsInRole("Owner")||User.IsInRole("owner");
        var assigned=HttpContext.Items["SportCenterId"] as long?;
        if(!owner && (assigned is null || venueId is not null && venueId!=assigned)) return Forbid();
        if(!owner) venueId=assigned;
        await using var connection=new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection,ct);
        if(venueId.HasValue)
        {
            await using var exists=new SqlCommand("SELECT COUNT(*) FROM dbo.venues WHERE id=@venue",connection);
            exists.Parameters.AddWithValue("@venue",venueId.Value);
            if((int)(await exists.ExecuteScalarAsync(ct))! ==0) return NotFound(new{message="Không tìm thấy Sport Center."});
        }
        await using var command=new SqlCommand("""
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
            """,connection);
        command.Parameters.AddWithValue("@start",new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)));
        command.Parameters.AddWithValue("@end",new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(7)));
        command.Parameters.Add("@venue",System.Data.SqlDbType.BigInt).Value=(object?)venueId??DBNull.Value;
        var rows=new Dictionary<DateOnly,Day>();
        await using var reader=await command.ExecuteReaderAsync(ct);
        while(await reader.ReadAsync(ct)) { 
            var day=DateOnly.FromDateTime(reader.GetDateTime(0)); 
            rows[day]=new(day,reader.GetInt64(1),reader.GetInt64(2),reader.GetInt64(3)); 
        }
        await reader.NextResultAsync(ct);
        await reader.ReadAsync(ct);var unposted=reader.GetInt64(0);
        var report=await store.GetDailyAsync(from,to,venueId,ct);
        if(!report.VenueExists) return NotFound(new{message="Không tìm thấy Sport Center."});
        rows=report.Entries.ToDictionary(e=>e.Date,e=>new Day(e.Date,e.Social,e.Goods,e.Refunds));
        var days=Enumerable.Range(0,to.DayNumber-from.DayNumber+1)
            .Select(i=>from.AddDays(i)).Select(d=>rows.GetValueOrDefault(d,new Day(d,0,0,0))).ToArray();
        unposted=report.UnpostedPayments;
        return Ok(new{venueId,from,to,timeZone="Asia/Ho_Chi_Minh",days,totals=new{social=days.Sum(d=>d.Social),goods=days.Sum(d=>d.Goods),refunds=days.Sum(d=>d.Refunds),net=days.Sum(d=>d.Net)},unpostedPayments=unposted});
    }
}
