using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;
using be_da.Application.Stock;

namespace be_da.Controllers;

// UC-41 / UC-42 (RP3 SCR-22 FR-22.3, 22.4): Staff issues supplies to a session and closes them with actual returns.
[ApiController]
[Route("api/stock/session-supplies")]
[Authorize(Roles = "Staff,Manager")]
public sealed class SessionSuppliesController(ISessionSupplyStore store, IAntiforgery antiforgery, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplySession>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        // Venue days follow Vietnam time regardless of server timezone.
        var day = date ?? DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        return Ok(await store.GetSessionsAsync(day, null, cancellationToken));
    }

    [HttpGet("{sessionId:long}")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(long sessionId, CancellationToken cancellationToken)
    {
        var details = await store.GetDetailsAsync(sessionId, cancellationToken);
        return details is null
            ? NotFound(new { code = "SESSION_NOT_FOUND", message = "Không tìm thấy buổi chơi." })
            : Ok(details);
    }

    [HttpPost("{sessionId:long}/issue")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(long sessionId, [FromBody] IssueSupplyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId)) return Unauthorized();
        if (SupplyRules.ValidateIssue(request) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });
        return ToResponse(await store.IssueAsync(sessionId, request, staffUserId, cancellationToken));
    }

    [HttpPost("{sessionId:long}/products/{productId:long}/reconcile")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reconcile(long sessionId, long productId, [FromBody] ReturnSupplyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId)) return Unauthorized();
        if (SupplyRules.ValidateReturn(request) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });
        return ToResponse(await store.ReconcileAsync(sessionId, productId, request, staffUserId, cancellationToken));
    }

    private IActionResult ToResponse(SupplyResult result) => result.Success
        ? Ok(result.Details)
        : result.ErrorCode is "SESSION_NOT_FOUND" or "PRODUCT_NOT_FOUND" or "ALLOCATION_NOT_FOUND"
            ? NotFound(new { code = result.ErrorCode, message = result.Message })
            : Conflict(new { code = result.ErrorCode, message = result.Message });

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
