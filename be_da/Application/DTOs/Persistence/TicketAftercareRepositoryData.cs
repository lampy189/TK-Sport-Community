namespace TKSC.Application.DTOs.Persistence;
public sealed record AftercareSessionSummary
{
    public long Id { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public string Status { get; init; } = default!;
    public global::System.TimeOnly StartTime { get; init; } = default!;
    public global::System.TimeOnly EndTime { get; init; } = default!;
    public string Sport { get; init; } = default!;
    public string? Court { get; init; } = default!;
    public string? Venue { get; init; } = default!;
    public string? Reason { get; init; } = default!;
}

public sealed record TicketChangeSummary
{
    public long Id { get; init; } = default!;
    public long TicketId { get; init; } = default!;
    public int OldPriceVnd { get; init; } = default!;
    public int NewPriceVnd { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string Holder { get; init; } = default!;
    public string Payer { get; init; } = default!;
    public string? Court { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public global::System.TimeOnly StartTime { get; init; } = default!;
    public global::System.TimeOnly EndTime { get; init; } = default!;
}

public sealed record TicketRefundSummary
{
    public long Id { get; init; } = default!;
    public long TicketId { get; init; } = default!;
    public global::System.Nullable<int> AmountVnd { get; init; } = default!;
    public string? Status { get; init; } = default!;
    public string Recipient { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string ReferenceCode { get; init; } = default!;
}

public sealed record FlaggedBankTransaction
{
    public long Id { get; init; } = default!;
    public string ExternalId { get; init; } = default!;
    public int AmountVnd { get; init; } = default!;
    public string? ReferenceCode { get; init; } = default!;
}

public sealed record FlaggedTicketPayment
{
    public long Id { get; init; } = default!;
    public global::System.Nullable<long> OrderId { get; init; } = default!;
    public string ReferenceCode { get; init; } = default!;
    public int AmountVnd { get; init; } = default!;
    public string Payer { get; init; } = default!;
    public global::System.Collections.Generic.List<FlaggedBankTransaction> Transactions { get; init; } = default!;
}
