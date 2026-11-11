using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;

namespace TKSC.Api.Tests.Stock;

public sealed class UnitConversionUnitTests
{
    private static readonly ProductUnit Bottle = new(1, "Bottle", 1, "8930001");
    private static readonly ProductUnit Jug = new(2, "Jug", 8, null);
    private static readonly ProductUnit Carton = new(3, "Carton", 24, "9930001");
    private static readonly IReadOnlyList<ProductUnit> Water = [Bottle, Jug, Carton];
    private static ProductUnitInput U(string? name, int factor, string? barcode = null) => new(name, factor, barcode);

    [UnitCase("UTC001", "NormalizeUnitName", "Normal", "unit=\"  big   carton \"", "Return: \"big carton\"")]
    public void NormalizeUnitName_CollapsesSpaces() => Assert.Equal("big carton", StockUnitRules.NormalizeUnitName("  big   carton "));

    [UnitCase("UTC002", "NormalizeUnitName", "Abnormal", "unit=null", "Return: \"\"")]
    public void NormalizeUnitName_Null() => Assert.Equal("", StockUnitRules.NormalizeUnitName(null));

    [UnitCase("UTC003", "NormalizeUnitName", "Boundary", "unit=\"\\t\\n\" (only whitespace)", "Return: \"\"")]
    public void NormalizeUnitName_Whitespace() => Assert.Equal("", StockUnitRules.NormalizeUnitName("\t\n"));

    [UnitCase("UTC004", "NormalizeBarcode", "Normal", "barcode=\" ab-12 \"", "Return: \"AB-12\"")]
    public void NormalizeBarcode_Upper() => Assert.Equal("AB-12", StockUnitRules.NormalizeBarcode(" ab-12 "));

    [UnitCase("UTC005", "NormalizeBarcode", "Abnormal", "barcode=null", "Return: null")]
    public void NormalizeBarcode_Null() => Assert.Null(StockUnitRules.NormalizeBarcode(null));

    [UnitCase("UTC006", "NormalizeBarcode", "Boundary", "barcode=\"   \"", "Return: null")]
    public void NormalizeBarcode_Blank() => Assert.Null(StockUnitRules.NormalizeBarcode("   "));

    [UnitCase("UTC007", "IsValidBarcode", "Normal", "barcode=8930000000012", "Return: true")]
    public void IsValidBarcode_Ean() => Assert.True(StockUnitRules.IsValidBarcode("8930000000012"));

    [UnitCase("UTC008", "IsValidBarcode", "Boundary", "barcode has 3 characters", "Return: false")]
    public void IsValidBarcode_TooShort() => Assert.False(StockUnitRules.IsValidBarcode("123"));

    [UnitCase("UTC009", "IsValidBarcode", "Boundary", "barcode has 4 characters", "Return: true")]
    public void IsValidBarcode_Minimum() => Assert.True(StockUnitRules.IsValidBarcode("1234"));

    [UnitCase("UTC010", "IsValidBarcode", "Boundary", "barcode has 64 characters", "Return: true")]
    public void IsValidBarcode_Maximum() => Assert.True(StockUnitRules.IsValidBarcode(new string('1', 64)));

    [UnitCase("UTC011", "IsValidBarcode", "Boundary", "barcode has 65 characters", "Return: false")]
    public void IsValidBarcode_TooLong() => Assert.False(StockUnitRules.IsValidBarcode(new string('1', 65)));

    [UnitCase("UTC012", "IsValidBarcode", "Abnormal", "barcode=\"12 34\" (has a space)", "Return: false")]
    public void IsValidBarcode_Space() => Assert.False(StockUnitRules.IsValidBarcode("12 34"));

    [UnitCase("UTC013", "IsValidCode", "Normal", "code=cau_as-30", "Return: true")]
    public void IsValidCode_Sku() => Assert.True(StockUnitRules.IsValidCode("cau_as-30"));

    [UnitCase("UTC014", "IsValidCode", "Boundary", "code has 1 character", "Return: false")]
    public void IsValidCode_TooShort() => Assert.False(StockUnitRules.IsValidCode("A"));

    [UnitCase("UTC015", "IsValidCode", "Boundary", "code has 101 characters", "Return: false")]
    public void IsValidCode_TooLong() => Assert.False(StockUnitRules.IsValidCode(new string('A', 101)));

    [UnitCase("UTC016", "IsValidCode", "Abnormal", "code=<script>", "Return: false")]
    public void IsValidCode_Script() => Assert.False(StockUnitRules.IsValidCode("<script>"));

    [UnitCase("UTC017", "ValidateUnitNameInput", "Normal", "unit=Carton", "Return: null (valid)")]
    public void ValidateUnitNameInput_Valid() => Assert.Null(StockUnitRules.ValidateUnitNameInput("Carton"));

