using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Application.DTOs.Persistence;

namespace TKSC.Application.Interfaces.Repositories;
public interface ISkillAssessmentRepository
{
    Task<T> MutateAsync<T>(Func<Task<T>> action, CancellationToken ct);
    Task<List<AssignedAssessmentSession>> SessionsSessionsListAsync(long actorId, CancellationToken ct, DateOnly today);
    Task<long> PlayerContextCourtsAsync(CancellationToken ct, Session session);
    Task<AssessmentPlayerProfile?> PlayerContextPlayerSportProfilesAsync(long playerId, CancellationToken ct, Session session);
    Task<List<AssessmentParticipation>> PlayerContextTicketsListAsync(long sessionId, long playerId, CancellationToken ct, Session session, long venueId);
    Task<List<AssessmentRatingEntry>> PlayerContextPlayerRatingHistoriesListAsync(long playerId, CancellationToken ct, Session session, long venueId);
    Task<List<Rubric>> ActiveRubricListAsync(long sportId, CancellationToken ct);
    Task<bool> AssignedSessionSessionRefereesExistsAsync(long sessionId, long actorId, CancellationToken ct);
    Task<Session?> AssignedSessionSessionsAsync(long sessionId, CancellationToken ct);
    Task<List<AssessmentAttendeeIdentity>> GetAttendeesTicketsListAsync(long sessionId, long actorId, CancellationToken ct);
    Task<List<PlayerSportProfile>> GetAttendeesPlayerSportProfilesListAsync(CancellationToken ct, Session session, long[] ids);
    Task<List<Assessment>> GetAttendeesAssessmentsListAsync(long sessionId, long actorId, CancellationToken ct);
    Task<PlayerSportProfile?> OpenPlayerSportProfilesAsync(long playerId, CancellationToken ct, Session session);
    Task<Assessment?> OpenAsync(long sessionId, long actorId, CancellationToken ct, PlayerSportProfile profile);
    void AddAssessments(Assessment value);
    Task<int> SaveAsync(CancellationToken ct);
    Task<bool> CheckAttendanceTicketsExistsAsync(long sessionId, PlayerSportProfile profile, CancellationToken ct);
    Task<Assessment?> LoadAsync(long id, CancellationToken ct);
    Task<Session> PublishSessionsAsync(CancellationToken ct, Assessment a);
    Task<bool> PublishAssessmentsExistsAsync(CancellationToken ct, PlayerSportProfile profile, DateOnly observedDate, TimeOnly observedTime);
    Task<Assessment?> PublishAssessmentsAsync(CancellationToken ct, Assessment a, PlayerSportProfile profile, DateOnly observedDate, TimeOnly observedTime);
    Task<bool> PublishPlayerRatingHistoriesExistsAsync(CancellationToken ct, PlayerSportProfile profile);
    void AddPlayerRatingHistories(PlayerRatingHistory value);
    Task<List<Ticket>> ListFuturePaidTicketsAsync(PlayerSportProfile profile, CancellationToken ct, DateOnly today, TimeOnly time);
    Task<bool> CheckFutureTicketsSessionExceptionsExistsAsync(PlayerSportProfile profile, CancellationToken ct, Ticket ticket);
    Task<bool> CheckFutureTicketsRefundsExistsAsync(CancellationToken ct, Ticket ticket);
    void AddRefunds(Refund value);
    void AddAuditLogs(AuditLog value);
    Task<long?> RevisionAuditLogsMaxAsync(long id, CancellationToken ct);
    Task<string?> EvidenceAuditLogsAsync(long id, CancellationToken ct);
}
