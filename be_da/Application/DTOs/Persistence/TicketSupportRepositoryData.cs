namespace TKSC.Application.DTOs.Persistence;
public sealed record TicketSupportInboxItem
{
    public long Id { get; init; } = default!;
    public long TicketId { get; init; } = default!;
    public string Holder { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? LastMessage { get; init; } = default!;
    public bool LastIsStaff { get; init; } = default!;
    public string? LastSender { get; init; } = default!;
    public string? RefundStatus { get; init; } = default!;
}

public sealed record TicketSupportMessageItem
{
    public long Id { get; init; } = default!;
    public string Content { get; init; } = default!;
    public global::System.DateTimeOffset CreatedAt { get; init; } = default!;
    public bool IsStaff { get; init; } = default!;
    public string Sender { get; init; } = default!;
    public bool HasImage { get; init; } = default!;
}