    [UnitCase("UTC018", "ValidateUnitNameInput", "Normal", "unit=null (means base unit)", "Return: null (valid)")]
    public void ValidateUnitNameInput_Null() => Assert.Null(StockUnitRules.ValidateUnitNameInput(null));

    [UnitCase("UTC019", "ValidateUnitNameInput", "Boundary", "unit has 51 characters", "Return: length message")]
    public void ValidateUnitNameInput_TooLong() => Assert.NotNull(StockUnitRules.ValidateUnitNameInput(new string('a', 51)));

    [UnitCase("UTC020", "ValidateUnitNameInput", "Abnormal", "unit contains control character U+0001", "Return: invalid character message")]
    public void ValidateUnitNameInput_Control() => Assert.NotNull(StockUnitRules.ValidateUnitNameInput("a\u0001b"));

    [UnitCase("UTC021", "ValidateUnits", "Normal", "Bottle=1; Jug=8; Carton=24 with barcode", "Return: null (valid)")]
    public void ValidateUnits_Valid() => Assert.Null(StockUnitRules.ValidateUnits([U("Bottle", 1), U("Jug", 8), U("Carton", 24, "9930001")]));

    [UnitCase("UTC022", "ValidateUnits", "Abnormal", "units=null", "Return: base unit message")]
    public void ValidateUnits_Null() => Assert.NotNull(StockUnitRules.ValidateUnits(null));

    [UnitCase("UTC023", "ValidateUnits", "Abnormal", "Carton=24 only (no base unit)", "Return: exactly one base unit message")]
    public void ValidateUnits_NoBase() => Assert.Contains("đúng một", StockUnitRules.ValidateUnits([U("Carton", 24)]));

    [UnitCase("UTC024", "ValidateUnits", "Abnormal", "A=1; B=1 (two base units)", "Return: exactly one base unit message")]
    public void ValidateUnits_TwoBases() => Assert.Contains("đúng một", StockUnitRules.ValidateUnits([U("A", 1), U("B", 1)]));

    [UnitCase("UTC025", "ValidateUnits", "Abnormal", "Bottle=1; \" bottle \"=5 (same name, other case)", "Return: duplicate unit message")]
    public void ValidateUnits_DuplicateName() => Assert.Contains("trùng", StockUnitRules.ValidateUnits([U("Bottle", 1), U(" bottle ", 5)]));

    [UnitCase("UTC026", "ValidateUnits", "Abnormal", "Bottle=1; Carton=0", "Return: factor range message")]
    public void ValidateUnits_ZeroFactor() => Assert.Contains("Hệ số", StockUnitRules.ValidateUnits([U("Bottle", 1), U("Carton", 0)]));

    [UnitCase("UTC027", "ValidateUnits", "Boundary", "Bottle=1; Carton=100000", "Return: null (valid)")]
    public void ValidateUnits_MaxFactor() => Assert.Null(StockUnitRules.ValidateUnits([U("Bottle", 1), U("Carton", 100_000)]));

    [UnitCase("UTC028", "ValidateUnits", "Boundary", "Bottle=1; Carton=100001", "Return: factor range message")]
    public void ValidateUnits_FactorAboveMax() => Assert.Contains("Hệ số", StockUnitRules.ValidateUnits([U("Bottle", 1), U("Carton", 100_001)]));

    [UnitCase("UTC029", "ValidateUnits", "Boundary", "unit name has 51 characters", "Return: unit name message")]
    public void ValidateUnits_LongName() => Assert.Contains("Tên đơn vị", StockUnitRules.ValidateUnits([U(new string('a', 51), 1)]));

    [UnitCase("UTC030", "ValidateUnits", "Abnormal", "barcode=\"ab\"", "Return: barcode format message")]
    public void ValidateUnits_BadBarcode() => Assert.Contains("Mã vạch", StockUnitRules.ValidateUnits([U("Bottle", 1, "ab")]));

    [UnitCase("UTC031", "ValidateUnits", "Abnormal", "two units share barcode 8930001", "Return: duplicate barcode message")]
    public void ValidateUnits_DuplicateBarcode() => Assert.Contains("trùng", StockUnitRules.ValidateUnits([U("Bottle", 1, "8930001"), U("Carton", 24, "8930001")]));

    [UnitCase("UTC032", "ValidateUnits", "Boundary", "11 units (maximum is 10)", "Return: unit count message")]
    public void ValidateUnits_TooMany() => Assert.Contains("tối đa 10", StockUnitRules.ValidateUnits(
        [U("Base", 1), .. Enumerable.Range(2, 10).Select(i => U($"U{i}", i))]));

    [UnitCase("UTC033", "ToBaseQuantity", "Normal", "quantity=2; factor=24", "Return: 48")]
    public void ToBaseQuantity_Converts() => Assert.Equal(48, StockUnitRules.ToBaseQuantity(2, 24));

