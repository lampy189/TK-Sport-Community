using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;

namespace TKSC.Controllers;

[ApiController, Authorize]
[Route("api/notifications")]
public class NotificationsController(INotificationStore notifications, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var feed = await notifications.GetFeedAsync(UserId, ct);
        return Ok(new { items = feed.Items, unread = feed.Unread, cursor = feed.Cursor });
    }
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        return await notifications.MarkReadAsync(UserId, id, ct) ? NoContent() : NotFound();
    }
    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        await notifications.MarkAllReadAsync(UserId, ct);
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
                if (!await notifications.IsActiveUserAsync(UserId, ct)) break;
                var feed = await notifications.GetFeedAsync(UserId, ct);
                var items = feed.Items.Select(item => new
                {
                    id = item.Id,
                    title = item.Title,
                    referenceId = item.ReferenceId,
                    referenceType = item.ReferenceType,
                    status = item.Status
                });
                var payload = JsonSerializer.Serialize(new { items, unread = feed.Unread });
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
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
