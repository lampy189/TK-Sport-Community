namespace TKSC.Application.DTOs.Persistence;
public sealed record GoodsOrderListItem
{
    public long Id { get; init; } = default!;
    public string OrderCode { get; init; } = default!;
    public string? CustomerName { get; init; } = default!;
    public int TotalAmount { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? PaymentState { get; init; } = default!;
    public string? HandoverState { get; init; }
    public string? ReservationState { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
