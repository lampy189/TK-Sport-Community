using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;

public sealed partial class GoodsPaymentService
{
    public Task<GoodsOrderView> CancelAsync(long id, long actorId, long? buyerId, CancellationToken ct) => repository.TransactionAsync(async () =>
    {
        var order = await LoadForPaymentAsync(id, buyerId, ct);
        if (order.Status == "Cancelled") return await ViewAsync(order, ct);
        if (order.PaymentState is "Paid" or "NeedsReview" || order.Payments.Any(p => p.Status is "paid" or "review_required"))
            throw Error(409,"Đơn đã nhận tiền hoặc đang đối soát. Không thể hủy giữ hàng.");
        if (order.HandoverState is Delivered or Received) throw Error(409,"Đơn đã bàn giao.");
        await repository.ReleaseHoldAsync(id, ct);
        foreach (var payment in order.Payments.Where(p=>p.Status=="pending")) payment.Status="failed";
        order.Status="Cancelled"; order.PaymentState="Cancelled";
        Audit(id, actorId, "goods_cancelled", new { at=clock.GetUtcNow() });
        await repository.SaveAsync(ct);
        return await ViewAsync(order,ct);
    }, null, ct);

    // Ownership is checked on every load, including both sides of the external payOS call.
    private async Task<Order> LoadForPaymentAsync(long id, long? buyerId, CancellationToken ct)
    {
        var order = await LoadAsync(id, ct);
        if (buyerId.HasValue && (buyerId <= 0 || order.BuyerId != buyerId))
            throw Error(404, "Không tìm thấy đơn hàng hóa.");
        return order;
    }

    public async Task<GoodsOrderView> ReadForBuyerAsync(long id, long buyerId, CancellationToken ct) =>
        await ViewAsync(await LoadForPaymentAsync(id, buyerId, ct), ct);

    public Task<GoodsOrderView> CheckoutForBuyerAsync(long id, long buyerId, CancellationToken ct) =>
        InstructionsCoreAsync(id, buyerId, buyerId, ct);

    public Task<GoodsOrderView> RefreshForBuyerAsync(long id, long buyerId, CancellationToken ct) =>
        ConfirmTransferCoreAsync(id, buyerId, ct);

    public async Task<GoodsInvoice> InvoiceForBuyerAsync(long id, long buyerId, CancellationToken ct)
    {
        await LoadForPaymentAsync(id, buyerId, ct);
        return await InvoiceRecordAsync(id, ct) ?? throw Error(409, "Đơn chưa được xuất hóa đơn.");
    }
}
