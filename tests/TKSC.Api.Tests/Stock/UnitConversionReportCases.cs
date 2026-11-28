using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.UnitConversion;

// Report 5.1 – UnitConversion (Lê Tuấn Dũng): units of an item, conversion to the base unit, barcodes and unit
// changes once stock has moved (RP3 UC-26, UC-27, FR-22.1; task split 05/11/2026).
public sealed class UnitConversionReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private static readonly ProductUnit Bottle = new(1, "Chai", 1, "8930001");
    private static readonly ProductUnit Jug = new(2, "Lốc", 8, null);
    private static readonly ProductUnit Carton = new(3, "Thùng", 24, "9930001");
    private static readonly IReadOnlyList<ProductUnit> Water = [Bottle, Jug, Carton];
    private const string WaterUnits = "units=[Chai=1 (8930001), Lốc=8, Thùng=24 (9930001)]";
    private const string OneBase = "Mặt hàng phải có đúng một đơn vị chuẩn (hệ số 1).";

    private static ProductUnitInput U(string? name, int factor, string? barcode = null) => new(name, factor, barcode);

    // ---------------- StockUnitRules.NormalizeUnitName / NormalizeBarcode
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Unit name typed with extra spaces")]
    public void UTC001_NormalizeUnitName() => Check("StockUnitRules.NormalizeUnitName", "unitName=\"  thùng   lớn \"", "thùng lớn", () => StockUnitRules.NormalizeUnitName("  thùng   lớn "));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Barcode typed in lower case with spaces")]
    public void UTC002_NormalizeBarcode() => Check("StockUnitRules.NormalizeBarcode", "barcode=\" ab-12 \"", "AB-12", () => StockUnitRules.NormalizeBarcode(" ab-12 "));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Barcode of spaces only")]
    public void UTC003_NormalizeBarcodeBlank() => Check("StockUnitRules.NormalizeBarcode", "barcode=\"   \"", (string?)null, () => StockUnitRules.NormalizeBarcode("   "));

    // ---------------- StockUnitRules.IsValidBarcode / IsValidCode
    [Fact, Trait("Type", "Normal"), Trait("Pre", "EAN-13 barcode printed on the product")]
    public void UTC004_BarcodeEan13() => Check("StockUnitRules.IsValidBarcode", "barcode=\"8930000000012\"", true, () => StockUnitRules.IsValidBarcode("8930000000012"));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Barcode of 3 characters (minimum is 4)")]
    public void UTC005_BarcodeTooShort() => Check("StockUnitRules.IsValidBarcode", "barcode=\"123\"", false, () => StockUnitRules.IsValidBarcode("123"));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Barcode of 64 characters (maximum)")]
    public void UTC006_BarcodeMaximum() => Check("StockUnitRules.IsValidBarcode", "barcode=string('1', 64)", true, () => StockUnitRules.IsValidBarcode(new string('1', 64)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Barcode containing a space")]
    public void UTC007_BarcodeWithSpace() => Check("StockUnitRules.IsValidBarcode", "barcode=\"12 34\"", false, () => StockUnitRules.IsValidBarcode("12 34"));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Lookup by SKU instead of barcode")]
    public void UTC008_CodeSku() => Check("StockUnitRules.IsValidCode", "code=\"cau_as-30\"", true, () => StockUnitRules.IsValidCode("cau_as-30"));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Lookup code containing HTML")]
    public void UTC009_CodeScript() => Check("StockUnitRules.IsValidCode", "code=\"<script>\"", false, () => StockUnitRules.IsValidCode("<script>"));

    // ---------------- StockUnitRules.ValidateUnitNameInput
    [Fact, Trait("Type", "Normal"), Trait("Pre", "No unit chosen (means the base unit)")]
    public void UTC010_UnitInputNull() => Check("StockUnitRules.ValidateUnitNameInput", "unitName=null", (string?)null, () => StockUnitRules.ValidateUnitNameInput(null));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Unit name of 51 characters")]
    public void UTC011_UnitInputTooLong() =>
        Check("StockUnitRules.ValidateUnitNameInput", "unitName=string('a', 51)", "Tên đơn vị tối đa 50 ký tự.", () => StockUnitRules.ValidateUnitNameInput(new string('a', 51)));

    // ---------------- StockUnitRules.ValidateUnits
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Item sold by bottle, pack of 8 and carton of 24")]
    public void UTC012_UnitsValid() =>
        Check("StockUnitRules.ValidateUnits", "units=[Chai=1, Lốc=8, Thùng=24 (9930001)]", (string?)null,
            () => StockUnitRules.ValidateUnits([U("Chai", 1), U("Lốc", 8), U("Thùng", 24, "9930001")]));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Only a carton unit, no base unit")]
    public void UTC013_UnitsNoBase() => Check("StockUnitRules.ValidateUnits", "units=[Thùng=24]", OneBase, () => StockUnitRules.ValidateUnits([U("Thùng", 24)]));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Two units with factor 1")]
    public void UTC014_UnitsTwoBases() => Check("StockUnitRules.ValidateUnits", "units=[Chai=1, Lon=1]", OneBase, () => StockUnitRules.ValidateUnits([U("Chai", 1), U("Lon", 1)]));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Same unit name twice in different case")]
    public void UTC015_UnitsDuplicateName() =>
        Check("StockUnitRules.ValidateUnits", "units=[Chai=1, \" chai \"=5]", "Đơn vị 'chai' bị trùng.", () => StockUnitRules.ValidateUnits([U("Chai", 1), U(" chai ", 5)]));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Conversion factor one above the maximum")]
    public void UTC016_UnitsFactorAboveMax() =>
        Check("StockUnitRules.ValidateUnits", "units=[Chai=1, Thùng=100001]", "Hệ số quy đổi của 'Thùng' phải từ 1 đến 100.000.", () => StockUnitRules.ValidateUnits([U("Chai", 1), U("Thùng", 100_001)]));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Two units share one barcode")]
    public void UTC017_UnitsDuplicateBarcode() =>
        Check("StockUnitRules.ValidateUnits", "units=[Chai=1 (8930001), Thùng=24 (8930001)]", "Mã vạch 8930001 bị trùng.",
            () => StockUnitRules.ValidateUnits([U("Chai", 1, "8930001"), U("Thùng", 24, "8930001")]));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "11 units (maximum is 10)")]
    public void UTC018_UnitsTooMany() =>
        Check("StockUnitRules.ValidateUnits", "units=[Base=1, U2=2, ..., U11=11]", "Mỗi mặt hàng tối đa 10 đơn vị.",
            () => StockUnitRules.ValidateUnits([U("Base", 1), .. Enumerable.Range(2, 10).Select(i => U($"U{i}", i))]));

    // ---------------- StockUnitRules.ToBaseQuantity
    [Fact, Trait("Type", "Normal"), Trait("Pre", "2 cartons of 24 bottles")]
    public void UTC019_ToBaseConvert() => Check("StockUnitRules.ToBaseQuantity", "quantity=2; factorToBase=24", (int?)48, () => StockUnitRules.ToBaseQuantity(2, 24));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Converted quantity exactly at the 10,000,000 limit")]
    public void UTC020_ToBaseAtLimit() => Check("StockUnitRules.ToBaseQuantity", "quantity=100000; factorToBase=100", (int?)10_000_000, () => StockUnitRules.ToBaseQuantity(100_000, 100));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Converted quantity above the limit")]
    public void UTC021_ToBaseAboveLimit() => Check("StockUnitRules.ToBaseQuantity", "quantity=100000; factorToBase=101", (int?)null, () => StockUnitRules.ToBaseQuantity(100_000, 101));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Very large numbers that would overflow int")]
    public void UTC022_ToBaseOverflow() =>
        Check("StockUnitRules.ToBaseQuantity", "quantity=int.MaxValue; factorToBase=int.MaxValue", (int?)null, () => StockUnitRules.ToBaseQuantity(int.MaxValue, int.MaxValue));

    // ---------------- StockUnitRules.ResolveUnit
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Unit typed in lower case")]
    public void UTC023_ResolveUnitIgnoresCase() =>
        Check("StockUnitRules.ResolveUnit", $"{WaterUnits}; unitName=\"thùng\"", "Thùng", () => StockUnitRules.ResolveUnit(Water, "thùng")?.UnitName);

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Unit not defined for the item")]
    public void UTC024_ResolveUnitUnknown() =>
        Check("StockUnitRules.ResolveUnit", $"{WaterUnits}; unitName=\"Hộp\"", (string?)null, () => StockUnitRules.ResolveUnit(Water, "Hộp")?.UnitName);

    // ---------------- StockUnitRules.FormatBreakdown
    [Fact, Trait("Type", "Normal"), Trait("Pre", "58 bottles on hand")]
    public void UTC025_Breakdown() =>
        Check("StockUnitRules.FormatBreakdown", $"baseQuantity=58; {WaterUnits}; baseUnit=\"Chai\"", "2 Thùng 1 Lốc 2 Chai", () => StockUnitRules.FormatBreakdown(58, Water, "Chai"));

    // ---------------- StockUnitRules.ValidateUnitChange
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Item already has stock movements; Lốc factor changed from 8 to 6")]
    public void UTC026_UnitChangeLocked() =>
        Check("StockUnitRules.ValidateUnitChange", $"existing {WaterUnits}; incoming=[Chai=1, Lốc=6, Thùng=24]; locked=true",
            "Đã có phiếu kho, không thể đổi hệ số của 'Lốc'.", () => StockUnitRules.ValidateUnitChange(Water, [U("Chai", 1), U("Lốc", 6), U("Thùng", 24)], true));
}
