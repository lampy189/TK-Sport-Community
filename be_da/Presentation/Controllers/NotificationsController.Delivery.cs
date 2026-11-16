using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Interfaces.Services;
namespace TKSC.Presentation.Controllers;
public partial class NotificationsController
{
    [HttpGet("email")]
    public async Task<IActionResult> EmailStatus([FromServices] INotificationDeliveryService delivery, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await delivery.EmailsAsync(UserId, ct));
    }
    [HttpPost("email/{id:long}/retry")]
    public async Task<IActionResult> RetryEmail(long id, [FromServices] INotificationDeliveryService delivery, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        return StatusCode(await delivery.RetryAsync(UserId, id, ct));
    }
    public sealed record DeviceToken(string Token);
    [HttpPut("device")]
    public async Task<IActionResult> RegisterDevice(DeviceToken request, [FromServices] INotificationDeliveryService delivery, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 512) return BadRequest();
        await delivery.RegisterDeviceAsync(UserId, request.Token.Trim(), ct);
        return NoContent();
    }
    [HttpDelete("device")]
    public async Task<IActionResult> RemoveDevice(DeviceToken request, [FromServices] INotificationDeliveryService delivery, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        await delivery.RemoveDeviceAsync(UserId, request.Token, ct);
        return NoContent();
    }
}
