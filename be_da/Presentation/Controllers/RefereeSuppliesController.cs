using be_da.Presentation.Filters;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Validators;
using TKSC.Presentation.Middlewares;

namespace be_da.Presentation.Controllers;
// UC-42 (RP3 FR-22.4): the assigned Referee sees issued supplies and records observed use.
// Staff still confirms the physical return through SessionSuppliesController.
[ApiController]
[Route("api/stock/referee-supplies")]
[Authorize(Roles = "Referee")]
[ValidatedRequest]
public sealed class RefereeSuppliesController(ISessionSupplyService store, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplySession>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var refereeId))
            return Unauthorized();
        return (await store.GetSessionsAsync(date, refereeId, cancellationToken)).ToResponse();
    }

    [HttpGet("{sessionId:long:min(1)}")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(long sessionId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var refereeId))
            return Unauthorized();
        // Unassigned sessions look missing so a referee cannot browse other sessions.
        var details = await store.IsRefereeAssignedAsync(sessionId, refereeId, cancellationToken) ? await store.GetDetailsAsync(sessionId, cancellationToken) : null;
        return details is null ? NotFound(new { code = "SESSION_NOT_FOUND", message = "Không tìm thấy buổi chơi được phân công." }) : Ok(details);
    }

    [HttpPost("{sessionId:long:min(1)}/products/{productId:long:min(1)}/usage")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportUse(long sessionId, long productId, [FromBody] ReportSupplyUseRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var refereeId))
            return Unauthorized();
        var result = await store.ReportUseAsync(sessionId, productId, request, refereeId, cancellationToken);
        return result.Success ? Ok(result.Details) : result.ErrorCode == "VALIDATION_FAILED" ? BadRequest(new { code = result.ErrorCode, message = result.Message }) : result.ErrorCode is "NOT_ASSIGNED" or "ALLOCATION_NOT_FOUND" ? NotFound(new { code = result.ErrorCode, message = result.Message }) : Conflict(new { code = result.ErrorCode, message = result.Message });
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
