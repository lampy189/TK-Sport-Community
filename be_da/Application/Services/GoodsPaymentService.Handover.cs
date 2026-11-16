using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Validators;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
// UC-31 (team breakdown; RP3 UC-36..UC-38, JOB-04): hold -> pay -> invoice -> deliver -> receive (stock out once).
// An unpaid order can be cancelled or expires; only its own hold is released. Paid goods stay held until handover.
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
        order.DeliveredBy = actorId;
        order.DeliveredAt = clock.GetUtcNow();
        Audit(id, actorId, "goods_delivered", new { at = order.DeliveredAt });
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

    // Staff cancels an unpaid order at the counter; buyerId limits the cancel to the buyer's own online order.
    public Task<GoodsOrderView> CancelAsync(long id, long actorId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        await CancelCoreAsync(order, actorId, ct);
        return await ViewAsync(order, ct);
    }, null, ct);
    public Task<BuyerGoodsOrder> CancelAsBuyerAsync(long id, long buyerId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        if (order.BuyerId != buyerId)
            throw Error(404, "Không tìm thấy đơn hàng của bạn.");
        await CancelCoreAsync(order, buyerId, ct);
        return ToBuyerView(order);
    }, null, ct);

    // JOB-04: release expired unpaid holds. Each order is rechecked in its own protected transaction, so a payment
    // confirmed meanwhile keeps its goods, and a retry never releases the same hold twice.
    public async Task<int> ExpireDueAsync(CancellationToken ct)
    {
        var expired = 0;
        foreach (var id in await repository.DueForExpiryAsync(clock.GetUtcNow(), 50, ct))
        {
            var done = await repository.TransactionAsync(async () =>
            {
                var order = await LoadAsync(id, ct);
                var now = clock.GetUtcNow();
                if (!GoodsReservationRules.IsDueForExpiry(order.Status, order.PaymentState, order.ReservationState, order.ExpiresAt, HasPaidPayment(order), now))
                    return false;
                order.Status = GoodsReservationRules.Expired;
                foreach (var payment in order.Payments.Where(p => p.Status == "pending"))
                    payment.Status = "expired";
                await ReleaseHoldAsync(order, ct);
                Audit(id, null, "goods_expired", new { at = now, order.ExpiresAt });
                await repository.SaveAsync(ct);
                return true;
            }, null, ct);
            if (done)
                expired++;
        }

        return expired;
    }

    private async Task CancelCoreAsync(Order order, long actorId, CancellationToken ct)
    {
        if (order.Status == GoodsReservationRules.Cancelled)
            return;
        if (GoodsReservationRules.ValidateCancel(order.Status, order.PaymentState, HasPaidPayment(order)) is { } error)
            throw Error(409, error);
        order.Status = GoodsReservationRules.Cancelled;
        order.CancelledBy = actorId;
        order.CancelledAt = clock.GetUtcNow();
        // A transfer that still arrives is kept for review by the bank-event handler; it never re-opens the order.
        foreach (var payment in order.Payments.Where(p => p.Status == "pending"))
            payment.Status = "cancelled";
        await ReleaseHoldAsync(order, ct);
        Audit(order.Id, actorId, "goods_cancelled", new { at = order.CancelledAt });
        await repository.SaveAsync(ct);
    }

    // Holds legacy carts (created before reservations existed) when payment starts; held orders are left alone.
    private async Task EnsureHeldAsync(Order order, CancellationToken ct)
    {
        if (order.ReservationState == GoodsReservationRules.Held)
            return;
        if (order.ReservationState is not null)
            throw Error(409, "Hàng của đơn này đã được trả lại kho. Cần tạo đơn mới.");
        if (await repository.ReserveStockAsync(order.Id, ct) is long shortProduct)
            throw Error(409, $"Không đủ hàng {await repository.ProductNameAsync(shortProduct, ct)} có thể bán cho đơn này. Cần giảm số lượng hoặc tạo đơn mới.");
        order.ReservationState = GoodsReservationRules.Held;
        order.ExpiresAt ??= GoodsReservationRules.HoldUntil(clock.GetUtcNow(), options?.Value.HoldMinutes ?? new GoodsOrderOptions().HoldMinutes);
    }

    private async Task ReleaseHoldAsync(Order order, CancellationToken ct)
    {
        if (order.ReservationState != GoodsReservationRules.Held)
            return;
        await repository.ReleaseStockAsync(order.Id, ct);
        order.ReservationState = GoodsReservationRules.Released;
    }

    private static bool HasPaidPayment(Order order) => order.Payments.Any(p => p.Status is "paid" or "review_required");
    private static bool CanCancel(Order order) => order.Status != GoodsReservationRules.Cancelled && GoodsReservationRules.ValidateCancel(order.Status, order.PaymentState, HasPaidPayment(order)) is null;
    private static BuyerGoodsOrder ToBuyerView(Order order) => new(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, Lines(order), order.ReservationState == GoodsReservationRules.Held ? order.ExpiresAt : null, CanCancel(order));
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
        // One SaleOut per product, posted once (a retry finds the movement and posts nothing).
        var held = order.ReservationState == GoodsReservationRules.Held;
        var result = await repository.ConsumeStockAsync(id, actorId, held, ct);
        if (result > 0)
            throw Error(409, $"Không đủ tồn kho {await repository.ProductNameAsync(result, ct)} để chốt đơn. Cần kiểm kho/đối soát.");
        if (held)
            order.ReservationState = GoodsReservationRules.Consumed;
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
