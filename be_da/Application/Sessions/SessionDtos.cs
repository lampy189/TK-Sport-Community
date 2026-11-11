using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Sessions;

public sealed record SessionFilter(
    long? SportId = null,
    string? Sport = null,
    DateOnly? Date = null,
    long? TimeSlotId = null,
    string? Status = null,
    DateOnly? FromDate = null,
<<<<<<< HEAD
    DateOnly? ToDate = null
=======
    DateOnly? ToDate = null,
    long? VenueId = null,
    string? Venue = null,
    string? SessionType = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
);

public sealed record RegisterSessionResult(
    bool Success,
    string? ErrorCode,
    string? Message,
    string? Reason = null,
    long? OrderId = null,
    string? OrderCode = null,
    long? TicketId = null,
    string? TicketCode = null,
    int? AmountVnd = null,
    DateTimeOffset? ExpiresAt = null
);
