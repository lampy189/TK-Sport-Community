using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TKSC.Application.Services;

namespace TKSC.Controllers;

[ApiController]
[Authorize(Roles = "referee,Referee,manager,Manager")]
[Route("api/v1/commerce/payment/check-in")]
public class TicketCheckInController(TicketCheckInService service, IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost("inspect")]
    public Task<IActionResult> Inspect(CheckInRequest request) => Execute(request, false);
    [HttpPost("confirm")]
    public Task<IActionResult> Confirm(CheckInRequest request) => Execute(request, true);
    private async Task<IActionResult> Execute(CheckInRequest request, bool confirm)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        { return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest();
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Unauthorized();
        return Ok(await service.ExecuteAsync(request.TicketCode, actorId,
            User.IsInRole("manager") || User.IsInRole("Manager"), confirm));
    }
    public record CheckInRequest(string? TicketCode);
}
