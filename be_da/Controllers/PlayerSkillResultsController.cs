using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.DbContext;
using TKSC.Domain.Assessments;

namespace TKSC.Controllers;

[ApiController]
[Authorize(Roles = "player,Player")]
[Route("api/v1/skill/my-results")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlayerSkillResultsController(Sep490Context db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] long sportId, CancellationToken ct)
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        // Deliberately project only published values; never serialize entity graphs or internal evidence.
        return Ok(await db.PlayerRatingHistories.AsNoTracking()
            .Where(h => h.PlayerSportProfile.Player.UserId == userId && h.PlayerSportProfile.SportId == sportId)
            .Where(RatingPublicationRules.OfficialHistory)
            .OrderByDescending(h => h.Id)
            .Select(h => new { h.Id, h.AssessmentId, Score = h.NewScore, Band = h.NewBand,
                SportId = h.PlayerSportProfile.SportId }).ToListAsync(ct));
    }
}
