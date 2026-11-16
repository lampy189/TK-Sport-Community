using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Application.Services;
public sealed class StoreService(IStoreRepository repository) : IStoreService
{
    public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => repository.ProductsAsync(scoped, venueId, ct);
    public async Task<OperationResult> CreateCartAsync(CreateCartRequest request, long userId, bool isPlayer, string? customerName, bool scoped, long? venueId, CancellationToken ct)
    {
        var items = request.Items ?? [];
        if (items.Count == 0)
            return OperationResult.Invalid(new { code = "empty_cart", message = "Vui lòng chọn ít nhất một món hàng." });
        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var allowed = await repository.CenterProductIdsAsync(scoped, venueId, ct);
        if (scoped && venueId is null)
            return OperationResult.Invalid(new { code = "center_required", message = "Chọn một cơ sở trước khi tạo đơn hàng." });
        if (allowed is not null && productIds.Except(allowed).Any())
            return OperationResult.Missing(new { code = "product_not_found", message = "Sản phẩm không thuộc cơ sở đang chọn." });
        var products = await repository.FindProductsAsync(productIds);
        if (products.Count != productIds.Count)
            return OperationResult.Missing(new { code = "product_not_found", message = "Một số sản phẩm không còn mở bán hoặc không tồn tại." });
        var orderItems = new List<StoreCartItemResult>();
        var totalAmount = 0;
        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return OperationResult.Invalid(new { code = "invalid_quantity", message = "Số lượng phải lớn hơn 0." });
            var product = products.SingleOrDefault(candidate => candidate.Id == item.ProductId) ?? throw new InvalidOperationException("Product not found while building cart.");
            var quantityOnHand = product.StockItem?.QuantityOnHand ?? 0;
            if (item.Quantity > quantityOnHand)
                return OperationResult.Conflict(new { code = "stock_unavailable", message = $"Sản phẩm '{product.Name}' chỉ còn {quantityOnHand} trong kho. Vui lòng giảm số lượng đặt hàng." });
            var subtotal = item.Quantity * product.UnitPriceVnd;
            totalAmount += subtotal;
            orderItems.Add(new StoreCartItemResult(product.Id, product.Name, item.Quantity, product.UnitPriceVnd, subtotal));
        }

        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var order = new Order
        {
            OrderCode = orderCode,
            StaffId = userId,
            BuyerId = isPlayer ? userId : null,
            CustomerName = customerName ?? "Khách hàng",
            TotalAmount = totalAmount,
            Status = "Cart",
            PaymentState = "Unpaid",
            HandoverState = "Pending"
        };
        await repository.SaveCartAsync(order, orderItems, products, scoped, venueId);
        return OperationResult.Success(new StoreCartOrderResult(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, orderItems));
    }
}
