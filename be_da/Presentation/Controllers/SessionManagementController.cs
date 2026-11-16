using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Authorization;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Manager,manager")]
[Route("api/sessions/manage")]
public sealed class SessionManagementController(ISessionManagementService service, IDataAccessContext dataScope, IAntiforgery antiforgery) : ControllerBase
{
    private long? VenueId => CenterSelection.CurrentVenue ?? dataScope.VenueId;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (VenueId is not long venueId) return BadRequest(new { code = "CENTER_REQUIRED", message = "Chưa có cơ sở được phân công." });
        return Ok(await service.GetSessionsAsync(venueId, ct));
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken ct)
    {
        if (VenueId is not long venueId) return BadRequest(new { code = "CENTER_REQUIRED", message = "Chưa có cơ sở được phân công." });
        return Ok(await service.GetOptionsAsync(venueId, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveManagedSessionRequest request, CancellationToken ct)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (VenueId is not long venueId || !ActorId(out var actorId)) return Unauthorized();
        var result = await service.SaveAsync(venueId, actorId, null, request, ct);
        return result.Outcome == OperationOutcome.Created ? Created("/api/sessions/manage", result.Value) : result.ToResponse();
    }

    [HttpPut("{sessionId:long}")]
    public async Task<IActionResult> Update(long sessionId, SaveManagedSessionRequest request, CancellationToken ct)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (VenueId is not long venueId || !ActorId(out var actorId)) return Unauthorized();
        return (await service.SaveAsync(venueId, actorId, sessionId, request, ct)).ToResponse();
    }

    [HttpPut("{sessionId:long}/assignments")]
    public async Task<IActionResult> Assign(long sessionId, SessionAssignmentsRequest request, CancellationToken ct)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (VenueId is not long venueId || !ActorId(out var actorId)) return Unauthorized();
        return (await service.SetAssignmentsAsync(venueId, actorId, sessionId, request, ct)).ToResponse();
    }

    [HttpPost("{sessionId:long}/open")]
    public Task<IActionResult> Open(long sessionId, CancellationToken ct) => ChangeStatus(sessionId, true, ct);

    [HttpPost("{sessionId:long}/close")]
    public Task<IActionResult> Close(long sessionId, CancellationToken ct) => ChangeStatus(sessionId, false, ct);

    private async Task<IActionResult> ChangeStatus(long sessionId, bool open, CancellationToken ct)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (VenueId is not long venueId || !ActorId(out var actorId)) return Unauthorized();
        return (await service.SetStatusAsync(venueId, actorId, sessionId, open, ct)).ToResponse();
    }

    private bool ActorId(out long actorId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out actorId);

    private async Task<bool> HasValidCsrf()
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }
}