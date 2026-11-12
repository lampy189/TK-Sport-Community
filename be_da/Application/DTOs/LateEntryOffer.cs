namespace be_da.Application.DTOs;
public sealed record LateEntryOffer(long SessionId, long VenueId, string VenueName, string CourtName, string SportName, string? Band, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int Capacity, int Playing, int Confirmed, int Holds, int Available, int Remaining, int PriceVnd, bool IsOpen, long Revision, int BasePriceVnd = 0, decimal PricePercent = 100);
