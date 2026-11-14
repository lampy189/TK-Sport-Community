using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace TKSC.Application.Services;
public sealed class StoreService(IStoreRepository repository, TimeProvider clock) : IStoreService
{
    public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => repository.ProductsAsync(scoped, venueId, ct);
    public async Task<OperationResult> CreateCartAsync(CreateCartRequest request, long userId, bool isPlayer, string? customerName, bool scoped, long? venueId, string? idempotencyKey, CancellationToken ct)
    {
        var rawItems = request.Items ?? [];
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length is < 16 or > 100
            || idempotencyKey.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_' and not '.'))
            return OperationResult.Invalid(new { code = "IDEMPOTENCY_KEY_REQUIRED", message = "Thiếu khóa tạo đơn. Vui lòng thử lại." });
        if (rawItems.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
            return OperationResult.Invalid(new { code = "INVALID_QUANTITY", message = "Sản phẩm và số lượng phải hợp lệ." });
        var items = rawItems.GroupBy(item => item.ProductId).Select(group =>
        {
            var quantity = group.Sum(item => (int)item.Quantity);
            return new CreateCartLine(group.Key, quantity <= short.MaxValue ? (short)quantity : (short)0);
        }).ToList();
        if (items.Any(item => item.Quantity <= 0))
            return OperationResult.Invalid(new { code = "INVALID_QUANTITY", message = "Tổng số lượng của một sản phẩm vượt giới hạn." });
        if (items.Count == 0)
            return OperationResult.Invalid(new { code = "empty_cart", message = "Vui lòng chọn ít nhất một món hàng." });
        var stableItems = string.Join(';', items.OrderBy(item => item.ProductId).Select(item => $"{item.ProductId}:{item.Quantity}"));
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stableItems)));
        var previous = await repository.FindIdempotentCartAsync(userId, idempotencyKey.Trim(), fingerprint, ct);
        if (previous is not null)
            return previous;

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
        long totalAmount = 0;
        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return OperationResult.Invalid(new { code = "invalid_quantity", message = "Số lượng phải lớn hơn 0." });
            var product = products.SingleOrDefault(candidate => candidate.Id == item.ProductId) ?? throw new InvalidOperationException("Product not found while building cart.");
            var subtotal = item.Quantity * product.UnitPriceVnd;
            totalAmount += subtotal;
            if (totalAmount > int.MaxValue)
                return OperationResult.Invalid(new { code = "ORDER_TOTAL_TOO_LARGE", message = "Tổng tiền vượt giới hạn cho phép." });
            orderItems.Add(new StoreCartItemResult(product.Id, product.Name, item.Quantity, product.UnitPriceVnd, subtotal));
        }

        var expiresAt = clock.GetUtcNow().AddMinutes(15);
        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var order = new Order
        {
            OrderCode = orderCode,
            StaffId = userId,
            BuyerId = isPlayer ? userId : null,
            CustomerName = customerName ?? "Khách hàng",
            TotalAmount = (int)totalAmount,
            Status = "Cart",
            PaymentState = "Unpaid",
            HandoverState = "Pending"
        };
        return await repository.SaveCartAsync(order, orderItems, products, userId, scoped, venueId, idempotencyKey.Trim(), fingerprint, expiresAt, ct);
    }
}
