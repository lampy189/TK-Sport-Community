using System.Security.Claims;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Referee,Staff")]
[Route("api/shifts")]
public sealed class ShiftsController(IShiftService service, IDataAccessContext scope, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        if (scope.RestrictedVenueId is not long venue || !long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            return Forbid();
        return Ok(await service.MyShiftsAsync(venue, actor, scope.Role, ct));
    }

    [HttpPost("{sessionId:long}/accept")]
    public async Task<IActionResult> Accept(long sessionId, CancellationToken ct)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code = "invalid_csrf_token" }); }
        if (scope.RestrictedVenueId is not long venue || !long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            return Forbid();
        return (await service.AcceptShiftAsync(venue, actor, scope.Role, sessionId, ct)).ToResponse();
    }
}
