using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;
using be_da.Application.Stock;
<<<<<<< HEAD
=======
using TKSC.Controllers.Middlewares;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

namespace be_da.Controllers;

// UC-42 (RP3 FR-22.4): the assigned Referee sees issued supplies and records observed use.
// Staff still confirms the physical return through SessionSuppliesController.
[ApiController]
[Route("api/stock/referee-supplies")]
[Authorize(Roles = "Referee")]
<<<<<<< HEAD
=======
[ValidatedRequest]
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
public sealed class RefereeSuppliesController(ISessionSupplyStore store, IAntiforgery antiforgery, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplySession>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var refereeId)) return Unauthorized();
<<<<<<< HEAD
        var day = date ?? DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        return Ok(await store.GetSessionsAsync(day, refereeId, cancellationToken));
    }

    [HttpGet("{sessionId:long}")]
=======
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        var day = date ?? today;
        if (SupplyRules.ValidateDay(day, today) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });
        return Ok(await store.GetSessionsAsync(day, refereeId, cancellationToken));
    }

    [HttpGet("{sessionId:long:min(1)}")]
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(long sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var refereeId)) return Unauthorized();
        // Unassigned sessions look missing so a referee cannot browse other sessions.
        var details = await store.IsRefereeAssignedAsync(sessionId, refereeId, cancellationToken)
            ? await store.GetDetailsAsync(sessionId, cancellationToken)
            : null;
        return details is null
            ? NotFound(new { code = "SESSION_NOT_FOUND", message = "Không tìm thấy buổi chơi được phân công." })
            : Ok(details);
    }

<<<<<<< HEAD
    [HttpPost("{sessionId:long}/products/{productId:long}/usage")]
=======
    [HttpPost("{sessionId:long:min(1)}/products/{productId:long:min(1)}/usage")]
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportUse(long sessionId, long productId, [FromBody] ReportSupplyUseRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var refereeId)) return Unauthorized();
        if (SupplyRules.ValidateUse(request) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });
        var result = await store.ReportUseAsync(sessionId, productId, request, refereeId, cancellationToken);
        return result.Success
            ? Ok(result.Details)
            : result.ErrorCode is "NOT_ASSIGNED" or "ALLOCATION_NOT_FOUND"
                ? NotFound(new { code = result.ErrorCode, message = result.Message })
                : Conflict(new { code = result.ErrorCode, message = result.Message });
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
