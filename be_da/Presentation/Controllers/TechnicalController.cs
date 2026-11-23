using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController, Route("api/technical"), Authorize(Roles = "SystemAdmin"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TechnicalController(be_da.Application.Interfaces.Services.ITechnicalSettingsService service, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet] public Task<object> Read(CancellationToken ct) => service.ReadAsync(ct);
    [HttpGet("incidents")] public Task<IReadOnlyList<TechnicalIncident>> Incidents(CancellationToken ct) => service.IncidentsAsync(ct);
    [HttpPut("settings/{key}")] public Task<IActionResult> Save(string key, TechnicalSettingChange request, CancellationToken ct) => Mutate(actor => service.SaveAsync(key, request, actor, ct));
    [HttpPost("incidents")] public Task<IActionResult> CreateIncident(TechnicalIncidentChange request, CancellationToken ct) => Mutate(actor => service.SaveIncidentAsync(null, request, actor, ct));
    [HttpPut("incidents/{id:long}")] public Task<IActionResult> UpdateIncident(long id, TechnicalIncidentChange request, CancellationToken ct) => Mutate(actor => service.SaveIncidentAsync(id, request, actor, ct));
    private async Task<IActionResult> Mutate(Func<long, Task> action) {
        try { await antiforgery.ValidateRequestAsync(HttpContext); } catch (AntiforgeryValidationException) { return BadRequest(new { message = "Phiên bảo mật đã thay đổi." }); }
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Unauthorized();
        await action(actor); return NoContent();
    }
}
