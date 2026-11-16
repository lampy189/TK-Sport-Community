namespace TKSC.Application.DTOs;
public sealed record EmailDeliveryItem(long Id, string? Title, string? Status, long? ReferenceId, bool CanRetry);
