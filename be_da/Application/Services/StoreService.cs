using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Application.Validators;
using TKSC.Domain.Entities;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using Microsoft.Extensions.Options;

namespace TKSC.Application.Services;
// RP3 UC-36: a goods order holds its items from the moment it is created (task split 13/11/2026, Dũng).
public sealed class StoreService(IStoreRepository repository, TimeProvider? clock = null, IOptions<GoodsOrderOptions>? options = null) : IStoreService
{
    public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => repository.ProductsAsync(scoped, venueId, ct);
    public async Task<OperationResult> CreateCartAsync(CreateCartRequest request, long userId, bool isPlayer, string? customerName, bool scoped, long? venueId, CancellationToken ct)
    {
        var items = request.Items ?? [];
        if (GoodsReservationRules.ValidateCart(items) is { } invalid)
            return OperationResult.Invalid(new { code = items.Count == 0 ? "empty_cart" : "invalid_quantity", message = invalid });
        var lines = GoodsReservationRules.MergeCartLines(items);
        var productIds = lines.Select(line => line.ProductId).ToList();
        var allowed = await repository.CenterProductIdsAsync(scoped, venueId, ct);
        if (scoped && venueId is null)
            return OperationResult.Invalid(new { code = "center_required", message = "Chọn một cơ sở trước khi tạo đơn hàng." });
        if (allowed is not null && productIds.Except(allowed).Any())
            return OperationResult.Missing(new { code = "product_not_found", message = "Sản phẩm không thuộc cơ sở đang chọn." });
        var products = await repository.FindProductsAsync(productIds);
        if (products.Count != productIds.Count)
            return OperationResult.Missing(new { code = "product_not_found", message = "Một số sản phẩm không còn mở bán hoặc không tồn tại." });
        var orderItems = new List<StoreCartItemResult>();
        var totalAmount = 0L;
        foreach (var line in lines)
        {
            var product = products.Single(candidate => candidate.Id == line.ProductId);
            var available = Available(product);
            if (line.Quantity > available)
                return Unavailable(product.Name, available);
            var quantity = (short)line.Quantity; // ValidateCart: at most short.MaxValue (order_items.quantity is smallint)
            var subtotal = (long)quantity * product.UnitPriceVnd;
            totalAmount += subtotal;
            if (subtotal > int.MaxValue || totalAmount > int.MaxValue)
                return OperationResult.Invalid(new { code = "invalid_quantity", message = "Tổng tiền đơn hàng vượt giới hạn cho phép." });
            orderItems.Add(new StoreCartItemResult(product.Id, product.Name, quantity, product.UnitPriceVnd, (int)subtotal));
        }

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        var order = new Order
        {
            OrderCode = $"ORD-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
            StaffId = userId,
            BuyerId = isPlayer ? userId : null,
            CustomerName = customerName ?? "Khách hàng",
            TotalAmount = (int)totalAmount,
            Status = "Cart",
            PaymentState = "Unpaid",
            HandoverState = "Pending",
            CreatedAt = now,
            ExpiresAt = GoodsReservationRules.HoldUntil(now, options?.Value.HoldMinutes ?? new GoodsOrderOptions().HoldMinutes),
            ReservationState = GoodsReservationRules.Held
        };
        // Someone may have taken the last items since the check above; the reservation decides.
        var shortProduct = await repository.SaveCartAsync(order, orderItems, products, scoped, venueId);
        if (shortProduct is long id)
        {
            var product = products.Single(p => p.Id == id);
            return Unavailable(product.Name, Available(product));
        }

        return OperationResult.Success(new StoreCartOrderResult(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, orderItems, order.ExpiresAt));
    }

    private static int Available(Product product) => product.StockItem is null ? 0 : product.StockItem.QuantityOnHand - product.StockItem.ReservedQuantity;
    private static OperationResult Unavailable(string name, int available) => OperationResult.Conflict(new { code = "stock_unavailable", message = $"Sản phẩm '{name}' chỉ còn {Math.Max(available, 0)} có thể bán (đã trừ hàng đang giữ cho đơn khác). Vui lòng giảm số lượng." });
}