    [UnitCase("UTC034", "ToBaseQuantity", "Abnormal", "quantity=0; factor=24", "Return: null")]
    public void ToBaseQuantity_ZeroQuantity() => Assert.Null(StockUnitRules.ToBaseQuantity(0, 24));

    [UnitCase("UTC035", "ToBaseQuantity", "Abnormal", "quantity=1; factor=0", "Return: null")]
    public void ToBaseQuantity_ZeroFactor() => Assert.Null(StockUnitRules.ToBaseQuantity(1, 0));

    [UnitCase("UTC036", "ToBaseQuantity", "Boundary", "quantity=100000; factor=100 (exactly 10,000,000)", "Return: 10000000")]
    public void ToBaseQuantity_AtLimit() => Assert.Equal(10_000_000, StockUnitRules.ToBaseQuantity(100_000, 100));

    [UnitCase("UTC037", "ToBaseQuantity", "Boundary", "quantity=100000; factor=101", "Return: null")]
    public void ToBaseQuantity_AboveLimit() => Assert.Null(StockUnitRules.ToBaseQuantity(100_000, 101));

    [UnitCase("UTC038", "ToBaseQuantity", "Abnormal", "quantity=int.MaxValue; factor=int.MaxValue", "Return: null (no overflow)")]
    public void ToBaseQuantity_Overflow() => Assert.Null(StockUnitRules.ToBaseQuantity(int.MaxValue, int.MaxValue));

    [UnitCase("UTC039", "ResolveUnit", "Normal", "unit=\"carton\" (lower case)", "Return: unit Carton")]
    public void ResolveUnit_IgnoresCase() => Assert.Equal(Carton, StockUnitRules.ResolveUnit(Water, "carton"));

    [UnitCase("UTC040", "ResolveUnit", "Normal", "unit=\"\"", "Return: base unit Bottle")]
    public void ResolveUnit_EmptyMeansBase() => Assert.Equal(Bottle, StockUnitRules.ResolveUnit(Water, ""));

    [UnitCase("UTC041", "ResolveUnit", "Abnormal", "unit=Pack (not defined)", "Return: null")]
    public void ResolveUnit_Unknown() => Assert.Null(StockUnitRules.ResolveUnit(Water, "Pack"));

    [UnitCase("UTC042", "FormatBreakdown", "Normal", "quantity=58; units Bottle, Jug=8, Carton=24", "Return: \"2 Carton 1 Jug 2 Bottle\"")]
    public void FormatBreakdown_Mixed() => Assert.Equal("2 Carton 1 Jug 2 Bottle", StockUnitRules.FormatBreakdown(58, Water, "Bottle"));

    [UnitCase("UTC043", "FormatBreakdown", "Boundary", "quantity=48 (whole Carton)", "Return: \"2 Carton\"")]
    public void FormatBreakdown_Whole() => Assert.Equal("2 Carton", StockUnitRules.FormatBreakdown(48, Water, "Bottle"));

    [UnitCase("UTC044", "FormatBreakdown", "Boundary", "quantity=0", "Return: \"0 Bottle\"")]
    public void FormatBreakdown_Zero() => Assert.Equal("0 Bottle", StockUnitRules.FormatBreakdown(0, Water, "Bottle"));

    [UnitCase("UTC045", "FormatBreakdown", "Abnormal", "quantity=5; no units loaded; base=Piece", "Return: \"5 Piece\"")]
    public void FormatBreakdown_NoUnits() => Assert.Equal("5 Piece", StockUnitRules.FormatBreakdown(5, [], "Piece"));

    [UnitCase("UTC046", "ValidateUnitChange", "Normal", "not locked; base renamed and factor changed", "Return: null (allowed)")]
    public void ValidateUnitChange_NotLocked() => Assert.Null(StockUnitRules.ValidateUnitChange(Water, [U("Lon", 1), U("Carton", 12)], false));

    [UnitCase("UTC047", "ValidateUnitChange", "Normal", "locked; existing units kept, Pack=6 added, barcode changed", "Return: null (allowed)")]
    public void ValidateUnitChange_AddKeeps() => Assert.Null(StockUnitRules.ValidateUnitChange(Water,
        [U("Bottle", 1), U("Jug", 8, "1234"), U("Carton", 24), U("Pack", 6)], true));

    [UnitCase("UTC048", "ValidateUnitChange", "Abnormal", "locked; Jug factor 8 changed to 6", "Return: cannot change factor message")]
    public void ValidateUnitChange_FactorChanged() => Assert.Contains("hệ số", StockUnitRules.ValidateUnitChange(Water,
        [U("Bottle", 1), U("Jug", 6), U("Carton", 24)], true));

    [UnitCase("UTC049", "ValidateUnitChange", "Abnormal", "locked; unit Jug removed", "Return: cannot remove unit message")]
    public void ValidateUnitChange_Removed() => Assert.Contains("xóa", StockUnitRules.ValidateUnitChange(Water,
        [U("Bottle", 1), U("Carton", 24)], true));
}
