using be_da.Application.DTOs;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.Validators;
// UC-27, UC-39 (FR-22.2): a whole receipt posts together and one source reference never adds stock twice.
public static partial class StockReceiptRules
{
    public const int MaxLines = 500;
    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9_./-]{1,99}$")]
    private static partial Regex SourceRefPattern();
    public static string? NormalizeSourceRef(string? sourceRef)
    {
        var value = sourceRef?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static string? ValidateBatch(BatchReceiptRequest request)
    {
        var sourceRef = NormalizeSourceRef(request.SourceRef);
        if (sourceRef is null || !SourceRefPattern().IsMatch(sourceRef))
            return "Mã phiếu nhập gồm 2–100 ký tự chữ, số, '-', '_', '.' hoặc '/'.";
        if (request.Lines is null || request.Lines.Count == 0)
            return "Phiếu nhập cần ít nhất một dòng hàng.";
        if (request.Lines.Count > MaxLines)
            return $"Mỗi phiếu nhập tối đa {MaxLines} dòng.";
        if (request.Lines.Any(l => l is null))
            return "Phiếu nhập có dòng trống không hợp lệ.";
        return null;
    }

    // Returns the resolved line, or the reason it cannot be received. A barcode already names its unit.
    public static (ResolvedReceiptLine? Line, BatchLineError? Error) ResolveLine(int lineNo, BatchReceiptLine line, IReadOnlyList<ReceiptCatalogItem> catalog)
    {
        var code = StockUnitRules.NormalizeBarcode(line.Code);
        if (code is null)
            return (null, new(lineNo, "Thiếu mã vạch hoặc mã hàng."));
        if (!StockUnitRules.IsValidCode(code))
            return (null, new(lineNo, "Mã vạch hoặc mã hàng không hợp lệ."));
        if (StockUnitRules.ValidateUnitNameInput(line.UnitName)is { } unitError)
            return (null, new(lineNo, unitError));
        if (line.Quantity <= 0 || line.Quantity > StockRules.MaxQuantity)
            return (null, new(lineNo, $"Số lượng phải từ 1 đến 100.000."));
        ReceiptCatalogItem? item = null;
        ProductUnit? unit = null;
        foreach (var candidate in catalog)
        {
            unit = candidate.Units.FirstOrDefault(u => u.Barcode == code);
            if (unit is null)
                continue;
            item = candidate;
            break;
        }

        var requestedUnit = StockUnitRules.NormalizeUnitName(line.UnitName);
        if (item is not null)
        {
            if (requestedUnit.Length > 0 && !string.Equals(requestedUnit, unit!.UnitName, StringComparison.OrdinalIgnoreCase))
                return (null, new(lineNo, $"Mã vạch {code} là của đơn vị '{unit.UnitName}', không phải '{requestedUnit}'."));
        }
        else
        {
            item = catalog.FirstOrDefault(c => c.Sku == code);
            if (item is null)
                return (null, new(lineNo, $"Không tìm thấy mặt hàng có mã {code}."));
            unit = StockUnitRules.ResolveUnit(item.Units, requestedUnit);
            if (unit is null)
                return (null, new(lineNo, $"{item.Name} không có đơn vị '{requestedUnit}'. Đơn vị hợp lệ: {string.Join(", ", item.Units.Select(u => u.UnitName))}."));
        }

        if (!item.IsActive)
            return (null, new(lineNo, $"{item.Name} đã ngừng kinh doanh."));
        var baseQuantity = StockUnitRules.ToBaseQuantity(line.Quantity, unit!.FactorToBase);
        if (baseQuantity is null)
            return (null, new(lineNo, "Số lượng quy đổi vượt giới hạn cho phép."));
        return (new(lineNo, item.ProductId, item.Name, unit.UnitName, line.Quantity, baseQuantity.Value), null);
    }

    // One movement per product per receipt, so lines of the same product are added together.
    public static IReadOnlyList<MergedReceiptLine> MergeLines(IEnumerable<ResolvedReceiptLine> lines) => lines.GroupBy(l => l.ProductId).Select(g =>
    {
        var sameUnit = g.Select(l => l.UnitName).Distinct().Count() == 1;
        return new MergedReceiptLine(g.Key, g.First().Name, g.Sum(l => l.BaseQuantity), sameUnit ? g.First().UnitName : null, sameUnit ? g.Sum(l => l.Quantity) : null);
    }).ToList();
    // True when a repeated source reference carries exactly the receipt that was already posted.
    public static bool MatchesPosted(IReadOnlyList<MergedReceiptLine> merged, IReadOnlyDictionary<long, int> postedByProduct) => merged.Count == postedByProduct.Count && merged.All(m => postedByProduct.TryGetValue(m.ProductId, out var posted) && posted == m.BaseQuantity);
}
