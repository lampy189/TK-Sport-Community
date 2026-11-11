using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface ISkillAssessmentService
{
    Task<object> SessionsAsync(long actorId, CancellationToken ct);
    Task<object> PlayerContextAsync(long sessionId, long playerId, long actorId, CancellationToken ct);
    Task<RubricView> GetRubricAsync(long sportId, CancellationToken ct);
    Task<IReadOnlyList<AttendeeView>> GetAttendeesAsync(long sessionId, long actorId, CancellationToken ct);
    Task<AssessmentView> OpenAsync(long sessionId, long playerId, long actorId, CancellationToken ct);
    Task<AssessmentView> ReadAsync(long id, long actorId, CancellationToken ct);
    Task<AssessmentView> SaveAsync(long id, long actorId, SaveAssessmentRequest request, CancellationToken ct);
    Task<AssessmentView> PublishAsync(long id, long actorId, PublishAssessmentRequest request, CancellationToken ct);
    Task CheckFutureTicketsAsync(PlayerSportProfile profile, long actorId, CancellationToken ct);
}
