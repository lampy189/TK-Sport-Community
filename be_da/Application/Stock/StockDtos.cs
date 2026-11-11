<<<<<<< HEAD
=======
using System.Text.Json.Serialization;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
using System.Text.RegularExpressions;

namespace be_da.Application.Stock;

public sealed record StockProduct(
    long Id,
    string Sku,
    string Name,
    string? Category,
    int UnitPriceVnd,
    bool IsActive,
    int QuantityOnHand,
<<<<<<< HEAD
    int ReorderLevel
)
{
    public bool IsLowStock => IsActive && QuantityOnHand <= ReorderLevel;
=======
    int ReorderLevel,
    string BaseUnit = StockUnitRules.DefaultBaseUnit,
    IReadOnlyList<ProductUnit>? Units = null,
    bool UnitsLocked = false
)
{
    public bool IsLowStock => IsActive && QuantityOnHand <= ReorderLevel;
    public string QuantityBreakdown => StockUnitRules.FormatBreakdown(QuantityOnHand, Units ?? [], BaseUnit);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}

public sealed record StockMovementEntry(
    long Id,
    string MovementType,
    int Quantity,
    string? ReferenceType,
    long? ReferenceId,
    long? CreatedBy,
<<<<<<< HEAD
    string? CreatedByName
=======
    string? CreatedByName,
    DateTimeOffset? CreatedAt = null,
    string? EnteredUnit = null,
    int? EnteredQuantity = null,
    string? SourceRef = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
);

public sealed record StockProductFilter(string? Search = null, string? Category = null, bool IncludeInactive = true);

<<<<<<< HEAD
public sealed record CreateProductRequest(string? Sku, string? Name, string? Category, int UnitPriceVnd, int ReorderLevel, int InitialQuantity);

public sealed record UpdateProductRequest(string? Name, string? Category, int UnitPriceVnd, int ReorderLevel, bool IsActive);

public sealed record StockMovementRequest(string? Type, int Quantity);
=======
// [JsonRequired] on value types: a missing field must be rejected, not read as 0 or false.
public sealed record CreateProductRequest(
    string? Sku,
    string? Name,
    string? Category,
    [property: JsonRequired] int UnitPriceVnd,
    [property: JsonRequired] int ReorderLevel,
    [property: JsonRequired] int InitialQuantity,
    IReadOnlyList<ProductUnitInput>? Units = null);

// Units = null keeps the current units.
public sealed record UpdateProductRequest(
    string? Name,
    string? Category,
    [property: JsonRequired] int UnitPriceVnd,
    [property: JsonRequired] int ReorderLevel,
    [property: JsonRequired] bool IsActive,
    IReadOnlyList<ProductUnitInput>? Units = null);

// Quantity is in UnitName; an empty UnitName means the base unit.
public sealed record StockMovementRequest(string? Type, [property: JsonRequired] int Quantity, string? UnitName = null);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

public sealed record StockResult(bool Success, string? ErrorCode, string? Message, StockProduct? Product = null)
{
    public static StockResult Ok(StockProduct product) => new(true, null, null, product);
    public static StockResult Fail(string code, string message) => new(false, code, message);
}

// UC-26 / UC-27 (team breakdown) → RP3 SCR-22 / FR-22: one item code per stock item, movements are append-only.
public static partial class StockRules
{
    public const string Receipt = "Receipt";
    public const string Issue = "Issue";
    public const string OpeningBalance = "OpeningBalance";
    public const int MaxQuantity = 100_000;
    public const int MaxPriceVnd = 100_000_000;
<<<<<<< HEAD
=======
    public const int MaxSearchLength = 100;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

    // products.category is VARCHAR, so categories are stable codes; the UI shows Vietnamese labels.
    public static readonly IReadOnlySet<string> Categories =
        new HashSet<string>(["Drink", "Food", "Equipment", "Accessory", "Apparel", "Other"]);

    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]{1,49}$")]
    private static partial Regex SkuPattern();

    public static string? NormalizeSku(string? sku) => sku?.Trim().ToUpperInvariant();

    public static string? ValidateCreate(CreateProductRequest request)
    {
        var sku = NormalizeSku(request.Sku);
        if (string.IsNullOrEmpty(sku) || !SkuPattern().IsMatch(sku))
            return "Mã hàng (SKU) gồm 2–50 ký tự A-Z, 0-9, '-' hoặc '_'.";
        if (request.InitialQuantity < 0 || request.InitialQuantity > MaxQuantity)
<<<<<<< HEAD
            return $"Tồn kho ban đầu phải từ 0 đến {MaxQuantity:N0}.";
=======
            return $"Tồn kho ban đầu phải từ 0 đến 100.000.";
        if (request.Units is not null && StockUnitRules.ValidateUnits(request.Units) is { } unitError) return unitError;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        return ValidateDetails(request.Name, request.Category, request.UnitPriceVnd, request.ReorderLevel);
    }

    public static string? ValidateDetails(string? name, string? category, int unitPriceVnd, int reorderLevel)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 255) return "Tên mặt hàng là bắt buộc (tối đa 255 ký tự).";
<<<<<<< HEAD
        if (category is not null && !Categories.Contains(category)) return "Nhóm hàng không hợp lệ.";
        if (unitPriceVnd < 0 || unitPriceVnd > MaxPriceVnd) return "Đơn giá phải từ 0 đến 100.000.000 VND.";
        if (reorderLevel < 0 || reorderLevel > MaxQuantity) return $"Mức cảnh báo tồn phải từ 0 đến {MaxQuantity:N0}.";
=======
        if (trimmed.Any(char.IsControl)) return "Tên mặt hàng chứa ký tự không hợp lệ.";
        if (category is not null && !Categories.Contains(category)) return "Nhóm hàng không hợp lệ.";
        if (unitPriceVnd < 0 || unitPriceVnd > MaxPriceVnd) return "Đơn giá phải từ 0 đến 100.000.000 VND.";
        if (reorderLevel < 0 || reorderLevel > MaxQuantity) return $"Mức cảnh báo tồn phải từ 0 đến 100.000.";
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        return null;
    }

    public static string? ValidateMovement(StockMovementRequest request)
    {
        if (request.Type is not (Receipt or Issue)) return "Loại phiếu phải là nhập (Receipt) hoặc xuất (Issue).";
<<<<<<< HEAD
        if (request.Quantity <= 0 || request.Quantity > MaxQuantity) return $"Số lượng phải từ 1 đến {MaxQuantity:N0}.";
        return null;
    }
=======
        if (request.Quantity <= 0 || request.Quantity > MaxQuantity) return $"Số lượng phải từ 1 đến 100.000.";
        return StockUnitRules.ValidateUnitNameInput(request.UnitName);
    }

    public static string? ValidateSearch(string? search) =>
        search?.Trim().Length > MaxSearchLength ? $"Từ khóa tìm kiếm tối đa {MaxSearchLength} ký tự." : null;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}
