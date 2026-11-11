using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;

namespace TKSC.Api.Tests.Stock;

public sealed class StockReceiptUnitTests
{
    private static readonly ReceiptCatalogItem Water = new(1, "WATER", "Mineral water", true,
    [
        new(1, "Bottle", 1, "8930001"),
        new(2, "Pack", 6, null),
        new(3, "Carton", 24, "9930001"),
        new(4, "Pallet", 101, null),
    ]);
    private static readonly ReceiptCatalogItem Old = new(2, "OLD", "Old item", false, [new(5, "Piece", 1, "7770001")]);
    private static readonly IReadOnlyList<ReceiptCatalogItem> Catalog = [Water, Old];
    private static readonly IReadOnlyList<BatchReceiptLine> OneLine = [new("WATER", null, 1)];

    private static (ResolvedReceiptLine? Line, BatchLineError? Error) Resolve(string? code, string? unit, int quantity) =>
        StockReceiptRules.ResolveLine(1, new BatchReceiptLine(code, unit, quantity), Catalog);
    private static ResolvedReceiptLine Resolved(long product, string unit, int quantity, int baseQuantity) =>
        new(1, product, $"P{product}", unit, quantity, baseQuantity);

    [UnitCase("UTC001", "NormalizeSourceRef", "Normal", "ref=\" pn-01 \"", "Return: \"PN-01\"")]
    public void NormalizeSourceRef_Upper() => Assert.Equal("PN-01", StockReceiptRules.NormalizeSourceRef(" pn-01 "));

    [UnitCase("UTC002", "NormalizeSourceRef", "Abnormal", "ref=null", "Return: null")]
    public void NormalizeSourceRef_Null() => Assert.Null(StockReceiptRules.NormalizeSourceRef(null));

    [UnitCase("UTC003", "NormalizeSourceRef", "Boundary", "ref=\"  \"", "Return: null")]
    public void NormalizeSourceRef_Blank() => Assert.Null(StockReceiptRules.NormalizeSourceRef("  "));

    [UnitCase("UTC004", "ValidateBatch", "Normal", "ref=PN-01; 1 line", "Return: null (valid)")]
    public void ValidateBatch_Valid() => Assert.Null(StockReceiptRules.ValidateBatch(new("PN-01", OneLine)));

    [UnitCase("UTC005", "ValidateBatch", "Abnormal", "ref=null; 1 line", "Return: source reference message")]
    public void ValidateBatch_NoRef() => Assert.Contains("Mã phiếu", StockReceiptRules.ValidateBatch(new(null, OneLine)));

    [UnitCase("UTC006", "ValidateBatch", "Abnormal", "ref=\"PN 01\" (has a space)", "Return: source reference message")]
    public void ValidateBatch_RefWithSpace() => Assert.Contains("Mã phiếu", StockReceiptRules.ValidateBatch(new("PN 01", OneLine)));

    [UnitCase("UTC007", "ValidateBatch", "Boundary", "ref has 1 character", "Return: source reference message")]
    public void ValidateBatch_RefTooShort() => Assert.Contains("Mã phiếu", StockReceiptRules.ValidateBatch(new("P", OneLine)));

    [UnitCase("UTC008", "ValidateBatch", "Boundary", "ref has 100 characters", "Return: null (valid)")]
    public void ValidateBatch_RefMaximum() => Assert.Null(StockReceiptRules.ValidateBatch(new(new string('P', 100), OneLine)));

    [UnitCase("UTC009", "ValidateBatch", "Boundary", "ref has 101 characters", "Return: source reference message")]
    public void ValidateBatch_RefTooLong() => Assert.Contains("Mã phiếu", StockReceiptRules.ValidateBatch(new(new string('P', 101), OneLine)));

    [UnitCase("UTC010", "ValidateBatch", "Abnormal", "lines=null", "Return: at least one line message")]
    public void ValidateBatch_NullLines() => Assert.Contains("ít nhất", StockReceiptRules.ValidateBatch(new("PN-01", null)));

    [UnitCase("UTC011", "ValidateBatch", "Boundary", "lines=[] (0 lines)", "Return: at least one line message")]
    public void ValidateBatch_NoLines() => Assert.Contains("ít nhất", StockReceiptRules.ValidateBatch(new("PN-01", [])));

