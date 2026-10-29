using be_da.Application.Skills;

namespace be_da.Application.Abstractions;

public interface ISkillProfileStore
{
    Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken);
    Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken);
    Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(long userId, SubmitSkillSurveyRequest request, CancellationToken cancellationToken);
}

