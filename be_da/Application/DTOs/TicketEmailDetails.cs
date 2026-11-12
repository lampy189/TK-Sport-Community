namespace TKSC.Application.DTOs;
public sealed class TicketEmailDetails
{
    public string? TicketCode { get; init; }
    public string Status { get; init; } = null!;
    public int AmountVnd { get; init; }
    public DateOnly SessionDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public string? Court { get; init; }
    public string? Venue { get; init; }
    public string Sport { get; init; } = null!;
    public string Holder { get; init; } = null!;
}

public sealed record OutgoingEmail(long EventId, string RecipientName, string RecipientEmail, string Subject, string Body);
