using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class SkillProfileService(ISkillProfileStore repository) : ISkillProfileService
{
    public Task<IReadOnlyList<SportRubricDto>> GetActiveRubricsAsync(long? sportId, CancellationToken cancellationToken) => repository.GetActiveRubricsAsync(sportId, cancellationToken);
    public Task<PlayerSkillOverviewDto?> GetPlayerSkillOverviewAsync(long userId, CancellationToken cancellationToken) => repository.GetPlayerSkillOverviewAsync(userId, cancellationToken);
    public Task<SubmitSkillSurveyResult> SubmitSelfAssessmentAsync(long userId, SubmitSkillSurveyRequest request, CancellationToken cancellationToken) => repository.SubmitSelfAssessmentAsync(userId, request, cancellationToken);
}
