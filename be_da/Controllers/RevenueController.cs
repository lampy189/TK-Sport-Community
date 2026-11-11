using be_da.Application.Abstractions;
using be_da.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Controllers;

[ApiController, Authorize(Roles="Owner,Manager,Staff,owner,manager,staff")]
[Route("api/v1/revenue"), ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class RevenueController(IRevenueStore store) : ControllerBase
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
        var report=await store.GetDailyAsync(from,to,venueId,ct);
        if(!report.VenueExists) return NotFound(new{message="Không tìm thấy Sport Center."});
        var rows=report.Entries.ToDictionary(e=>e.Date,e=>new Day(e.Date,e.Social,e.Goods,e.Refunds));
        var days=Enumerable.Range(0,to.DayNumber-from.DayNumber+1).Select(i=>from.AddDays(i)).Select(d=>rows.GetValueOrDefault(d,new Day(d,0,0,0))).ToArray();
        var unposted=report.UnpostedPayments;
        return Ok(new{venueId,from,to,timeZone="Asia/Ho_Chi_Minh",days,totals=new{social=days.Sum(d=>d.Social),goods=days.Sum(d=>d.Goods),refunds=days.Sum(d=>d.Refunds),net=days.Sum(d=>d.Net)},unpostedPayments=unposted});
    }
}
