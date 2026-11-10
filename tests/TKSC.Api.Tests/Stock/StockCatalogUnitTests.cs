using be_da.Application.Stock;
using Xunit;

namespace TKSC.Api.Tests.Stock;

public sealed class StockCatalogUnitTests
{
    private static CreateProductRequest Create(string? sku = "CAU-AS30", string? name = "Shuttlecock AS30", int initial = 0, IReadOnlyList<ProductUnitInput>? units = null) =>
        new(sku, name, "Equipment", 25000, 5, initial, units);

    [UnitCase("UTC001", "NormalizeSku", "Normal", "sku=\" cau-as30 \"", "Return: \"CAU-AS30\"")]
    public void NormalizeSku_TrimsAndUppercases() => Assert.Equal("CAU-AS30", StockRules.NormalizeSku(" cau-as30 "));

    [UnitCase("UTC002", "NormalizeSku", "Abnormal", "sku=null", "Return: null")]
    public void NormalizeSku_Null() => Assert.Null(StockRules.NormalizeSku(null));

    [UnitCase("UTC003", "NormalizeSku", "Boundary", "sku=\"   \"", "Return: \"\"")]
    public void NormalizeSku_OnlySpaces() => Assert.Equal("", StockRules.NormalizeSku("   "));

    [UnitCase("UTC004", "ValidateCreate", "Normal", "sku=CAU-AS30; name=Shuttlecock AS30; initial=0", "Return: null (valid)")]
    public void ValidateCreate_Valid() => Assert.Null(StockRules.ValidateCreate(Create()));

    [UnitCase("UTC005", "ValidateCreate", "Abnormal", "sku=\"cau as30\" (has a space)", "Return: SKU format message")]
    public void ValidateCreate_SkuWithSpace() => Assert.Contains("SKU", StockRules.ValidateCreate(Create(sku: "cau as30")));

    [UnitCase("UTC006", "ValidateCreate", "Boundary", "sku=\"A\" (1 character, minimum is 2)", "Return: SKU format message")]
    public void ValidateCreate_SkuTooShort() => Assert.Contains("SKU", StockRules.ValidateCreate(Create(sku: "A")));

    [UnitCase("UTC007", "ValidateCreate", "Boundary", "sku=\"AB\" (2 characters)", "Return: null (valid)")]
    public void ValidateCreate_SkuMinimum() => Assert.Null(StockRules.ValidateCreate(Create(sku: "AB")));

    [UnitCase("UTC008", "ValidateCreate", "Abnormal", "initial=-1", "Return: initial quantity message")]
    public void ValidateCreate_NegativeInitial() => Assert.Contains("ban đầu", StockRules.ValidateCreate(Create(initial: -1)));

    [UnitCase("UTC009", "ValidateCreate", "Boundary", "initial=100000", "Return: null (valid)")]
    public void ValidateCreate_MaxInitial() => Assert.Null(StockRules.ValidateCreate(Create(initial: 100_000)));

    [UnitCase("UTC010", "ValidateCreate", "Abnormal", "units=[] (no base unit)", "Return: base unit message")]
    public void ValidateCreate_EmptyUnits() => Assert.Contains("đơn vị chuẩn", StockRules.ValidateCreate(Create(units: [])));

    [UnitCase("UTC011", "ValidateDetails", "Normal", "name=Mineral water; category=Drink; price=10000; reorder=10", "Return: null (valid)")]
    public void ValidateDetails_Valid() => Assert.Null(StockRules.ValidateDetails("Mineral water", "Drink", 10000, 10));

    [UnitCase("UTC012", "ValidateDetails", "Abnormal", "name=\"  \"", "Return: name required message")]
    public void ValidateDetails_BlankName() => Assert.Contains("bắt buộc", StockRules.ValidateDetails("  ", null, 1, 0));

    [UnitCase("UTC013", "ValidateDetails", "Boundary", "name has 255 characters", "Return: null (valid)")]
    public void ValidateDetails_Name255() => Assert.Null(StockRules.ValidateDetails(new string('a', 255), null, 1, 0));

    [UnitCase("UTC014", "ValidateDetails", "Boundary", "name has 256 characters", "Return: name length message")]
    public void ValidateDetails_Name256() => Assert.Contains("255", StockRules.ValidateDetails(new string('a', 256), null, 1, 0));

    [UnitCase("UTC015", "ValidateDetails", "Abnormal", "name contains control character U+0007", "Return: invalid character message")]
    public void ValidateDetails_ControlCharacter() => Assert.Contains("không hợp lệ", StockRules.ValidateDetails("a\u0007b", null, 1, 0));

    [UnitCase("UTC016", "ValidateDetails", "Abnormal", "category=Unknown", "Return: category message")]
    public void ValidateDetails_UnknownCategory() => Assert.Contains("Nhóm hàng", StockRules.ValidateDetails("Item", "Unknown", 1, 0));

