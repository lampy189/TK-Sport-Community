using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Repositories;
public interface ISkillProfileStore
{
    Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken);
    Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken);
    Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(long userId, SubmitSkillSurveyRequest request, CancellationToken cancellationToken);
}
