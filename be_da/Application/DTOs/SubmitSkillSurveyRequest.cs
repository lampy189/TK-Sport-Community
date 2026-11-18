namespace be_da.Application.DTOs;
public sealed record SubmitSkillSurveyRequest(long SportId, IReadOnlyList<SurveyDimensionAnswerInput> Answers, string? ExperienceContext = null, string? FrequencyContext = null);
