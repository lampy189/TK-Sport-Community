using be_da.Presentation.Filters;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Presentation.Middlewares;

namespace be_da.Presentation.Controllers;
// RP3 UC-43 / UC-44 (task split 13/11/2026, Dũng): Staff counts and proposes, Manager/Owner decides, stock moves once.
// System Admin has no access: it handles technical work, not business approvals.
[ApiController]
[Route("api/stock")]
[Authorize(Roles = "Staff,Manager,Owner")]
[ValidatedRequest]
public sealed class StockCountsController(IStockCountService counts, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("products/{productId:long:min(1)}/count-sheet")]
    [Authorize(Roles = "Staff,Manager")]
    [ProducesResponseType(typeof(StockCountSheet), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSheet(long productId, CancellationToken cancellationToken)
    {
        var sheet = await counts.GetSheetAsync(productId, cancellationToken);
        return sheet is null ? NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Không tìm thấy mặt hàng trong kho của cơ sở." }) : Ok(sheet);
    }

    [HttpPost("counts")]
    [Authorize(Roles = "Staff")]
    [ProducesResponseType(typeof(StockCountView), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateStockCountRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId))
            return Unauthorized();
        var result = await counts.CreateAsync(request, staffUserId, cancellationToken);
        return result.Success ? Created($"/api/stock/counts/{result.Count!.Id}", result.Count) : ToError(result);
    }

    [HttpGet("counts")]
    [ProducesResponseType(typeof(IReadOnlyList<StockCountView>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] long? productId, CancellationToken cancellationToken) =>
        (await counts.ListAsync(status, productId, cancellationToken)).ToResponse();

    [HttpGet("counts/{id:long:min(1)}")]
    [ProducesResponseType(typeof(StockCountView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        var count = await counts.GetAsync(id, cancellationToken);
        return count is null ? NotFound(new { code = "COUNT_NOT_FOUND", message = "Không tìm thấy phiếu kiểm kê." }) : Ok(count);
    }

    [HttpPost("counts/{id:long:min(1)}/approve")]
    [Authorize(Roles = "Manager,Owner")]
    [ProducesResponseType(typeof(StockCountView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Approve(long id, [FromBody] StockCountDecisionRequest request, CancellationToken cancellationToken) =>
        Decide(deciderId => counts.ApproveAsync(id, deciderId, request, cancellationToken));

    [HttpPost("counts/{id:long:min(1)}/reject")]
    [Authorize(Roles = "Manager,Owner")]
    [ProducesResponseType(typeof(StockCountView), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Reject(long id, [FromBody] StockCountDecisionRequest request, CancellationToken cancellationToken) =>
        Decide(deciderId => counts.RejectAsync(id, deciderId, request, cancellationToken));

    // Supply and stock figures of the signed-in center for a period (for the operations and revenue report).
    [HttpGet("reports/supplies")]
    [ProducesResponseType(typeof(SupplyReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SupplyReport([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        (await counts.GetSupplyReportAsync(from, to, cancellationToken)).ToResponse();

    // Completion check of the task: on hand = sum of movements, reserved = held order lines.
    [HttpGet("reconciliation")]
    [ProducesResponseType(typeof(IReadOnlyList<StockReconciliationLine>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reconciliation([FromQuery] bool onlyMismatches = false, CancellationToken cancellationToken = default) =>
        Ok(await counts.GetReconciliationAsync(onlyMismatches, cancellationToken));

    private async Task<IActionResult> Decide(Func<long, Task<StockCountResult>> decide)
    {
        if (!await HasValidCsrf())
            return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var deciderId))
            return Unauthorized();
        var result = await decide(deciderId);
        return result.Success ? Ok(result.Count) : ToError(result);
    }

    private IActionResult ToError(StockCountResult result)
    {
        var body = new { code = result.ErrorCode, message = result.Message, sheet = result.Sheet };
        return result.ErrorCode switch
        {
            "PRODUCT_NOT_FOUND" or "COUNT_NOT_FOUND" => NotFound(body),
            "VALIDATION_FAILED" or "UNIT_NOT_FOUND" or "QUANTITY_TOO_LARGE" => BadRequest(body),
            "SELF_APPROVAL" => StatusCode(StatusCodes.Status403Forbidden, body),
            _ => Conflict(body)
        };
    }

    private bool TryGetUserId(out long userId) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
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
