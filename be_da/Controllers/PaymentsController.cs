using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Interfaces;
using TKSC.Application.DTOs;

namespace TKSC.Controllers;

[ApiController]
[Route("api/v1/commerce/payment")]
public class PaymentsController(IPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
    private async Task<bool> HasValidAppRequest()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return false; }
        // Custom header + same-origin cookie; do not enable credentialed CORS for arbitrary origins.
        if (Request.Headers["X-TKSC-Request"] != "1") throw new PaymentException(400, "Missing application request header.");
        return true;
    }
    [Authorize]
    [HttpGet("orders/{id:long}")]
    public async Task<IActionResult> Display(long id) => Ok(await service.DisplayAsync(id, UserId));
    [Authorize]

    [HttpPost("orders/{id:long}/process")]
    public async Task<IActionResult> Process(long id)
    {
        if (!await HasValidAppRequest()) return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        return Ok(await service.ProcessAsync(id, UserId));
    }
    [Authorize]

    [HttpPost("orders/{id:long}/refresh")]
    public async Task<IActionResult> Refresh(long id)
    {
        if (!await HasValidAppRequest()) return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        return Ok(await service.RefreshAsync(id, UserId));
    }
    [Authorize]

    [HttpPost("orders/{id:long}/review")]
    public async Task<IActionResult> Flag(long id, ReviewRequest request)
    {
        if (!await HasValidAppRequest()) return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        await service.FlagAsync(id, UserId, request.Reason);
        return Ok(new { status = "review_required" });
    }
    [Authorize]
    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets() => Ok(await service.TicketsAsync(UserId));
    [Authorize]
    [HttpPost("tickets/{id:long}/cancel")]
    public async Task<IActionResult> CancelTicket(long id)
    {
        if (!await HasValidAppRequest()) return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        return Ok(await service.CancelTicketAsync(id, UserId));
    }
    [Authorize(Roles = "staff,manager,Staff,Manager")]
    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews() => Ok(await service.ReviewsAsync());
    [AllowAnonymous]
    [HttpPost("webhook/payos")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Webhook([FromBody] JsonElement payload)
    {
        await service.HandleWebhookAsync(payload.GetRawText());
        return Ok(new { code = "00", desc = "success" });
    }
    public record ReviewRequest(string? Reason);
}
