using TKSC.Application.DTOs;
using TKSC.Application.Services;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.GoodsHandover;

// Report 5.1 – GoodsHandover (Lê Tuấn Dũng): delivery and receipt conditions of a goods order and the stock lines a
// delivery deducts, one per item (RP3 UC-38; team breakdown UC-31).
public sealed class GoodsHandoverReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string DeliverRule = "Chỉ giao hàng cho đơn đã thanh toán và đã xuất hóa đơn.";
    private const string ReceiveRule = "Đơn chưa được giao. Cần bấm Giao hàng trước khi xác nhận nhận hàng.";

    private static GoodsLine Line(long product, short quantity) => new(product, $"P{product}", quantity, 10000, quantity * 10000);
    private static string Stock(params GoodsLine[] lines) => string.Join("; ", GoodsHandoverRules.StockLines(lines).Select(l => $"P{l.ProductId}={l.Quantity}"));

    // ---------------- GoodsHandoverRules.ValidateDeliver
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Order paid and invoiced")]
    public void UTC001_DeliverReady() =>
        Check("GoodsHandoverRules.ValidateDeliver", "status=\"Invoiced\"; paymentState=\"Paid\"; hasInvoice=true", (string?)null, () => GoodsHandoverRules.ValidateDeliver("Invoiced", "Paid", true));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Order still a cart, unpaid")]
    public void UTC002_DeliverCart() =>
        Check("GoodsHandoverRules.ValidateDeliver", "status=\"Cart\"; paymentState=\"Unpaid\"; hasInvoice=false", DeliverRule, () => GoodsHandoverRules.ValidateDeliver("Cart", "Unpaid", false));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Order paid but no invoice issued")]
    public void UTC003_DeliverNoInvoice() =>
        Check("GoodsHandoverRules.ValidateDeliver", "status=\"Paid\"; paymentState=\"Paid\"; hasInvoice=false", DeliverRule, () => GoodsHandoverRules.ValidateDeliver("Paid", "Paid", false));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Payment needs review")]
    public void UTC004_DeliverNeedsReview() =>
        Check("GoodsHandoverRules.ValidateDeliver", "status=\"Invoiced\"; paymentState=\"NeedsReview\"; hasInvoice=true", DeliverRule, () => GoodsHandoverRules.ValidateDeliver("Invoiced", "NeedsReview", true));

    // ---------------- GoodsHandoverRules.ValidateReceive
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff has delivered the order")]
    public void UTC005_ReceiveDelivered() => Check("GoodsHandoverRules.ValidateReceive", "handoverState=\"Delivered\"", (string?)null, () => GoodsHandoverRules.ValidateReceive("Delivered"));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Order not delivered yet")]
    public void UTC006_ReceivePending() => Check("GoodsHandoverRules.ValidateReceive", "handoverState=\"Pending\"", ReceiveRule, () => GoodsHandoverRules.ValidateReceive("Pending"));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Order has no handover state")]
    public void UTC007_ReceiveNull() => Check("GoodsHandoverRules.ValidateReceive", "handoverState=null", ReceiveRule, () => GoodsHandoverRules.ValidateReceive(null));

    // ---------------- GoodsHandoverRules.StockLines
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Invoice has item 1 on two lines and item 2 once")]
    public void UTC008_StockLinesGrouped() => Check("GoodsHandoverRules.StockLines", "lines=[P1 x2, P1 x3, P2 x1]", "P1=5; P2=1", () => Stock(Line(1, 2), Line(1, 3), Line(2, 1)));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Invoice has one line")]
    public void UTC009_StockLinesSingle() => Check("GoodsHandoverRules.StockLines", "lines=[P1 x4]", "P1=4", () => Stock(Line(1, 4)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Invoice has no lines")]
    public void UTC010_StockLinesEmpty() => Check("GoodsHandoverRules.StockLines", "lines=[]", "", () => Stock());
}
