using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Application.DTOs.Persistence;

namespace TKSC.Application.Interfaces.Repositories;
public interface ISkillReviewRepository
{
    Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct);
    void AddAuditLogs(AuditLog value);
    Task<List<string?>> EventsAuditLogsListAsync(string type, long id, bool internalNotes, CancellationToken ct);
    Task<Player?> RequestExceptionPlayersAsync(long actor, CancellationToken ct);
    Task<Session?> RequestExceptionSessionsAsync(long sessionId, CancellationToken ct);
    Task<SessionException?> RequestExceptionSessionExceptionsAsync(long sessionId, CancellationToken ct, Player player);
    Task<PlayerSportProfile?> RequestExceptionPlayerSportProfilesAsync(CancellationToken ct, Player player, Session session);
    void AddSessionExceptions(SessionException value);
    Task<int> SaveAsync(CancellationToken ct);
    Task<List<SkillExceptionSummary>> ExceptionsSessionExceptionsListAsync(long actor, string role, long? sessionId, CancellationToken ct);
    Task<SessionException?> DecideExceptionSessionExceptionsAsync(long id, CancellationToken ct);
    Task<bool> DecideExceptionSessionRefereesExistsAsync(long actor, CancellationToken ct, SessionException row);
    Task<Assessment?> RequestAppealAssessmentsAsync(long assessmentId, long actor, CancellationToken ct);
    Task<RatingAppeal?> RequestAppealRatingAppealsAsync(long assessmentId, CancellationToken ct);
    void AddRatingAppeals(RatingAppeal value);
    Task<PlayerRatingHistory?> RequestAppealPlayerRatingHistoriesAsync(CancellationToken ct, Assessment a);
    Task<List<SkillAppealSummary>> AppealsListAsync(long actor, string role, CancellationToken ct);
    Task<List<SkillAppealHistoryEntry>> AppealsPlayerRatingHistoriesListAsync(CancellationToken ct, SkillAppealSummary row);
    Task<string?> AppealsAuditLogsAsync(CancellationToken ct, SkillAppealSummary row);
    Task<RatingAppeal?> ReviewAppealAsync(long id, long actor, CancellationToken ct);
    Task<RatingAppeal?> DecideAppealRatingAppealsAsync(long id, CancellationToken ct);
    Task<bool> DecideAppealAuditLogsExistsAsync(long id, CancellationToken ct);
    Task<PlayerRatingHistory?> DecideAppealPlayerRatingHistoriesAsync(CancellationToken ct, PlayerSportProfile profile);
    void AddPlayerRatingHistories(PlayerRatingHistory value);
}