    [UnitCase("UTC012", "ValidateBatch", "Boundary", "500 lines", "Return: null (valid)")]
    public void ValidateBatch_MaxLines() => Assert.Null(StockReceiptRules.ValidateBatch(new("PN-01", Enumerable.Repeat(OneLine[0], 500).ToList())));

    [UnitCase("UTC013", "ValidateBatch", "Boundary", "501 lines", "Return: maximum lines message")]
    public void ValidateBatch_TooManyLines() => Assert.Contains("500", StockReceiptRules.ValidateBatch(new("PN-01", Enumerable.Repeat(OneLine[0], 501).ToList())));

    [UnitCase("UTC014", "ValidateBatch", "Abnormal", "lines=[null]", "Return: blank line message")]
    public void ValidateBatch_NullLine() => Assert.Contains("dòng trống", StockReceiptRules.ValidateBatch(new("PN-01", [null!])));

    [UnitCase("UTC015", "ResolveLine", "Normal", "code=9930001 (barcode of Carton); quantity=2", "Return: unit Carton; baseQuantity=48")]
    public void ResolveLine_BarcodeNamesUnit()
    {
        var (line, error) = Resolve("9930001", null, 2);
        Assert.Null(error);
        Assert.Equal("Carton", line!.UnitName);
        Assert.Equal(48, line.BaseQuantity);
    }

    [UnitCase("UTC016", "ResolveLine", "Normal", "code=water (SKU, lower case); unit=Pack; quantity=3", "Return: unit Pack; baseQuantity=18")]
    public void ResolveLine_SkuWithUnit() => Assert.Equal(18, Resolve("water", "Pack", 3).Line!.BaseQuantity);

    [UnitCase("UTC017", "ResolveLine", "Normal", "code=WATER; no unit; quantity=5", "Return: base unit Bottle; baseQuantity=5")]
    public void ResolveLine_SkuDefaultsToBase()
    {
        var line = Resolve("WATER", null, 5).Line!;
        Assert.Equal("Bottle", line.UnitName);
        Assert.Equal(5, line.BaseQuantity);
    }

    [UnitCase("UTC018", "ResolveLine", "Normal", "code=9930001; unit=carton (same as barcode)", "Return: resolved line")]
    public void ResolveLine_BarcodeSameUnit() => Assert.NotNull(Resolve("9930001", "carton", 1).Line);

    [UnitCase("UTC019", "ResolveLine", "Abnormal", "code=\"\"", "Return: missing code error")]
    public void ResolveLine_EmptyCode() => Assert.Contains("Thiếu", Resolve("", null, 1).Error!.Message);

    [UnitCase("UTC020", "ResolveLine", "Abnormal", "code=<script>", "Return: invalid code error")]
    public void ResolveLine_InvalidCode() => Assert.Contains("không hợp lệ", Resolve("<script>", null, 1).Error!.Message);

    [UnitCase("UTC021", "ResolveLine", "Abnormal", "code=NOPE1234", "Return: item not found error")]
    public void ResolveLine_UnknownCode() => Assert.Contains("Không tìm thấy", Resolve("NOPE1234", null, 1).Error!.Message);

    [UnitCase("UTC022", "ResolveLine", "Abnormal", "code=WATER; unit=Xe", "Return: unknown unit error listing valid units")]
    public void ResolveLine_UnknownUnit() => Assert.Contains("Đơn vị hợp lệ", Resolve("WATER", "Xe", 1).Error!.Message);

    [UnitCase("UTC023", "ResolveLine", "Abnormal", "code=9930001 (Carton); unit=Bottle", "Return: barcode belongs to another unit error")]
    public void ResolveLine_BarcodeOtherUnit() => Assert.Contains("là của đơn vị", Resolve("9930001", "Bottle", 1).Error!.Message);

    [UnitCase("UTC024", "ResolveLine", "Abnormal", "code=OLD (inactive item)", "Return: discontinued error")]
    public void ResolveLine_Inactive() => Assert.Contains("ngừng kinh doanh", Resolve("OLD", null, 1).Error!.Message);

