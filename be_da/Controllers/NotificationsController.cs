using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.DbContext;

namespace TKSC.Controllers;

[ApiController, Authorize]
[Route("api/notifications")]
public class NotificationsController(Sep490Context db, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        var query = db.Notifications.AsNoTracking().Where(n => n.UserId == UserId && n.Channel == "web");
        var items = await query.OrderByDescending(n => n.Id).Take(50)
            .Select(n => new { n.Id, n.Title, n.ReferenceId, n.ReferenceType, n.Status }).ToListAsync(ct);
        return Ok(new { items, unread = await query.CountAsync(n => n.Status == "unread", ct), cursor = items.FirstOrDefault()?.Id ?? 0 });
    }
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        var changed = await db.Notifications.Where(n => n.Id == id && n.UserId == UserId && n.Channel == "web")
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, "read"), ct);
        return changed == 0 ? NotFound() : NoContent();
    }
    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        await db.Notifications.Where(n => n.UserId == UserId && n.Channel == "web" && n.Status == "unread")
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Status, "read"), ct);
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
                if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == UserId && u.DeletedAt == null && u.Status.ToLower() == "active", ct)) break;
                var query = db.Notifications.AsNoTracking().Where(n => n.UserId == UserId && n.Channel == "web");
                var items = await query.OrderByDescending(n => n.Id).Take(50)
                    .Select(n => new { id = n.Id, title = n.Title, referenceId = n.ReferenceId, referenceType = n.ReferenceType, status = n.Status }).ToListAsync(ct);
                var payload = JsonSerializer.Serialize(new { items, unread = await query.CountAsync(n => n.Status == "unread", ct) });
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
