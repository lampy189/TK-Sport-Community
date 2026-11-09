namespace be_da.Application.Sessions;
public sealed record LateEntryOffer(long SessionId, long VenueId, string VenueName, string CourtName,
    string SportName, string? Band, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity,
    int Playing, int Confirmed, int Holds, int Available, int Remaining, int PriceVnd, bool IsOpen, long Revision);
public sealed record PublishLateEntryRequest(int Seats, int PriceVnd, long ExpectedRevision);
public sealed record HoldLateEntryRequest(long ExpectedRevision);
public sealed record StopLateEntryRequest(long ExpectedRevision);
