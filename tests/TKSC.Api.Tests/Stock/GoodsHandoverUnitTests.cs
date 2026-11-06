using TKSC.Application.Services;
using Xunit;

namespace TKSC.Api.Tests.Stock;

public sealed class GoodsHandoverUnitTests
{
    private static GoodsLine Line(long product, short quantity) => new(product, $"P{product}", quantity, 1000, quantity * 1000);

    [UnitCase("UTC001", "ValidateDeliver", "Normal", "status=Invoiced; payment=Paid; invoice exists", "Return: null (can deliver)")]
    public void ValidateDeliver_Ready() => Assert.Null(GoodsHandoverRules.ValidateDeliver("Invoiced", "Paid", true));

    [UnitCase("UTC002", "ValidateDeliver", "Abnormal", "status=Cart; payment=Unpaid; no invoice", "Return: must be paid and invoiced message")]
    public void ValidateDeliver_Cart() => Assert.NotNull(GoodsHandoverRules.ValidateDeliver("Cart", "Unpaid", false));

    [UnitCase("UTC003", "ValidateDeliver", "Abnormal", "status=Paid; payment=Paid; no invoice", "Return: must be paid and invoiced message")]
    public void ValidateDeliver_NoInvoice() => Assert.NotNull(GoodsHandoverRules.ValidateDeliver("Paid", "Paid", false));

    [UnitCase("UTC004", "ValidateDeliver", "Abnormal", "status=Invoiced; payment=NeedsReview; invoice exists", "Return: must be paid and invoiced message")]
    public void ValidateDeliver_NeedsReview() => Assert.NotNull(GoodsHandoverRules.ValidateDeliver("Invoiced", "NeedsReview", true));

    [UnitCase("UTC005", "ValidateReceive", "Normal", "handover=Delivered", "Return: null (can confirm)")]
    public void ValidateReceive_Delivered() => Assert.Null(GoodsHandoverRules.ValidateReceive("Delivered"));

    [UnitCase("UTC006", "ValidateReceive", "Abnormal", "handover=Pending", "Return: not delivered yet message")]
    public void ValidateReceive_Pending() => Assert.NotNull(GoodsHandoverRules.ValidateReceive("Pending"));

    [UnitCase("UTC007", "ValidateReceive", "Abnormal", "handover=null", "Return: not delivered yet message")]
    public void ValidateReceive_Null() => Assert.NotNull(GoodsHandoverRules.ValidateReceive(null));

    [UnitCase("UTC008", "StockLines", "Normal", "product 1: 2 + 3; product 2: 1", "Return: product 1=5; product 2=1")]
    public void StockLines_Groups()
    {
        var lines = GoodsHandoverRules.StockLines([Line(1, 2), Line(1, 3), Line(2, 1)]);
        Assert.Equal(5, lines.Single(l => l.ProductId == 1).Quantity);
        Assert.Equal(1, lines.Single(l => l.ProductId == 2).Quantity);
    }

    [UnitCase("UTC009", "StockLines", "Normal", "one line: product 1 quantity 4", "Return: one line quantity=4")]
    public void StockLines_Single() => Assert.Equal(4, Assert.Single(GoodsHandoverRules.StockLines([Line(1, 4)])).Quantity);

    [UnitCase("UTC010", "StockLines", "Boundary", "no lines", "Return: empty list")]
    public void StockLines_Empty() => Assert.Empty(GoodsHandoverRules.StockLines([]));
}
