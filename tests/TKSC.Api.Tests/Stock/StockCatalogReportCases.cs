using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.StockCatalog;

// Report 5.1 – StockCatalog (Lê Tuấn Dũng): item code, item details, stock receipt/issue request, search and the
// low-stock warning (RP3 UC-26, UC-27; task split 05/11/2026).
public sealed class StockCatalogReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string SkuFormat = "Mã hàng (SKU) gồm 2–50 ký tự A-Z, 0-9, '-' hoặc '_'.";
    private const string NameRequired = "Tên mặt hàng là bắt buộc (tối đa 255 ký tự).";
    private const string PriceRange = "Đơn giá phải từ 0 đến 100.000.000 VND.";
    private const string MovementQuantity = "Số lượng phải từ 1 đến 100.000.";

    private static CreateProductRequest Create(string? sku = "CAU-AS30", int initial = 0, IReadOnlyList<ProductUnitInput>? units = null) =>
        new(sku, "Shuttlecock AS30", "Equipment", 25000, 5, initial, units);

    private static StockProduct Product(bool active, int onHand, int reorder) => new(1, "WATER", "Nước suối 500ml", "Drink", 10000, active, onHand, reorder);

    // ---------------- StockRules.NormalizeSku
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff types a SKU with spaces and lower case")]
    public void UTC001_NormalizeSku() => Check("StockRules.NormalizeSku", "sku=\" cau-as30 \"", "CAU-AS30", () => StockRules.NormalizeSku(" cau-as30 "));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "SKU not sent")]
    public void UTC002_NormalizeSkuNull() => Check("StockRules.NormalizeSku", "sku=null", (string?)null, () => StockRules.NormalizeSku(null));

    // ---------------- StockRules.ValidateCreate
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Manager creates a new item with valid data")]
    public void UTC003_CreateValid() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"CAU-AS30\", Name=\"Shuttlecock AS30\", Category=\"Equipment\", UnitPriceVnd=25000, ReorderLevel=5, InitialQuantity=0)",
            (string?)null, () => StockRules.ValidateCreate(Create()));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "SKU contains a space")]
    public void UTC004_CreateSkuWithSpace() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"cau as30\", ...)", SkuFormat, () => StockRules.ValidateCreate(Create(sku: "cau as30")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "SKU of 1 character (minimum is 2)")]
    public void UTC005_CreateSkuTooShort() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"A\", ...)", SkuFormat, () => StockRules.ValidateCreate(Create(sku: "A")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "SKU of 2 characters")]
    public void UTC006_CreateSkuMinimum() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"AB\", ...)", (string?)null, () => StockRules.ValidateCreate(Create(sku: "AB")));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Negative opening stock")]
    public void UTC007_CreateNegativeInitial() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"CAU-AS30\", InitialQuantity=-1, ...)",
            "Tồn kho ban đầu phải từ 0 đến 100.000.", () => StockRules.ValidateCreate(Create(initial: -1)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Unit list sent empty (no base unit)")]
    public void UTC008_CreateNoBaseUnit() =>
        Check("StockRules.ValidateCreate", "request=CreateProductRequest(Sku=\"CAU-AS30\", Units=[], ...)",
            "Mặt hàng cần có đơn vị chuẩn.", () => StockRules.ValidateCreate(Create(units: [])));

    // ---------------- StockRules.ValidateDetails
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Manager edits item details with valid data")]
    public void UTC009_DetailsValid() =>
        Check("StockRules.ValidateDetails", "name=\"Nước suối 500ml\"; category=\"Drink\"; unitPriceVnd=10000; reorderLevel=10",
            (string?)null, () => StockRules.ValidateDetails("Nước suối 500ml", "Drink", 10000, 10));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Item name left blank")]
    public void UTC010_DetailsBlankName() =>
        Check("StockRules.ValidateDetails", "name=\"  \"; category=null; unitPriceVnd=1; reorderLevel=0", NameRequired, () => StockRules.ValidateDetails("  ", null, 1, 0));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Item name of 256 characters")]
    public void UTC011_DetailsNameTooLong() =>
        Check("StockRules.ValidateDetails", "name=string('a', 256); unitPriceVnd=1; reorderLevel=0", NameRequired, () => StockRules.ValidateDetails(new string('a', 256), null, 1, 0));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Item name contains a control character")]
    public void UTC012_DetailsControlCharacter() =>
        Check("StockRules.ValidateDetails", "name=\"a\\u0007b\"; unitPriceVnd=1; reorderLevel=0",
            "Tên mặt hàng chứa ký tự không hợp lệ.", () => StockRules.ValidateDetails("a\u0007b", null, 1, 0));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Category not in the approved list")]
    public void UTC013_DetailsUnknownCategory() =>
        Check("StockRules.ValidateDetails", "name=\"Item\"; category=\"Electronics\"; unitPriceVnd=1; reorderLevel=0",
            "Nhóm hàng không hợp lệ.", () => StockRules.ValidateDetails("Item", "Electronics", 1, 0));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Unit price at the maximum")]
    public void UTC014_DetailsMaxPrice() =>
        Check("StockRules.ValidateDetails", "name=\"Item\"; unitPriceVnd=100000000; reorderLevel=0", (string?)null, () => StockRules.ValidateDetails("Item", null, 100_000_000, 0));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Unit price one above the maximum")]
    public void UTC015_DetailsPriceAboveMax() =>
        Check("StockRules.ValidateDetails", "name=\"Item\"; unitPriceVnd=100000001; reorderLevel=0", PriceRange, () => StockRules.ValidateDetails("Item", null, 100_000_001, 0));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Negative low-stock warning level")]
    public void UTC016_DetailsNegativeReorder() =>
        Check("StockRules.ValidateDetails", "name=\"Item\"; unitPriceVnd=1; reorderLevel=-1",
            "Mức cảnh báo tồn phải từ 0 đến 100.000.", () => StockRules.ValidateDetails("Item", null, 1, -1));

    // ---------------- StockRules.ValidateMovement
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff receives 5 cartons")]
    public void UTC017_MovementValid() =>
        Check("StockRules.ValidateMovement", "request=StockMovementRequest(Type=\"Receipt\", Quantity=5, UnitName=\"Thùng\")",
            (string?)null, () => StockRules.ValidateMovement(new StockMovementRequest("Receipt", 5, "Thùng")));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Movement type is not Receipt or Issue")]
    public void UTC018_MovementBadType() =>
        Check("StockRules.ValidateMovement", "request=StockMovementRequest(Type=\"Adjust\", Quantity=5)",
            "Loại phiếu phải là nhập (Receipt) hoặc xuất (Issue).", () => StockRules.ValidateMovement(new StockMovementRequest("Adjust", 5)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Quantity zero")]
    public void UTC019_MovementZero() =>
        Check("StockRules.ValidateMovement", "request=StockMovementRequest(Type=\"Issue\", Quantity=0)", MovementQuantity, () => StockRules.ValidateMovement(new StockMovementRequest("Issue", 0)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Quantity one above the limit")]
    public void UTC020_MovementAboveMax() =>
        Check("StockRules.ValidateMovement", "request=StockMovementRequest(Type=\"Receipt\", Quantity=100001)", MovementQuantity, () => StockRules.ValidateMovement(new StockMovementRequest("Receipt", 100_001)));

    // ---------------- StockRules.ValidateSearch
    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Search text of 101 characters")]
    public void UTC021_SearchTooLong() =>
        Check("StockRules.ValidateSearch", "search=string('a', 101)", "Từ khóa tìm kiếm tối đa 100 ký tự.", () => StockRules.ValidateSearch(new string('a', 101)));

    // ---------------- StockProduct.IsLowStock
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Active item, 3 left, warning level 5")]
    public void UTC022_LowStockBelow() => Check("StockProduct.IsLowStock", "isActive=true; quantityOnHand=3; reorderLevel=5", true, () => Product(true, 3, 5).IsLowStock);

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Active item, quantity equals the warning level")]
    public void UTC023_LowStockEqual() => Check("StockProduct.IsLowStock", "isActive=true; quantityOnHand=5; reorderLevel=5", true, () => Product(true, 5, 5).IsLowStock);

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Discontinued item with no stock")]
    public void UTC024_LowStockInactive() => Check("StockProduct.IsLowStock", "isActive=false; quantityOnHand=0; reorderLevel=5", false, () => Product(false, 0, 5).IsLowStock);
}
