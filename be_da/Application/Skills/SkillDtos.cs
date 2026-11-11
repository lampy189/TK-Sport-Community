namespace be_da.Application.Skills;

public sealed record RubricDimensionDto(
    long Id,
    string Code,
    string Name,
    short WeightPercent
);

public sealed record SportRubricDto(
    long Id,
    long SportId,
    string SportCode,
    string SportName,
    string Version,
    string Name,
    IReadOnlyList<RubricDimensionDto> Dimensions
);

public sealed record AssessmentDimensionScoreDto(
    long DimensionId,
    string DimensionCode,
    string DimensionName,
    short WeightPercent,
    short Score,
<<<<<<< HEAD
    short WeightedPoints
=======
    decimal WeightedPoints,
    string? Evidence = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
);

public sealed record AssessmentResultDto(
    long Id,
    long RubricId,
    string RubricName,
    string RubricVersion,
    string AssessmentType,
    string Status,
    short TotalScore,
    string BandResult,
    long AssessorId,
    string AssessorName,
    string AssessorRole,
    long? SessionId,
    DateOnly? SessionDate,
    string? CourtName,
    string? TimeLabel,
<<<<<<< HEAD
    IReadOnlyList<AssessmentDimensionScoreDto> Scores
=======
    IReadOnlyList<AssessmentDimensionScoreDto> Scores,
    string? VenueName = null,
    string? PublishReason = null,
    DateTimeOffset? EvaluatedAt = null,
    short? OldScore = null,
    string? OldBand = null,
    short? PublishedScore = null,
    string? PublishedBand = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
);

public sealed record RatingHistoryItemDto(
    long Id,
    short? OldScore,
    short? NewScore,
    string? OldBand,
    string? NewBand,
    long ChangedByUserId,
    string ChangedByName,
    string ChangedByRole,
    long? AssessmentId,
    string? AssessmentType,
    long? SessionId,
    DateOnly? SessionDate,
<<<<<<< HEAD
    string? CourtName
=======
    string? CourtName,
    AssessmentResultDto? AssessmentDetails = null
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
);

public sealed record PlayerSportSkillProfileDto(
    long? ProfileId,
    long SportId,
    string SportCode,
    string SportName,
    short? CurrentScore,
    string? CurrentBand,
    string RatingStatus,
    SportRubricDto? ActiveRubric,
    AssessmentResultDto? LatestAssessment,
    IReadOnlyList<RatingHistoryItemDto> RatingHistory
);

public sealed record PlayerSkillOverviewDto(
    long PlayerId,
    long UserId,
    string FullName,
    string? AvatarUrl,
    IReadOnlyList<PlayerSportSkillProfileDto> Profiles
);

public sealed record SurveyDimensionAnswerInput(
    long DimensionId,
    short Score
);

public sealed record SubmitSkillSurveyRequest(
    long SportId,
    IReadOnlyList<SurveyDimensionAnswerInput> Answers,
    string? ExperienceContext = null,
    string? FrequencyContext = null
);

public sealed record SubmitSkillSurveyResult(
    bool Success,
    string? ErrorCode,
    string? Message,
    PlayerSportSkillProfileDto? Profile = null
);

