namespace be_da.Application.DTOs;
public sealed record PlayerSportSkillProfileDto(long? ProfileId, long SportId, string SportCode, string SportName, short? CurrentScore, string? CurrentBand, string RatingStatus, SportRubricDto? ActiveRubric, AssessmentResultDto? LatestAssessment, IReadOnlyList<RatingHistoryItemDto> RatingHistory);
