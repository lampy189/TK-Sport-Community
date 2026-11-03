using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;

// UC-31 (team breakdown; RP3 UC-38): Delivery -> Receive order -> Update Stock, the last three cells of SWL 3.
// The sale is deducted once, from the invoiced lines, when the receipt is confirmed.
public sealed partial class GoodsPaymentService
{
    public const string Delivered = "Delivered";
    public const string Received = "Received";
    public const string SaleOut = "SaleOut";

    public Task<GoodsOrderView> DeliverAsync(long id, long actorId, CancellationToken ct) => TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        if (order.HandoverState is Delivered or Received) return await ViewAsync(order, ct);
        if (order.Status != "Invoiced" || order.PaymentState != "Paid" || await InvoiceRecordAsync(id, ct) == null)
            throw Error(409, "Chỉ giao hàng cho đơn đã thanh toán và đã xuất hóa đơn.");
        order.HandoverState = Delivered;
        Audit(id, actorId, "goods_delivered", new { at = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return await ViewAsync(order, ct);
    }, null, ct);

    public Task<GoodsOrderView> ReceiveAsync(long id, long actorId, CancellationToken ct) => TransactionAsync(async () =>
    {
        var order = await LoadAsync(id, ct);
        if (order.HandoverState == Received) return await ViewAsync(order, ct);
        if (order.HandoverState != Delivered) throw Error(409, "Đơn chưa được giao. Cần bấm Giao hàng trước khi xác nhận nhận hàng.");
        var invoice = await InvoiceRecordAsync(id, ct) ?? throw Error(409, "Thiếu hóa đơn của đơn hàng. Cần đối soát.");
        if (!Lines(order).SequenceEqual(invoice.Lines))
            throw Error(409, "Dòng hàng khác với hóa đơn. Cần đối soát trước khi trừ kho.");

        // A retried request must not post the sale twice.
        var posted = await db.StockMovements.AnyAsync(m => m.ReferenceType == "GoodsOrder" && m.ReferenceId == id
            && m.MovementType == SaleOut, ct);
        if (!posted)
        {
            foreach (var line in invoice.Lines.GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, g.First().Name, Quantity = g.Sum(l => l.Quantity) }))
            {
                var stockItemId = await db.StockItems.Where(s => s.ProductId == line.ProductId)
                    .Select(s => (long?)s.Id).SingleOrDefaultAsync(ct)
                    ?? throw Error(409, $"{line.Name} chưa có trong kho. Cần nhập kho trước khi chốt đơn.");
                var updated = await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand - {line.Quantity}
                    WHERE id = {stockItemId} AND quantity_on_hand >= {line.Quantity};
                    """, ct);
                if (updated == 0) throw Error(409, $"Không đủ tồn kho {line.Name} để chốt đơn. Cần kiểm kho/đối soát.");
                db.StockMovements.Add(new StockMovement { StockItemId = stockItemId, MovementType = SaleOut,
                    Quantity = -line.Quantity, ReferenceType = "GoodsOrder", ReferenceId = id, CreatedBy = actorId });
            }
        }
        order.HandoverState = Received;
        order.Status = "Completed";
        Audit(id, actorId, "goods_received", new { at = clock.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return await ViewAsync(order, ct);
    }, null, ct);
}
