namespace be_da.Application.DTOs;
public sealed record SubmitSkillSurveyResult(bool Success, string? ErrorCode, string? Message, PlayerSportSkillProfileDto? Profile = null);
