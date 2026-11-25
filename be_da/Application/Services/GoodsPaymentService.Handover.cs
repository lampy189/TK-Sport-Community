using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
// UC-31 (team breakdown; RP3 UC-38): Delivery -> Receive order -> Update Stock, the last three cells of SWL 3.
// Staff delivery consumes reserved stock once, using the invoiced lines.
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
        var invoice = await InvoiceRecordAsync(id, ct);
        if (invoice is null && order.Status == "Paid" && order.PaymentState == "Paid")
            invoice = await IssueInvoiceAsync(order, actorId, ct, delivering: true);
        if (GoodsHandoverRules.ValidateDeliver(order.Status, order.PaymentState, invoice != null) is { } error)
            throw Error(409, error);
        if (!Lines(order).SequenceEqual(invoice!.Lines)) throw Error(409,"Dòng hàng khác hóa đơn.");
        await repository.ConsumeHoldAsync(order, actorId, ct);
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

    // Same rule as CancelAsync (GoodsPaymentService.Buyer): only unpaid, not handed-over orders; the UI shows the button.
    private static bool HasPaidPayment(Order order) => order.Payments.Any(p => p.Status is "paid" or "review_required");
    private static bool CanCancel(Order order) => order.Status is not ("Cancelled" or "Completed") && order.PaymentState is not ("Paid" or "NeedsReview" or "Cancelled")
        && !HasPaidPayment(order) && order.HandoverState is not (Delivered or Received);
    private static BuyerGoodsOrder ToBuyerView(Order order) => new(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, Lines(order, includeSku: true), CanCancel(order));
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
        // Delivery already atomically converted the reservation to SaleOut.
        // Legacy deliveries can still consume a committed hold at receipt.
        if (!await repository.ConfirmReceiptStockMovementsExistsAsync(ct, id))
            await repository.ConsumeHoldAsync(order, actorId, ct);

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
