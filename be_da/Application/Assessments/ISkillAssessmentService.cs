namespace TKSC.Application.Assessments;

public interface ISkillAssessmentService
{
    Task<object> SessionsAsync(long actorId, CancellationToken ct);
    Task<RubricView> GetRubricAsync(long sportId, CancellationToken ct);
    Task<IReadOnlyList<AttendeeView>> GetAttendeesAsync(long sessionId, long actorId, CancellationToken ct);
    Task<AssessmentView> OpenAsync(long sessionId, long playerId, long actorId, CancellationToken ct);
    Task<AssessmentView> ReadAsync(long id, long actorId, CancellationToken ct);
    Task<AssessmentView> SaveAsync(long id, long actorId, SaveAssessmentRequest request, CancellationToken ct);
    Task<AssessmentView> PublishAsync(long id, long actorId, PublishAssessmentRequest request, CancellationToken ct);
}
