namespace TKSC.Application.DTOs;

public record CreateCartRequest(IReadOnlyList<CreateCartLine>? Items);
public record CreateCartLine(long ProductId, short Quantity);
public record StoreProductItem(long ProductId, string Sku, string Name, string? Category, int UnitPriceVnd, int QuantityOnHand, int ReorderLevel);
public record StoreCartOrderResult(long OrderId, string OrderCode, int TotalAmount, string Status, string? PaymentState, string? HandoverState, IReadOnlyList<StoreCartItemResult> Items);
public record StoreCartItemResult(long ProductId, string Name, short Quantity, int UnitPriceVnd, int SubtotalVnd);
