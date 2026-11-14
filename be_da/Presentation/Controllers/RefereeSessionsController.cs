using TKSC.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
[ApiController]
[Authorize(Roles = "referee,Referee,manager,Manager")]
[Route("api/v1/referee")]
public class RefereeSessionsController(ITicketCheckInService service) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAssignedSessions()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();
        var sessions = await service.GetAssignedSessionsAsync(actorId);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId:long}/attendees")]
    public async Task<IActionResult> GetAttendees(long sessionId)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();
        return Ok(await service.GetSessionRosterAsync(sessionId, actorId));
    }
}
