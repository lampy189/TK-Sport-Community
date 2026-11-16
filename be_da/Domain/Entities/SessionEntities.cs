namespace be_da.Domain.Entities;

public sealed record SessionSummary(
    long Id,
    long SportId,
    string SportCode,
    string SportName,
    long CourtId,
    string CourtName,
    string VenueName,
    long TimeSlotId,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string TimeLabel,
    DateOnly SessionDate,
    string? Band,
    short Capacity,
    int PriceVnd,
    string Status,
    long CreatedBy,
    short OccupiedPlaces,
    bool? IsEligibleSuggestion = null,
    string? IneligibilityReason = null
)
{
    public short AvailableSlots => (short)Math.Max(0, Capacity - OccupiedPlaces);
    public bool IsFull => AvailableSlots <= 0;
}

public sealed record SessionParticipant(
    long TicketId,
    long PlayerId,
    string FullName,
    string? AvatarUrl,
    string? CurrentBand,
    string TicketStatus
);

public sealed record SessionDetails(
    SessionSummary Summary,
    IReadOnlyList<SessionParticipant> PublicParticipants
);

public sealed record TimeSlotSummary(
    long Id,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string Label
);

public sealed record PlayerSportProfileInfo(
    long PlayerId,
    long SportId,
    string? CurrentBand,
    short? CurrentScore,
    string? RatingStatus
);
