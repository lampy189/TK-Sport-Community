using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Domain.Assessments;

namespace TKSC.Presentation.Controllers;
[ApiController]
[Authorize(Roles = "player,Player")]
[Route("api/v1/skill/my-results")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlayerSkillResultsController(IPlayerSkillResultService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long sportId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        // Deliberately project only published values; never serialize entity graphs or internal evidence.
        return Ok(await service.ListAsync(userId, sportId, ct));
    }
}
