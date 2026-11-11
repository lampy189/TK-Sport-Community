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
<<<<<<< HEAD
    string? IneligibilityReason = null
=======
    string? IneligibilityReason = null,
    long VenueId = 1,
    string? VenueAddress = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
)
{
    public short AvailableSlots => (short)Math.Max(0, Capacity - OccupiedPlaces);
    public bool IsFull => AvailableSlots <= 0;
<<<<<<< HEAD
}

=======
    public int DurationMinutes => Math.Max(0, (int)(EndTime - StartTime).TotalMinutes);
    public bool IsHourlyCourt => DurationMinutes > 0 && DurationMinutes <= 60;
    public string SessionType => IsHourlyCourt ? "HourlyCourt" : "Social";
}

public sealed record VenueSummary(
    long Id,
    string Name,
    string? Address
);

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
);
=======
)
{
    public int DurationMinutes => Math.Max(0, (int)(EndTime - StartTime).TotalMinutes);
    public bool IsHourlySlot => DurationMinutes > 0 && DurationMinutes <= 60;
}
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

public sealed record PlayerSportProfileInfo(
    long PlayerId,
    long SportId,
    string? CurrentBand,
    short? CurrentScore,
    string? RatingStatus
);
