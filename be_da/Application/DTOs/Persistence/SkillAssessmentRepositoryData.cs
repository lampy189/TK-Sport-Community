namespace TKSC.Application.DTOs.Persistence;
public sealed record AssignedAssessmentSession
{
    public long Id { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Court { get; init; } = default!;
    public global::System.TimeOnly StartTime { get; init; } = default!;
}

public sealed record AssessmentPlayerProfile
{
    public global::System.Nullable<short> CurrentScore { get; init; } = default!;
    public string? CurrentBand { get; init; } = default!;
    public string? RatingStatus { get; init; } = default!;
    public string Sport { get; init; } = default!;
}

public sealed record AssessmentParticipation
{
    public long SessionId { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public global::System.TimeOnly StartTime { get; init; } = default!;
    public string? Court { get; init; } = default!;
}

public sealed record AssessmentRatingEntry
{
    public long Id { get; init; } = default!;
    public global::System.Nullable<short> NewScore { get; init; } = default!;
    public string? NewBand { get; init; } = default!;
    public global::System.Nullable<short> OldScore { get; init; } = default!;
    public string? OldBand { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public string Rubric { get; init; } = default!;
}

public sealed record AssessmentAttendeeIdentity
{
    public long PlayerId { get; init; } = default!;
    public string FullName { get; init; } = default!;
}
