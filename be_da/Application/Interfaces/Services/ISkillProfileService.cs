using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface ISkillProfileService
{
    Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken);
    Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken);
    Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(long userId, SubmitSkillSurveyRequest request, CancellationToken cancellationToken);
}
