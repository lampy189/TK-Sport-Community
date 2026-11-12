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
public sealed class RefereeSuppliesController(ISessionSupplyService store, IStockService stock, IAntiforgery antiforgery) : ControllerBase
{
    // Active items of the referee's center, to pick from when requesting supplies.
    [HttpGet("products")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var result = await stock.GetProductsAsync(new StockProductFilter(IncludeInactive: false), cancellationToken);
        if (result.Value is not IReadOnlyList<StockProduct> products)
            return result.ToResponse();
        return Ok(products.Select(p => new { p.Id, p.Sku, p.Name, p.Category, p.BaseUnit, p.Units, p.AvailableQuantity }));
    }

    [HttpPost("{sessionId:long:min(1)}/requests")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRequest(long sessionId, [FromBody] CreateSupplyRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var refereeId))
            return Unauthorized();
        return SupplyResponses.ToResponse(this, await store.CreateRequestAsync(sessionId, request, refereeId, cancellationToken));
    }

    [HttpPost("{sessionId:long:min(1)}/requests/{requestId:long:min(1)}/cancel")]
    [ProducesResponseType(typeof(SessionSupplyDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelRequest(long sessionId, long requestId, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var refereeId))
            return Unauthorized();
        if (!await store.IsRefereeAssignedAsync(sessionId, refereeId, cancellationToken))
            return NotFound(new { code = "NOT_ASSIGNED", message = "Bạn không được phân công cho buổi chơi này." });
        return SupplyResponses.ToResponse(this, await store.CancelRequestAsync(sessionId, requestId, refereeId, cancellationToken));
    }

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
        return SupplyResponses.ToResponse(this, await store.ReportUseAsync(sessionId, productId, request, refereeId, cancellationToken));
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
