using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Presentation.Filters;

namespace be_da.Presentation.Controllers;
[ApiController, Authorize(Roles = "Owner,Manager,Staff,owner,manager,staff")]
[Route("api/v1/revenue"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RevenueController(IRevenueService service) : ControllerBase
{
    public static bool ValidRange(DateOnly from, DateOnly to) => be_da.Application.Services.RevenueService.ValidRange(from, to);
    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] long? venueId, CancellationToken ct)
    {
        if (!ValidRange(from, to))
            return BadRequest(new { message = "Chọn khoảng ngày hợp lệ, tối đa 366 ngày." });
        var owner = User.IsInRole("Owner") || User.IsInRole("owner");
        var assigned = HttpContext.Items["SportCenterId"] as long? ;
        if (!owner && (assigned is null || venueId is not null && venueId != assigned))
            return Forbid();
        if (!owner)
            venueId = assigned;
        return (await service.DailyAsync(from, to, venueId, ct)).ToResponse();
    }
}
