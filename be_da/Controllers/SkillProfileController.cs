using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;
using be_da.Application.Skills;

namespace be_da.Controllers;

[ApiController]
[Route("api/skills")]
public sealed class SkillProfileController(ISkillProfileStore skillStore, IAntiforgery antiforgery) : ControllerBase
{
    // UC-04 / UC-06: Get active sport rubrics and 5 scoring dimensions
    [AllowAnonymous]
    [HttpGet("rubrics")]
    [ProducesResponseType(typeof(IReadOnlyList<SportRubricDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRubrics([FromQuery] long? sportId, CancellationToken cancellationToken)
    {
        var rubrics = await skillStore.GetActiveRubricsAsync(sportId, cancellationToken);
        return Ok(rubrics);
    }

    // UC-05 / UC-07 & UC-19 / UC-25: View player sport skill profiles, referee results, and rating history
    [Authorize(Roles = "Player")]
    [HttpGet("me")]
    [ProducesResponseType(typeof(PlayerSkillOverviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySkillProfiles(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var overview = await skillStore.GetPlayerSkillOverviewAsync(userId, cancellationToken);
        if (overview == null)
        {
            return NotFound(new { code = "PLAYER_NOT_FOUND", message = "Không tìm thấy hồ sơ người chơi." });
        }

        return Ok(overview);
    }

    // UC-04 / UC-06: Submit Initial Skill Self-Assessment Survey (SWL 1 Input -> PlayerSportProfiles)
    [Authorize(Roles = "Player")]
    [HttpPost("me/survey")]
    [ProducesResponseType(typeof(SubmitSkillSurveyResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitMySurvey([FromBody] SubmitSkillSurveyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await skillStore.SubmitSelfAssessmentAsync(userId, request, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "ALREADY_RATED" or "ALREADY_SURVEYED" => Conflict(new { code = result.ErrorCode, message = result.Message }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })
            };
        }

        return Ok(result);
    }

    private bool TryGetUserId(out long userId)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawId, out userId);
    }

    private async Task<bool> HasValidCsrf()
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}

