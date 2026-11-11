using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be_da.Application.Abstractions;
using be_da.Application.Profiles;
using be_da.Domain.Entities;

namespace be_da.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController(IProfileStore profileStore, IAntiforgery antiforgery) : ControllerBase
{
    private static readonly string AvatarDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "avatars");

    // UC-03: View Personal Profile
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var profile = await profileStore.GetProfileAsync(userId, cancellationToken);
        return profile is null ? NotFound(new { code = "profile_not_found" }) : Ok(profile);
    }

    // Serve uploaded player avatar files
    [AllowAnonymous]
    [HttpGet("avatars/{fileName}")]
    public IActionResult GetAvatarFile([FromRoute] string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) return NotFound();

        var fullPath = Path.Combine(AvatarDirectory, safeName);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var ext = Path.GetExtension(safeName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        return PhysicalFile(fullPath, contentType);
    }

    // UC-05: View Public Player Profile (Badminton & Pickleball bands/scores + Friendship state)
    [AllowAnonymous]
    [HttpGet("players/{playerId:long}")]
    [ProducesResponseType(typeof(PublicPlayerProfileDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicPlayerProfile(
        [FromRoute] long playerId,
        CancellationToken cancellationToken)
    {
        var viewerUserId = TryGetUserId(out var uid) ? uid : (long?)null;
        var profile = await profileStore.GetPublicPlayerProfileAsync(playerId, viewerUserId, cancellationToken);
        return profile is null
            ? NotFound(new { code = "player_not_found", message = "Không tìm thấy hồ sơ người chơi." })
            : Ok(profile);
    }

    [HttpGet("sports")]
    [ProducesResponseType(typeof(IReadOnlyList<SportProfileItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySportProfiles(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

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
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { code = "invalid_full_name", message = "Họ và tên không được để trống." });

        var trimmedName = request.FullName.Trim();
        if (trimmedName.Length is < 2 or > 100)
            return BadRequest(new { code = "invalid_full_name", message = "Họ và tên phải có độ dài từ 2 đến 100 ký tự." });

        var todayVn = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        if (request.DateOfBirth.HasValue && (request.DateOfBirth.Value > todayVn || request.DateOfBirth.Value < new DateOnly(1920, 1, 1)))
            return BadRequest(new { code = "invalid_date_of_birth", message = "Ngày sinh không hợp lệ (không được ở tương lai hoặc trước năm 1920)." });

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var g = request.Gender.Trim();
            if (!string.Equals(g, "Male", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Female", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Other", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Nam", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Nữ", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Nu", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(g, "Khác", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { code = "invalid_gender", message = "Giới tính không hợp lệ (Male, Female, Other)." });
            }
        }

        if (request.Bio is not null && request.Bio.Trim().Length > 500)
            return BadRequest(new { code = "invalid_bio", message = "Giới thiệu bản thân không được vượt quá 500 ký tự." });

        var normalizedRequest = request with { FullName = trimmedName };
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            var rawAvatar = request.AvatarUrl.Trim();
            if (rawAvatar.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var savedUrl = await TrySaveDataUrlAvatarAsync(userId, rawAvatar, cancellationToken);
                if (savedUrl is null)
                {
                    return BadRequest(new { code = "invalid_avatar_url", message = "Ảnh đại diện tải lên không hợp lệ." });
                }
                normalizedRequest = normalizedRequest with { AvatarUrl = savedUrl };
            }
            else if (rawAvatar.StartsWith("/api/profile/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                normalizedRequest = normalizedRequest with { AvatarUrl = rawAvatar };
            }
            else if (!Uri.TryCreate(rawAvatar, UriKind.Absolute, out var uri)
                     || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest(new { code = "invalid_avatar_url", message = "Avatar URL must be a valid HTTP or HTTPS URL." });
            }
        }

        var updated = await profileStore.UpdateProfileAsync(userId, normalizedRequest, cancellationToken);
        return updated is null ? NotFound(new { code = "profile_not_found" }) : Ok(updated);
    }

    [HttpPut("sports/{sportCode}")]
    [ProducesResponseType(typeof(SportProfileItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateSportProfile(
        [FromRoute] string sportCode,
        [FromBody] UpdateSportProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!request.Score.HasValue || request.Score.Value < 0 || request.Score.Value > 100)
            return BadRequest(new { code = "invalid_score", message = "Score must be between 0 and 100." });

        var result = await profileStore.UpdateSportProfileAsync(userId, sportCode, request, cancellationToken);
        if (!result.Success)
        {
            return result.ErrorCode switch
            {
                "sport_not_found" => NotFound(new { code = result.ErrorCode, message = result.Message }),
                "confirmed_rating_cannot_be_overwritten" => Conflict(new { code = result.ErrorCode, message = result.Message }),
                _ => BadRequest(new { code = result.ErrorCode, message = result.Message })
            };
        }

        return Ok(result.Profile);
    }

    private static async Task<string?> TrySaveDataUrlAvatarAsync(long userId, string dataUrl, CancellationToken cancellationToken)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex <= 0) return null;

        var header = dataUrl[..commaIndex].ToLowerInvariant();
        var base64Data = dataUrl[(commaIndex + 1)..];
        if (!header.Contains(";base64")) return null;

        var ext = header.Contains("image/png") ? ".png"
            : header.Contains("image/webp") ? ".webp"
            : header.Contains("image/gif") ? ".gif"
            : ".jpg";

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch
        {
            return null;
        }

        if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024) return null;

        Directory.CreateDirectory(AvatarDirectory);
        var fileName = $"u{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        var fullPath = Path.Combine(AvatarDirectory, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        return $"/api/profile/avatars/{fileName}";
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
