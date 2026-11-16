using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TKSC.Presentation.Controllers;
[ApiController, Authorize]
[Route("api/notifications")]
public class NotificationsController(INotificationService service, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await service.ListAsync(UserId, ct));
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token" });
        }

        return await service.MarkReadAsync(UserId, id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token" });
        }

        await service.MarkAllReadAsync(UserId, ct);
        return NoContent();
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no";
        string? previous = null;
        // Bounded connections force cookie revalidation on reconnect; account status is also rechecked below.
        var until = DateTimeOffset.UtcNow.AddMinutes(5);
        try
        {
            await Response.WriteAsync("retry: 3000\n\n", ct);
            await Response.Body.FlushAsync(ct);
            while (!ct.IsCancellationRequested && DateTimeOffset.UtcNow < until)
            {
                var payload = await service.StreamSnapshotAsync(UserId, ct);
                if (payload is null)
                    break;
                if (payload != previous)
                {
                    await Response.WriteAsync($"event: notifications\ndata: {payload}\n\n", ct);
                    previous = payload;
                }

                await Response.WriteAsync(": keepalive\n\n", ct);
                await Response.Body.FlushAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
        }
    }
}
