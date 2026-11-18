using be_da.Presentation.Filters;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Domain.Entities;

namespace be_da.Presentation.Controllers;
[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController(IProfileService profileStore, IAntiforgery antiforgery) : ControllerBase
{
    // UC-03: View Personal Profile
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var profile = await profileStore.GetProfileAsync(userId, cancellationToken);
        return profile is null ? NotFound(new { code = "profile_not_found" }) : Ok(profile);
    }

    // Serve uploaded player avatar files
    [AllowAnonymous]
    [HttpGet("avatars/{fileName}")]
    public IActionResult GetAvatarFile([FromRoute] string fileName)
    {
        var file = profileStore.FindAvatar(fileName);
        return file is null ? NotFound() : PhysicalFile(file.Path, file.ContentType);
    }

    // UC-05: View Public Player Profile (Badminton & Pickleball bands/scores + Friendship state)
    [AllowAnonymous]
    [HttpGet("players/{playerId:long}")]
    [ProducesResponseType(typeof(PublicPlayerProfileDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicPlayerProfile([FromRoute] long playerId, CancellationToken cancellationToken)
    {
        var viewerUserId = TryGetUserId(out var uid) ? uid : (long? )null;
        var profile = await profileStore.GetPublicPlayerProfileAsync(playerId, viewerUserId, cancellationToken);
        return profile is null ? NotFound(new { code = "player_not_found", message = "Không tìm thấy hồ sơ người chơi." }) : Ok(profile);
    }

    [HttpGet("sports")]
    [ProducesResponseType(typeof(IReadOnlyList<SportProfileItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySportProfiles(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var sports = await profileStore.GetSportProfilesAsync(userId, cancellationToken);
        return sports is null ? NotFound(new { code = "profile_not_found" }) : Ok(sports);
    }

    // UC-03: Edit Personal Profile (FullName, DateOfBirth, Gender, AvatarUrl / Local Image Upload, Bio)
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        return (await profileStore.UpdateProfileAsync(userId, request, cancellationToken)).ToResponse();
    }

    [HttpPut("sports/{sportCode}")]
    [ProducesResponseType(typeof(SportProfileItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSportProfile([FromRoute] string sportCode, [FromBody] UpdateSportProfileRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId))
            return Unauthorized();
        var result = await profileStore.UpdateSportProfileAsync(userId, sportCode, request, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "sport_not_found" => NotFound(new { code = result.ErrorCode, message = result.Message }),
                "confirmed_rating_cannot_be_overwritten" => Conflict(new { code = result.ErrorCode, message = result.Message }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })};
        }

        return Ok(result.Profile);
    }

    private bool TryGetUserId(out long userId)
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(rawId, out userId);
    }

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
