using be_da.Application.Interfaces.Services;
using be_da.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController, Route("api/v1/business-reports"), Authorize(Roles="Owner,Manager,Staff,owner,manager,staff")]
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public sealed class BusinessReportsController(IBusinessReportService reports) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Read(DateOnly from, DateOnly to, long? venueId, CancellationToken ct)
    {
        if(!RevenueService.ValidRange(from,to)) return BadRequest(new { message="Chọn khoảng ngày hợp lệ, tối đa 366 ngày." });
        var report=await reports.ReadAsync(from,to,venueId,ct);
        return Ok(new { report, timeZone="Asia/Ho_Chi_Minh", totals=new {
            social=report.Centers.Sum(x=>x.Social),goods=report.Centers.Sum(x=>x.Goods),refunds=report.Centers.Sum(x=>x.Refunds),net=report.Centers.Sum(x=>x.Net)
        }});
    }
}
