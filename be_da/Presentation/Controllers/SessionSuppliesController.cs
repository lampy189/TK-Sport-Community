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
// UC-41 / UC-42 (RP3 SCR-22 FR-22.3, 22.4): Staff issues supplies to a session and closes them with actual returns.
[ApiController]
[Route("api/stock/session-supplies")]
[Authorize(Roles = "Staff,Manager")]
[ValidatedRequest]
public sealed class SessionSuppliesController(ISessionSupplyService store, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SupplySession>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        // Venue days follow Vietnam time regardless of server timezone.
        return (await store.GetSessionsAsync(date, null, cancellationToken)).ToResponse();
    }

    [HttpGet("{sessionId:long:min(1)}")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(long sessionId, CancellationToken cancellationToken)
    {
        var details = await store.GetDetailsAsync(sessionId, cancellationToken);
        return details is null ? NotFound(new { code = "SESSION_NOT_FOUND", message = "Không tìm thấy buổi chơi." }) : Ok(details);
    }

    [HttpPost("{sessionId:long:min(1)}/issue")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Issue(long sessionId, [FromBody] IssueSupplyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId))
            return Unauthorized();
        return ToResponse(await store.IssueAsync(sessionId, request, staffUserId, cancellationToken));
    }

    // A referee request: issue to the requester (body may lower the quantity when stock is short).
    [HttpPost("{sessionId:long:min(1)}/requests/{requestId:long:min(1)}/issue")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IssueRequest(long sessionId, long requestId, [FromBody] IssueSupplyFromRequest? request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId))
            return Unauthorized();
        return ToResponse(await store.IssueRequestAsync(sessionId, requestId, request ?? new IssueSupplyFromRequest(), staffUserId, cancellationToken));
    }

    [HttpPost("{sessionId:long:min(1)}/requests/{requestId:long:min(1)}/reject")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RejectRequest(long sessionId, long requestId, [FromBody] SupplyDecisionRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId))
            return Unauthorized();
        return ToResponse(await store.RejectRequestAsync(sessionId, requestId, request, staffUserId, cancellationToken));
    }

    [HttpPost("{sessionId:long:min(1)}/products/{productId:long:min(1)}/reconcile")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reconcile(long sessionId, long productId, [FromBody] ReturnSupplyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId))
            return Unauthorized();
        return ToResponse(await store.ReconcileAsync(sessionId, productId, request, staffUserId, cancellationToken));
    }

    private IActionResult ToResponse(SupplyResult result) => SupplyResponses.ToResponse(this, result);
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
