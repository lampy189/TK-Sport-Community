using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be_da.Application.Abstractions;
using be_da.Application.Stock;
using TKSC.Controllers.Middlewares;

namespace be_da.Controllers;

// UC-26 / UC-27 (team breakdown; RP3 SCR-22 FR-22.1, 22.2, 22.6): product catalogue, stock receipts/issues and history.
[ApiController]
[Route("api/stock")]
[Authorize(Roles = "Staff,Manager")]

[ValidatedRequest]
public sealed class StockController(IStockStore stockStore, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetCategories() => Ok(StockRules.Categories.Order());

    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<StockProduct>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        if (category is not null && !StockRules.Categories.Contains(category))
            return BadRequest(new { code = "INVALID_CATEGORY", message = "Nhóm hàng không hợp lệ." });

        if (StockRules.ValidateSearch(search) is { } searchError)
            return BadRequest(new { code = "VALIDATION_FAILED", message = searchError });
        return Ok(await stockStore.GetProductsAsync(new StockProductFilter(search, category, includeInactive), cancellationToken));
    }

    [HttpGet("products/{id:long:min(1)}")]
    [ProducesResponseType(typeof(StockProduct), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(long id, CancellationToken cancellationToken)
    {
        var product = await stockStore.GetProductAsync(id, cancellationToken);
        return product is null ? ProductNotFound() : Ok(product);
    }

    [HttpPost("products")]
    [ProducesResponseType(typeof(StockProduct), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId)) return Unauthorized();
        if (StockRules.ValidateCreate(request) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });

        var result = await stockStore.CreateProductAsync(request, staffUserId, cancellationToken);
        if (!result.Success) return ToError(result);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Product!.Id }, result.Product);
    }


    [HttpPut("products/{id:long:min(1)}")]
    [ProducesResponseType(typeof(StockProduct), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (StockRules.ValidateDetails(request.Name, request.Category, request.UnitPriceVnd, request.ReorderLevel) is { } error)
            return BadRequest(new { code = "VALIDATION_FAILED", message = error });

        if (request.Units is not null && StockUnitRules.ValidateUnits(request.Units) is { } unitError)
            return BadRequest(new { code = "VALIDATION_FAILED", message = unitError });

        var result = await stockStore.UpdateProductAsync(id, request, cancellationToken);
        return result.Success ? Ok(result.Product) : ToError(result);
    }

    // Quantity changes only through posted movements; on-hand is never edited directly.
    [HttpPost("products/{id:long:min(1)}/movements")]
    [ProducesResponseType(typeof(StockProduct), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordMovement(long id, [FromBody] StockMovementRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId)) return Unauthorized();
        if (StockRules.ValidateMovement(request) is { } error) return BadRequest(new { code = "VALIDATION_FAILED", message = error });

        var result = await stockStore.RecordMovementAsync(id, request, staffUserId, cancellationToken);
        return result.Success ? Ok(result.Product) : ToError(result);
    }


    [HttpGet("products/{id:long:min(1)}/movements")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovementEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovements(long id, CancellationToken cancellationToken)
    {
        var movements = await stockStore.GetMovementsAsync(id, cancellationToken);
        return movements is null ? ProductNotFound() : Ok(movements);
    }

    // UC-27, UC-39: scan a barcode (or type a SKU) to find the item and its unit.
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(StockCodeLookup), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Lookup([FromQuery] string? code, CancellationToken cancellationToken)
    {
        if (!StockUnitRules.IsValidCode(code))
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Mã vạch hoặc mã hàng không hợp lệ." });
        var found = await stockStore.LookupCodeAsync(StockUnitRules.NormalizeBarcode(code)!, cancellationToken);
        return found is null
            ? NotFound(new { code = "CODE_NOT_FOUND", message = "Không tìm thấy mặt hàng có mã này." })
            : Ok(found);
    }

    // UC-27, UC-39: Excel import and the bulk grid both post here.
    [HttpPost("receipts/batch")]
    [ProducesResponseType(typeof(BatchReceiptResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BatchReceiptResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BatchReceiptResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostBatchReceipt([FromBody] BatchReceiptRequest request, CancellationToken cancellationToken)
    {
        if (!await HasValidCsrf()) return BadRequest(new { code = "invalid_csrf_token" });
        if (!TryGetUserId(out var staffUserId)) return Unauthorized();
        if (StockReceiptRules.ValidateBatch(request) is { } error)
            return BadRequest(BatchReceiptResult.Fail("VALIDATION_FAILED", error));

        var result = await stockStore.PostBatchReceiptAsync(request, staffUserId, cancellationToken);
        return result.Success ? Ok(result)
            : result.Code == "SOURCE_REF_USED" ? Conflict(result)
            : BadRequest(result);
    }

    private IActionResult ToError(StockResult result) => result.ErrorCode switch
    {
        "PRODUCT_NOT_FOUND" => NotFound(new { code = result.ErrorCode, message = result.Message }),
        "UNIT_NOT_FOUND" or "QUANTITY_TOO_LARGE" => BadRequest(new { code = result.ErrorCode, message = result.Message }),
        "SKU_EXISTS" or "BARCODE_EXISTS" or "UNITS_LOCKED" or "PRODUCT_INACTIVE" or "INSUFFICIENT_STOCK" => Conflict(new { code = result.ErrorCode, message = result.Message }),
        _ => BadRequest(new { code = result.ErrorCode, message = result.Message })
    };

    private NotFoundObjectResult ProductNotFound() =>
        NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Không tìm thấy mặt hàng." });

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
