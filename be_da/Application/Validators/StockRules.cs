using be_da.Application.DTOs;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.Validators;
// UC-26 / UC-27 (team breakdown) → RP3 SCR-22 / FR-22: one item code per stock item, movements are append-only.
public static partial class StockRules
{
    public const string Receipt = "Receipt";
    public const string Issue = "Issue";
    public const string OpeningBalance = "OpeningBalance";
    public const int MaxQuantity = 100_000;
    public const int MaxPriceVnd = 100_000_000;
    public const int MaxSearchLength = 100;
    // products.category is VARCHAR, so categories are stable codes; the UI shows Vietnamese labels.
    public static readonly IReadOnlySet<string> Categories = new HashSet<string>(["Drink", "Food", "Equipment", "Accessory", "Apparel", "Other"]);
    [GeneratedRegex("^[A-Z0-9][A-Z0-9_-]{1,49}$")]
    private static partial Regex SkuPattern();
    public static string? NormalizeSku(string? sku) => sku?.Trim().ToUpperInvariant();
    public static string? ValidateCreate(CreateProductRequest request)
    {
        var sku = NormalizeSku(request.Sku);
        if (string.IsNullOrEmpty(sku) || !SkuPattern().IsMatch(sku))
            return "Mã hàng (SKU) gồm 2–50 ký tự A-Z, 0-9, '-' hoặc '_'.";
        if (request.InitialQuantity < 0 || request.InitialQuantity > MaxQuantity)
            return $"Tồn kho ban đầu phải từ 0 đến 100.000.";
        if (request.Units is not null && StockUnitRules.ValidateUnits(request.Units)is { } unitError)
            return unitError;
        return ValidateDetails(request.Name, request.Category, request.UnitPriceVnd, request.ReorderLevel);
    }

    public static string? ValidateDetails(string? name, string? category, int unitPriceVnd, int reorderLevel)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 255)
            return "Tên mặt hàng là bắt buộc (tối đa 255 ký tự).";
        if (trimmed.Any(char.IsControl))
            return "Tên mặt hàng chứa ký tự không hợp lệ.";
        if (category is not null && !Categories.Contains(category))
            return "Nhóm hàng không hợp lệ.";
        if (unitPriceVnd < 0 || unitPriceVnd > MaxPriceVnd)
            return "Đơn giá phải từ 0 đến 100.000.000 VND.";
        if (reorderLevel < 0 || reorderLevel > MaxQuantity)
            return $"Mức cảnh báo tồn phải từ 0 đến 100.000.";
        return null;
    }

    public static string? ValidateMovement(StockMovementRequest request)
    {
        if (request.Type is not (Receipt or Issue))
            return "Loại phiếu phải là nhập (Receipt) hoặc xuất (Issue).";
        if (request.Quantity <= 0 || request.Quantity > MaxQuantity)
            return $"Số lượng phải từ 1 đến 100.000.";
        return StockUnitRules.ValidateUnitNameInput(request.UnitName);
    }

    public static string? ValidateSearch(string? search) => search?.Trim().Length > MaxSearchLength ? $"Từ khóa tìm kiếm tối đa {MaxSearchLength} ký tự." : null;
}
