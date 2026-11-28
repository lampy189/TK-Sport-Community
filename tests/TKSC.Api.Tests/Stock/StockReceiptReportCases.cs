using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.StockReceipt;

// Report 5.1 – StockReceipt (Lê Tuấn Dũng): bulk stock receipt by barcode or SKU, line resolving, merging lines of the
// same item and protection against posting one source reference twice (RP3 UC-27, UC-39, FR-22.2).
public sealed class StockReceiptReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private static readonly ReceiptCatalogItem Water = new(1, "WATER", "Nước suối 500ml", true,
    [
        new(1, "Chai", 1, "8930001"),
        new(2, "Lốc", 6, null),
        new(3, "Thùng", 24, "9930001"),
        new(4, "Pallet", 101, null),
    ]);
    private static readonly ReceiptCatalogItem Old = new(2, "OLD", "Khăn cũ", false, [new(5, "Cái", 1, "7770001")]);
    private static readonly IReadOnlyList<ReceiptCatalogItem> Catalog = [Water, Old];
    private static readonly IReadOnlyList<BatchReceiptLine> OneLine = [new("WATER", null, 1)];
    private const string CatalogText = "catalog=[WATER: Chai=1 (8930001), Lốc=6, Thùng=24 (9930001), Pallet=101; OLD (inactive)]";
    private const string RefFormat = "Mã phiếu nhập gồm 2–100 ký tự chữ, số, '-', '_', '.' hoặc '/'.";
    private const string LineQuantity = "Error: Số lượng phải từ 1 đến 100.000.";

    private static string Resolve(string? code, string? unit, int quantity)
    {
        var (line, error) = StockReceiptRules.ResolveLine(1, new BatchReceiptLine(code, unit, quantity), Catalog);
        return line is not null ? $"{line.Name}: {line.Quantity} {line.UnitName} = {line.BaseQuantity} base" : $"Error: {error!.Message}";
    }

    private static ResolvedReceiptLine Resolved(long product, string unit, int quantity, int baseQuantity) => new(1, product, $"P{product}", unit, quantity, baseQuantity);

    private static string Merge(params ResolvedReceiptLine[] lines) =>
        string.Join("; ", StockReceiptRules.MergeLines(lines).Select(m => $"P{m.ProductId}={m.BaseQuantity}" + (m.EnteredUnit is null ? " (mixed units)" : $" ({m.EnteredQuantity} {m.EnteredUnit})")));

    private static readonly IReadOnlyList<MergedReceiptLine> Receipt = [new(1, "P1", 24, "Thùng", 1), new(2, "P2", 5, "Chai", 5)];

    // ---------------- StockReceiptRules.NormalizeSourceRef
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Receipt reference typed in lower case with spaces")]
    public void UTC001_NormalizeRef() => Check("StockReceiptRules.NormalizeSourceRef", "sourceRef=\" pn-01 \"", "PN-01", () => StockReceiptRules.NormalizeSourceRef(" pn-01 "));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Receipt reference not sent")]
    public void UTC002_NormalizeRefNull() => Check("StockReceiptRules.NormalizeSourceRef", "sourceRef=null", (string?)null, () => StockReceiptRules.NormalizeSourceRef(null));

    // ---------------- StockReceiptRules.ValidateBatch
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff enters a receipt with one line")]
    public void UTC003_BatchValid() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=\"PN-01\", Lines=[(WATER, 1)])", (string?)null, () => StockReceiptRules.ValidateBatch(new("PN-01", OneLine)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Receipt reference contains a space")]
    public void UTC004_BatchRefWithSpace() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=\"PN 01\", Lines=[(WATER, 1)])", RefFormat, () => StockReceiptRules.ValidateBatch(new("PN 01", OneLine)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Receipt reference of 101 characters")]
    public void UTC005_BatchRefTooLong() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=string('A', 101), Lines=[(WATER, 1)])", RefFormat, () => StockReceiptRules.ValidateBatch(new(new string('A', 101), OneLine)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Receipt with no lines")]
    public void UTC006_BatchNoLines() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=\"PN-01\", Lines=[])", "Phiếu nhập cần ít nhất một dòng hàng.", () => StockReceiptRules.ValidateBatch(new("PN-01", [])));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Receipt with 501 lines (maximum is 500)")]
    public void UTC007_BatchTooManyLines() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=\"PN-01\", Lines=501 x (WATER, 1))", "Mỗi phiếu nhập tối đa 500 dòng.",
            () => StockReceiptRules.ValidateBatch(new("PN-01", Enumerable.Repeat(new BatchReceiptLine("WATER", null, 1), 501).ToList())));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Receipt contains an empty line")]
    public void UTC008_BatchNullLine() =>
        Check("StockReceiptRules.ValidateBatch", "request=BatchReceiptRequest(SourceRef=\"PN-01\", Lines=[null])", "Phiếu nhập có dòng trống không hợp lệ.", () => StockReceiptRules.ValidateBatch(new("PN-01", [null!])));

    // ---------------- StockReceiptRules.ResolveLine
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Line entered by SKU without a unit")]
    public void UTC009_ResolveSkuBaseUnit() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"WATER\", UnitName=null, Quantity=5); {CatalogText}", "Nước suối 500ml: 5 Chai = 5 base", () => Resolve("WATER", null, 5));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Carton barcode scanned, 2 cartons")]
    public void UTC010_ResolveCartonBarcode() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"9930001\", UnitName=null, Quantity=2); {CatalogText}", "Nước suối 500ml: 2 Thùng = 48 base", () => Resolve("9930001", null, 2));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "SKU in lower case with the pack unit")]
    public void UTC011_ResolveSkuPack() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"water\", UnitName=\"Lốc\", Quantity=3); {CatalogText}", "Nước suối 500ml: 3 Lốc = 18 base", () => Resolve("water", "Lốc", 3));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Carton barcode scanned but the bottle unit chosen")]
    public void UTC012_ResolveBarcodeUnitMismatch() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"9930001\", UnitName=\"Chai\", Quantity=1); {CatalogText}",
            "Error: Mã vạch 9930001 là của đơn vị 'Thùng', không phải 'Chai'.", () => Resolve("9930001", "Chai", 1));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Code not in the catalogue")]
    public void UTC013_ResolveUnknownCode() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"NOPE1234\", UnitName=null, Quantity=1); {CatalogText}", "Error: Không tìm thấy mặt hàng có mã NOPE1234.", () => Resolve("NOPE1234", null, 1));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Unit not defined for the item")]
    public void UTC014_ResolveUnknownUnit() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"WATER\", UnitName=\"Xe\", Quantity=1); {CatalogText}",
            "Error: Nước suối 500ml không có đơn vị 'Xe'. Đơn vị hợp lệ: Chai, Lốc, Thùng, Pallet.", () => Resolve("WATER", "Xe", 1));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Discontinued item")]
    public void UTC015_ResolveInactiveItem() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"OLD\", UnitName=null, Quantity=1); {CatalogText}", "Error: Khăn cũ đã ngừng kinh doanh.", () => Resolve("OLD", null, 1));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Code containing HTML")]
    public void UTC016_ResolveScriptCode() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"<script>\", UnitName=null, Quantity=1); {CatalogText}", "Error: Mã vạch hoặc mã hàng không hợp lệ.", () => Resolve("<script>", null, 1));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Quantity zero")]
    public void UTC017_ResolveZeroQuantity() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"WATER\", UnitName=null, Quantity=0); {CatalogText}", LineQuantity, () => Resolve("WATER", null, 0));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "100000 pallets of 101 bottles (above the converted limit)")]
    public void UTC018_ResolveConvertedTooLarge() =>
        Check("StockReceiptRules.ResolveLine", $"line=(Code=\"WATER\", UnitName=\"Pallet\", Quantity=100000); {CatalogText}",
            "Error: Số lượng quy đổi vượt giới hạn cho phép.", () => Resolve("WATER", "Pallet", 100_000));

    // ---------------- StockReceiptRules.MergeLines
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Same item on two lines in the same unit")]
    public void UTC019_MergeSameUnit() =>
        Check("StockReceiptRules.MergeLines", "lines=[P1: 2 Thùng = 48; P1: 1 Thùng = 24]", "P1=72 (3 Thùng)", () => Merge(Resolved(1, "Thùng", 2, 48), Resolved(1, "Thùng", 1, 24)));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Same item on two lines in different units")]
    public void UTC020_MergeMixedUnits() =>
        Check("StockReceiptRules.MergeLines", "lines=[P1: 1 Thùng = 24; P1: 2 Lốc = 12]", "P1=36 (mixed units)", () => Merge(Resolved(1, "Thùng", 1, 24), Resolved(1, "Lốc", 2, 12)));

    // ---------------- StockReceiptRules.MatchesPosted
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Same reference sent again with the same lines (retry)")]
    public void UTC021_MatchesPostedSame() =>
        Check("StockReceiptRules.MatchesPosted", "merged=[P1=24, P2=5]; posted={P1: 24, P2: 5}", true, () => StockReceiptRules.MatchesPosted(Receipt, new Dictionary<long, int> { [1] = 24, [2] = 5 }));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Same reference reused for a different receipt")]
    public void UTC022_MatchesPostedDifferent() =>
        Check("StockReceiptRules.MatchesPosted", "merged=[P1=24, P2=5]; posted={P1: 12, P2: 5}", false, () => StockReceiptRules.MatchesPosted(Receipt, new Dictionary<long, int> { [1] = 12, [2] = 5 }));
}
