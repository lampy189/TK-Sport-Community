using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Controllers;

[ApiController]
[Authorize(Roles = "referee,Referee,manager,Manager")]
[Route("api/v1/referee")]
public class RefereeSessionsController(IRefereeSessionsService service) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAssignedSessions()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Unauthorized();

        var sessions = await service.GetAssignedSessionsAsync(actorId);
        return Ok(sessions);
    }
}
