using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.Stock;

public sealed record ProductUnit(long Id, string UnitName, int FactorToBase, string? Barcode)
{
    public bool IsBase => FactorToBase == 1;
}

public sealed record ProductUnitInput(string? UnitName, [property: JsonRequired] int FactorToBase, string? Barcode = null);

// Answer to a scanned barcode or typed SKU. Quantities are counted in BaseUnit; FactorToBase converts UnitName to it.
public sealed record StockCodeLookup(
    long ProductId,
    string Sku,
    string Name,
    bool IsActive,
    int UnitPriceVnd,
    int QuantityOnHand,
    string BaseUnit,
    string UnitName,
    int FactorToBase,
    string? Barcode
);

// Task split 05/11 (UC-26, 27): goods arrive and sell in different units, stock is counted in one base unit (FR-22.1).
// The base unit is the smallest one, so every factor is a whole number.
public static partial class StockUnitRules
{
    public const string DefaultBaseUnit = "Cái";
    public const int MaxFactor = 100_000;
    public const int MaxBaseQuantity = 10_000_000;
    public const int MaxUnitNameLength = 50;
    public const int MaxUnitsPerProduct = 10;
    public const int MaxCodeLength = 100;

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("^[A-Z0-9-]{4,64}$")]
    private static partial Regex BarcodePattern();

    [GeneratedRegex("^[A-Z0-9_-]{2,100}$")]
    private static partial Regex CodePattern();

    public static string NormalizeUnitName(string? unitName) =>
        Whitespace().Replace(unitName ?? "", " ").Trim();

    public static string? NormalizeBarcode(string? barcode)
    {
        var value = barcode?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static bool IsValidBarcode(string? barcode) =>
        NormalizeBarcode(barcode) is { } value && BarcodePattern().IsMatch(value);

    // A code is either a barcode or a SKU.
    public static bool IsValidCode(string? code) =>
        NormalizeBarcode(code) is { } value && CodePattern().IsMatch(value);

    // For requests that may leave the unit empty (meaning the base unit).
    public static string? ValidateUnitNameInput(string? unitName)
    {
        var name = NormalizeUnitName(unitName);
        if (name.Length > MaxUnitNameLength) return $"Tên đơn vị tối đa {MaxUnitNameLength} ký tự.";
        return name.Any(char.IsControl) ? "Tên đơn vị chứa ký tự không hợp lệ." : null;
    }

    public static string? ValidateUnits(IReadOnlyList<ProductUnitInput>? units)
    {
        if (units is null || units.Count == 0) return "Mặt hàng cần có đơn vị chuẩn.";
        if (units.Count > MaxUnitsPerProduct) return $"Mỗi mặt hàng tối đa {MaxUnitsPerProduct} đơn vị.";
        if (units.Count(u => u.FactorToBase == 1) != 1)
            return "Mặt hàng phải có đúng một đơn vị chuẩn (hệ số 1).";
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var barcodes = new HashSet<string>();
        foreach (var unit in units)
        {
            var name = NormalizeUnitName(unit.UnitName);
            if (name.Length == 0 || name.Length > MaxUnitNameLength)
                return $"Tên đơn vị là bắt buộc (tối đa {MaxUnitNameLength} ký tự).";
            if (name.Any(char.IsControl)) return "Tên đơn vị chứa ký tự không hợp lệ.";
            if (!names.Add(name)) return $"Đơn vị '{name}' bị trùng.";
            if (unit.FactorToBase < 1 || unit.FactorToBase > MaxFactor)
                return $"Hệ số quy đổi của '{name}' phải từ 1 đến 100.000.";
            if (NormalizeBarcode(unit.Barcode) is { } barcode)
            {
                if (!IsValidBarcode(barcode)) return $"Mã vạch của '{name}' gồm 4–64 ký tự chữ, số hoặc '-'.";
                if (!barcodes.Add(barcode)) return $"Mã vạch {barcode} bị trùng.";
            }
        }
        return null;
    }

    // Null when the quantity cannot be converted: not positive, or too large to post.
    public static int? ToBaseQuantity(int quantity, int factorToBase)
    {
        if (quantity <= 0 || factorToBase < 1) return null;
        var result = (long)quantity * factorToBase;
        return result > MaxBaseQuantity ? null : (int)result;
    }

    // An empty unit name means the base unit.
    public static ProductUnit? ResolveUnit(IReadOnlyList<ProductUnit> units, string? unitName)
    {
        var name = NormalizeUnitName(unitName);
        return name.Length == 0
            ? units.FirstOrDefault(u => u.IsBase)
            : units.FirstOrDefault(u => string.Equals(u.UnitName, name, StringComparison.OrdinalIgnoreCase));
    }

    // Largest units first: 50 with Thùng = 24 and base Chai reads "2 Thùng 2 Chai".
    public static string FormatBreakdown(int baseQuantity, IReadOnlyList<ProductUnit> units, string baseUnit)
    {
        var parts = new List<string>();
        var rest = baseQuantity;
        foreach (var unit in units.Where(u => !u.IsBase).OrderByDescending(u => u.FactorToBase))
        {
            if (rest < unit.FactorToBase) continue;
            parts.Add($"{rest / unit.FactorToBase} {unit.UnitName}");
            rest %= unit.FactorToBase;
        }
        if (rest > 0 || parts.Count == 0) parts.Add($"{rest} {baseUnit}");
        return string.Join(" ", parts);
    }

    // Once stock has moved, old quantities depend on the base unit and factors: only additions and barcodes may change.
    public static string? ValidateUnitChange(IReadOnlyList<ProductUnit> existing, IReadOnlyList<ProductUnitInput> incoming, bool locked)
    {
        if (!locked) return null;
        foreach (var unit in existing)
        {
            var match = incoming.FirstOrDefault(u =>
                string.Equals(NormalizeUnitName(u.UnitName), unit.UnitName, StringComparison.OrdinalIgnoreCase));
            if (match is null) return $"Đã có phiếu kho, không thể xóa đơn vị '{unit.UnitName}'.";
            if (match.FactorToBase != unit.FactorToBase) return $"Đã có phiếu kho, không thể đổi hệ số của '{unit.UnitName}'.";
        }
        return null;
    }
}
