namespace TKSC.Application.Assessments;

public sealed record CriterionView(long Id, string Code, string Name, short? WeightPercent);
public sealed record RubricView(long Id, long SportId, string Version, string Name,
    bool ScoringSupported, IReadOnlyList<CriterionView> Criteria);
public sealed record AttendeeView(long PlayerId, string Name, long? ProfileId, string? Band,
    string? RatingStatus, long? AssessmentId);
public sealed record MarkInput(long DimensionId, short? Score, string? Evidence, IReadOnlyList<short>? SliderScores = null);
public sealed record SaveAssessmentRequest(long Revision, IReadOnlyList<MarkInput> Marks);
public sealed record PublishAssessmentRequest(long Revision, short? ExpectedScore, string? ExpectedBand);
public sealed record AssessmentView(long Id, long SessionId, long PlayerId, string Status,
    long Revision, RubricView Rubric, IReadOnlyList<MarkInput> Marks, short? TotalScore,
    string? BandResult, short? CurrentScore, string? CurrentBand);
public sealed record AssessmentEvidence(DateTimeOffset At, IReadOnlyList<MarkInput> Marks);

public sealed class AssessmentException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
