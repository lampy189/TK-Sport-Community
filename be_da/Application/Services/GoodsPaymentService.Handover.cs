using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
// UC-31 (team breakdown; RP3 UC-38): Delivery -> Receive order -> Update Stock, the last three cells of SWL 3.
// The sale is deducted once, from the invoiced lines, when the receipt is confirmed.
public sealed partial class GoodsPaymentService
{
    public const string Delivered = "Delivered";
    public const string Received = "Received";
    public const string SaleOut = "SaleOut";
    public Task<GoodsOrderView> DeliverAsync(long id, long actorId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        if (order.HandoverState is Delivered or Received)
            return await ViewAsync(order, ct);
        if (GoodsHandoverRules.ValidateDeliver(order.Status, order.PaymentState, await InvoiceRecordAsync(id, ct) != null)is { } error)
            throw Error(409, error);
        order.HandoverState = Delivered;
        Audit(id, actorId, "goods_delivered", new { at = clock.GetUtcNow() });
        await repository.SaveAsync(ct);
        return await ViewAsync(order, ct);
    }, null, ct);
    public Task<GoodsOrderView> ReceiveAsync(long id, long actorId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        await ConfirmReceiptAsync(order, actorId, ct);
        return await ViewAsync(order, ct);
    }, null, ct);
    // UC-31: the buyer of an online order confirms receipt. Other people's orders look missing.
    public Task<BuyerGoodsOrder> ReceiveAsBuyerAsync(long id, long buyerId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        if (order.BuyerId != buyerId)
            throw Error(404, "Không tìm thấy đơn hàng của bạn.");
        await ConfirmReceiptAsync(order, buyerId, ct);
        return ToBuyerView(order);
    }, null, ct);
    public async Task<IReadOnlyList<BuyerGoodsOrder>> ListForBuyerAsync(long buyerId, CancellationToken ct)
    {
        var orders = await repository.ListForBuyerOrdersListAsync(buyerId, ct);
        return orders.Select(ToBuyerView).ToList();
    }

    private static BuyerGoodsOrder ToBuyerView(Order order) => new(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, Lines(order));
    private async Task ConfirmReceiptAsync(Order order, long actorId, CancellationToken ct)
    {
        var id = order.Id;
        if (order.HandoverState == Received)
            return;
        if (GoodsHandoverRules.ValidateReceive(order.HandoverState)is { } stateError)
            throw Error(409, stateError);
        var invoice = await InvoiceRecordAsync(id, ct) ?? throw Error(409, "Thiếu hóa đơn của đơn hàng. Cần đối soát.");
        if (!Lines(order).SequenceEqual(invoice.Lines))
            throw Error(409, "Dòng hàng khác với hóa đơn. Cần đối soát trước khi trừ kho.");
        // A retried request must not post the sale twice.
        var posted = await repository.ConfirmReceiptStockMovementsExistsAsync(ct, id);
        if (!posted)
        {
            foreach (var line in GoodsHandoverRules.StockLines(invoice.Lines))
            {
                var stockItemId = await repository.ConfirmReceiptStockItemsAsync(ct, line) ?? throw Error(409, $"{line.Name} chưa có trong kho. Cần nhập kho trước khi chốt đơn.");
                var updated = await repository.ConfirmReceiptDatabaseUpdateAsync(ct, line, stockItemId, id);
                if (updated == 0)
                    throw Error(409, $"Không đủ tồn kho {line.Name} để chốt đơn. Cần kiểm kho/đối soát.");
                repository.AddStockMovements(new StockMovement { StockItemId = stockItemId, MovementType = SaleOut, Quantity = -line.Quantity, ReferenceType = "GoodsOrder", ReferenceId = id, CreatedBy = actorId });
            }
        }

        order.HandoverState = Received;
        order.Status = "Completed";
        Audit(id, actorId, "goods_received", new { at = clock.GetUtcNow() });
        await repository.SaveAsync(ct);
    }
}

public static class GoodsHandoverRules
{
    public static string? ValidateDeliver(string status, string? paymentState, bool hasInvoice) => status == "Invoiced" && paymentState == "Paid" && hasInvoice ? null : "Chỉ giao hàng cho đơn đã thanh toán và đã xuất hóa đơn.";
    public static string? ValidateReceive(string? handoverState) => handoverState == GoodsPaymentService.Delivered ? null : "Đơn chưa được giao. Cần bấm Giao hàng trước khi xác nhận nhận hàng.";
    // order_items.quantity is in the base unit, so invoice lines deduct directly; one product posts one movement.
    public static IReadOnlyList<GoodsStockLine> StockLines(IEnumerable<GoodsLine> lines) => lines.GroupBy(l => l.ProductId).Select(g => new GoodsStockLine(g.Key, g.First().Name, g.Sum(l => l.Quantity))).ToList();
}
