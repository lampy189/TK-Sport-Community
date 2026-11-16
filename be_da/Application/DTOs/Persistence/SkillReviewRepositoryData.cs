namespace TKSC.Application.DTOs.Persistence;
public sealed record SkillExceptionSummary
{
    public long Id { get; init; } = default!;
    public long SessionId { get; init; } = default!;
    public long PlayerId { get; init; } = default!;
    public string Player { get; init; } = default!;
    public string? Reason { get; init; } = default!;
    public global::System.Nullable<long> ApprovedBy { get; init; } = default!;
    public global::System.DateOnly SessionDate { get; init; } = default!;
    public string Sport { get; init; } = default!;
    public string? SessionBand { get; init; } = default!;
    public string? CurrentBand { get; init; } = default!;
}

public sealed record SkillAppealSummary
{
    public long Id { get; init; } = default!;
    public global::System.Nullable<long> AssessmentId { get; init; } = default!;
    public string? Status { get; init; } = default!;
    public string Player { get; init; } = default!;
    public string Sport { get; init; } = default!;
    public global::System.Nullable<short> CurrentScore { get; init; } = default!;
    public string? CurrentBand { get; init; } = default!;
    public global::System.Nullable<short> DisputedScore { get; init; } = default!;
    public string? DisputedBand { get; init; } = default!;
    public long ExpectedHistoryId { get; init; } = default!;
}

public sealed record SkillAppealHistoryEntry
{
    public long Id { get; init; } = default!;
    public global::System.Nullable<short> OldScore { get; init; } = default!;
    public global::System.Nullable<short> NewScore { get; init; } = default!;
    public string? OldBand { get; init; } = default!;
    public string? NewBand { get; init; } = default!;
}
