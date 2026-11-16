using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public record CreateCartRequest(IReadOnlyList<CreateCartLine>? Items);
public record CreateCartLine(long ProductId, short Quantity);
// AvailableQuantity = on hand - held for other orders: what can be put into a new cart.
public record StoreProductItem(long ProductId, string Sku, string Name, string? Category, int UnitPriceVnd, int QuantityOnHand, int ReorderLevel, int AvailableQuantity = 0);
public record StoreCartOrderResult(long OrderId, string OrderCode, int TotalAmount, string Status, string? PaymentState, string? HandoverState, IReadOnlyList<StoreCartItemResult> Items, DateTimeOffset? ExpiresAt = null);
public record StoreCartItemResult(long ProductId, string Name, short Quantity, int UnitPriceVnd, int SubtotalVnd);