    [UnitCase("UTC017", "ValidateDetails", "Boundary", "price=0", "Return: null (valid)")]
    public void ValidateDetails_PriceZero() => Assert.Null(StockRules.ValidateDetails("Item", null, 0, 0));

    [UnitCase("UTC018", "ValidateDetails", "Abnormal", "price=-1", "Return: price message")]
    public void ValidateDetails_NegativePrice() => Assert.Contains("Đơn giá", StockRules.ValidateDetails("Item", null, -1, 0));

    [UnitCase("UTC019", "ValidateDetails", "Boundary", "price=100000000", "Return: null (valid)")]
    public void ValidateDetails_MaxPrice() => Assert.Null(StockRules.ValidateDetails("Item", null, 100_000_000, 0));

    [UnitCase("UTC020", "ValidateDetails", "Boundary", "price=100000001", "Return: price message")]
    public void ValidateDetails_PriceAboveMax() => Assert.Contains("Đơn giá", StockRules.ValidateDetails("Item", null, 100_000_001, 0));

    [UnitCase("UTC021", "ValidateDetails", "Abnormal", "reorder=-1", "Return: reorder level message")]
    public void ValidateDetails_NegativeReorder() => Assert.Contains("cảnh báo", StockRules.ValidateDetails("Item", null, 1, -1));

    [UnitCase("UTC022", "ValidateMovement", "Normal", "type=Receipt; quantity=5; unit=Carton", "Return: null (valid)")]
    public void ValidateMovement_Valid() => Assert.Null(StockRules.ValidateMovement(new("Receipt", 5, "Carton")));

    [UnitCase("UTC023", "ValidateMovement", "Abnormal", "type=Adjust", "Return: movement type message")]
    public void ValidateMovement_BadType() => Assert.Contains("Loại phiếu", StockRules.ValidateMovement(new("Adjust", 5)));

    [UnitCase("UTC024", "ValidateMovement", "Boundary", "quantity=0", "Return: quantity message")]
    public void ValidateMovement_Zero() => Assert.Contains("Số lượng", StockRules.ValidateMovement(new("Issue", 0)));

    [UnitCase("UTC025", "ValidateMovement", "Boundary", "quantity=1", "Return: null (valid)")]
    public void ValidateMovement_One() => Assert.Null(StockRules.ValidateMovement(new("Issue", 1)));

    [UnitCase("UTC026", "ValidateMovement", "Boundary", "quantity=100001", "Return: quantity message")]
    public void ValidateMovement_AboveMax() => Assert.Contains("Số lượng", StockRules.ValidateMovement(new("Receipt", 100_001)));

    [UnitCase("UTC027", "ValidateMovement", "Abnormal", "unit name has 51 characters", "Return: unit name length message")]
    public void ValidateMovement_LongUnit() => Assert.Contains("đơn vị", StockRules.ValidateMovement(new("Receipt", 1, new string('a', 51))));

    [UnitCase("UTC028", "ValidateSearch", "Normal", "search=null", "Return: null (valid)")]
    public void ValidateSearch_Null() => Assert.Null(StockRules.ValidateSearch(null));

    [UnitCase("UTC029", "ValidateSearch", "Boundary", "search has 100 characters", "Return: null (valid)")]
    public void ValidateSearch_Max() => Assert.Null(StockRules.ValidateSearch(new string('a', 100)));

    [UnitCase("UTC030", "ValidateSearch", "Boundary", "search has 101 characters", "Return: search length message")]
    public void ValidateSearch_TooLong() => Assert.NotNull(StockRules.ValidateSearch(new string('a', 101)));

    private static StockProduct Product(bool active, int onHand, int reorder) =>
        new(1, "SKU", "Item", null, 1000, active, onHand, reorder);

    [UnitCase("UTC031", "StockProduct.IsLowStock", "Normal", "active; onHand=3; reorder=5", "Return: true")]
    public void IsLowStock_Below() => Assert.True(Product(true, 3, 5).IsLowStock);

    [UnitCase("UTC032", "StockProduct.IsLowStock", "Boundary", "active; onHand=5; reorder=5", "Return: true")]
    public void IsLowStock_Equal() => Assert.True(Product(true, 5, 5).IsLowStock);

    [UnitCase("UTC033", "StockProduct.IsLowStock", "Normal", "active; onHand=6; reorder=5", "Return: false")]
    public void IsLowStock_Above() => Assert.False(Product(true, 6, 5).IsLowStock);

    [UnitCase("UTC034", "StockProduct.IsLowStock", "Abnormal", "inactive; onHand=0; reorder=5", "Return: false (inactive items never warn)")]
    public void IsLowStock_Inactive() => Assert.False(Product(false, 0, 5).IsLowStock);
}
