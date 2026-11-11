namespace TKSC.Application.Services;

public interface ISkillReviewService
{
    Task<object> Exceptions(long actor, string role, long? sessionId, CancellationToken ct);
    Task<object> RequestException(long sessionId, long actor, ReviewInput input, CancellationToken ct);
    Task<object> DecideException(long id, long actor, string role, ExceptionDecision input, CancellationToken ct);
    Task<object> Appeals(long actor, string role, CancellationToken ct);
    Task<object> RequestAppeal(long assessmentId, long actor, ReviewInput input, CancellationToken ct);
    Task<object> ReviewAppeal(long id, long actor, ReviewInput input, CancellationToken ct);
    Task<object> DecideAppeal(long id, long actor, AppealDecision input, CancellationToken ct);
}