    [UnitCase("UTC025", "ResolveLine", "Boundary", "quantity=0", "Return: quantity error")]
    public void ResolveLine_ZeroQuantity() => Assert.Contains("Số lượng", Resolve("WATER", null, 0).Error!.Message);

    [UnitCase("UTC026", "ResolveLine", "Boundary", "quantity=100000 in base unit", "Return: resolved line")]
    public void ResolveLine_MaxQuantity() => Assert.Equal(100_000, Resolve("WATER", null, 100_000).Line!.BaseQuantity);

    [UnitCase("UTC027", "ResolveLine", "Boundary", "quantity=100001", "Return: quantity error")]
    public void ResolveLine_AboveMaxQuantity() => Assert.Contains("Số lượng", Resolve("WATER", null, 100_001).Error!.Message);

    [UnitCase("UTC028", "ResolveLine", "Boundary", "quantity=100000; unit=Pallet (factor 101)", "Return: converted quantity too large error")]
    public void ResolveLine_ConversionTooLarge() => Assert.Contains("quy đổi", Resolve("WATER", "Pallet", 100_000).Error!.Message);

    [UnitCase("UTC029", "MergeLines", "Normal", "product 1: 2 Carton + 1 Carton", "Return: 1 line; base=72; entered 3 Carton")]
    public void MergeLines_SameUnit()
    {
        var merged = Assert.Single(StockReceiptRules.MergeLines([Resolved(1, "Carton", 2, 48), Resolved(1, "Carton", 1, 24)]));
        Assert.Equal(72, merged.BaseQuantity);
        Assert.Equal("Carton", merged.EnteredUnit);
        Assert.Equal(3, merged.EnteredQuantity);
    }

    [UnitCase("UTC030", "MergeLines", "Normal", "product 1: 1 Carton + 2 Pack", "Return: 1 line; base=36; entered unit and quantity empty")]
    public void MergeLines_MixedUnits()
    {
        var merged = Assert.Single(StockReceiptRules.MergeLines([Resolved(1, "Carton", 1, 24), Resolved(1, "Pack", 2, 12)]));
        Assert.Equal(36, merged.BaseQuantity);
        Assert.Null(merged.EnteredUnit);
        Assert.Null(merged.EnteredQuantity);
    }

    [UnitCase("UTC031", "MergeLines", "Normal", "products 1 and 2", "Return: 2 lines")]
    public void MergeLines_TwoProducts() => Assert.Equal(2, StockReceiptRules.MergeLines([Resolved(1, "Bottle", 1, 1), Resolved(2, "Piece", 1, 1)]).Count);

    [UnitCase("UTC032", "MergeLines", "Boundary", "no lines", "Return: empty list")]
    public void MergeLines_Empty() => Assert.Empty(StockReceiptRules.MergeLines([]));

    private static readonly IReadOnlyList<MergedReceiptLine> Merged = [new(1, "P1", 24, "Carton", 1), new(2, "P2", 5, "Piece", 5)];

    [UnitCase("UTC033", "MatchesPosted", "Normal", "posted {1:24, 2:5} equals the receipt", "Return: true")]
    public void MatchesPosted_Same() => Assert.True(StockReceiptRules.MatchesPosted(Merged, new Dictionary<long, int> { [1] = 24, [2] = 5 }));

    [UnitCase("UTC034", "MatchesPosted", "Abnormal", "posted {1:12, 2:5} (other quantity)", "Return: false")]
    public void MatchesPosted_OtherQuantity() => Assert.False(StockReceiptRules.MatchesPosted(Merged, new Dictionary<long, int> { [1] = 12, [2] = 5 }));

    [UnitCase("UTC035", "MatchesPosted", "Abnormal", "posted {1:24} (product 2 missing)", "Return: false")]
    public void MatchesPosted_Missing() => Assert.False(StockReceiptRules.MatchesPosted(Merged, new Dictionary<long, int> { [1] = 24 }));

    [UnitCase("UTC036", "MatchesPosted", "Abnormal", "posted {1:24, 3:5} (other product)", "Return: false")]
    public void MatchesPosted_OtherProduct() => Assert.False(StockReceiptRules.MatchesPosted(Merged, new Dictionary<long, int> { [1] = 24, [3] = 5 }));
}
