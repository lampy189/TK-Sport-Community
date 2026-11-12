using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Services;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class SkillReviewRepository(Sep490Context db, TimeProvider clock) : ISkillReviewRepository
{
    private DateTimeOffset Now => clock.GetUtcNow();

    private static TKSC.Domain.Exceptions.AssessmentException Error(int status, string message) => new(status, message);
    public async Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource=N'TKSC:skill-assessments', @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Skill review is busy.', 1;
                """, ct);
            var result = await action();
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return result;
        }
        catch (SqlException e)when (e.Number is 1205 or 51000)
        {
            throw Error(409, "Dữ liệu đang được cập nhật. Vui lòng tải lại và thử lại.");
        }
    }

    private IQueryable<RatingAppeal> VisibleAppeals(long actor, string role) => db.RatingAppeals.Where(r => role == "manager" || (role == "player" && r.SubmittedBy == actor) || (role == "referee" && r.Assessment != null && r.Assessment.Session!.SessionReferees.Any(s => s.RefereeId == actor)));
    public void AddAuditLogs(AuditLog value) => db.AuditLogs.Add(value);
    public Task<List<string?>> EventsAuditLogsListAsync(string type, long id, bool internalNotes, CancellationToken ct) => db.AuditLogs.Where(l => l.EntityType == type && l.EntityId == id && (internalNotes || l.Action != "referee_review")).OrderBy(l => l.Id).Select(l => l.NewValue).ToListAsync(ct);
    public Task<Player?> RequestExceptionPlayersAsync(long actor, CancellationToken ct) => db.Players.SingleOrDefaultAsync(p => p.UserId == actor && p.User.Status == "active" && p.User.DeletedAt == null, ct);
    public Task<Session?> RequestExceptionSessionsAsync(long sessionId, CancellationToken ct) => db.Sessions.Include(s => s.TimeSlot).SingleOrDefaultAsync(s => s.Id == sessionId, ct);
    public Task<SessionException?> RequestExceptionSessionExceptionsAsync(long sessionId, CancellationToken ct, Player player) => db.SessionExceptions.Where(e => e.PlayerId == player.Id && e.SessionId == sessionId).OrderByDescending(e => e.Id).FirstOrDefaultAsync(ct);
    public Task<PlayerSportProfile?> RequestExceptionPlayerSportProfilesAsync(CancellationToken ct, Player player, Session session) => db.PlayerSportProfiles.SingleOrDefaultAsync(p => p.PlayerId == player.Id && p.SportId == session.SportId, ct);
    public void AddSessionExceptions(SessionException value) => db.SessionExceptions.Add(value);
    public Task<int> SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<List<SkillExceptionSummary>> ExceptionsSessionExceptionsListAsync(long actor, string role, long? sessionId, CancellationToken ct) => db.SessionExceptions.AsNoTracking().Where(e => (sessionId == null || e.SessionId == sessionId) && (role == "manager" || (role == "player" && e.Player.UserId == actor) || (role == "referee" && e.Session.SessionReferees.Any(r => r.RefereeId == actor)))).OrderByDescending(e => e.Id).Select(e => new SkillExceptionSummary { Id = e.Id, SessionId = e.SessionId, PlayerId = e.PlayerId, Player = e.Player.User.FullName, Reason = e.Reason, ApprovedBy = e.ApprovedBy, SessionDate = e.Session.SessionDate, Sport = e.Session.Sport.Name, SessionBand = e.Session.Band, CurrentBand = e.Player.PlayerSportProfiles.Where(p => p.SportId == e.Session.SportId).Select(p => p.CurrentBand).FirstOrDefault() }).ToListAsync(ct);
    public Task<SessionException?> DecideExceptionSessionExceptionsAsync(long id, CancellationToken ct) => db.SessionExceptions.Include(e => e.Player).Include(e => e.Session).ThenInclude(s => s.TimeSlot).SingleOrDefaultAsync(e => e.Id == id, ct);
    public Task<bool> DecideExceptionSessionRefereesExistsAsync(long actor, CancellationToken ct, SessionException row) => db.SessionReferees.AnyAsync(r => r.SessionId == row.SessionId && r.RefereeId == actor, ct);
    public Task<Assessment?> RequestAppealAssessmentsAsync(long assessmentId, long actor, CancellationToken ct) => db.Assessments.SingleOrDefaultAsync(a => a.Id == assessmentId && a.PlayerSportProfile.Player.UserId == actor && a.Status == "published" && a.SessionId != null, ct);
    public Task<RatingAppeal?> RequestAppealRatingAppealsAsync(long assessmentId, CancellationToken ct) => db.RatingAppeals.FirstOrDefaultAsync(r => r.AssessmentId == assessmentId && (r.Status == "Pending" || r.Status == "UnderReview"), ct);
    public void AddRatingAppeals(RatingAppeal value) => db.RatingAppeals.Add(value);
    public Task<PlayerRatingHistory?> RequestAppealPlayerRatingHistoriesAsync(CancellationToken ct, Assessment a) => db.PlayerRatingHistories.Where(h => h.AssessmentId == a.Id).OrderByDescending(h => h.Id).FirstOrDefaultAsync(ct);
    public Task<List<SkillAppealSummary>> AppealsListAsync(long actor, string role, CancellationToken ct) => VisibleAppeals(actor, role).AsNoTracking().OrderByDescending(r => r.Id).Select(r => new SkillAppealSummary { Id = r.Id, AssessmentId = r.AssessmentId, Status = r.Status, Player = r.PlayerSportProfile.Player.User.FullName, Sport = r.PlayerSportProfile.Sport.Name, CurrentScore = r.PlayerSportProfile.CurrentScore, CurrentBand = r.PlayerSportProfile.CurrentBand, DisputedScore = r.Assessment!.TotalScore, DisputedBand = r.Assessment.BandResult, ExpectedHistoryId = r.PlayerSportProfile.PlayerRatingHistories.OrderByDescending(h => h.Id).Select(h => h.Id).FirstOrDefault() }).ToListAsync(ct);
    public Task<List<SkillAppealHistoryEntry>> AppealsPlayerRatingHistoriesListAsync(CancellationToken ct, SkillAppealSummary row) => db.PlayerRatingHistories.Where(h => h.AssessmentId == row.AssessmentId).OrderBy(h => h.Id).Select(h => new SkillAppealHistoryEntry { Id = h.Id, OldScore = h.OldScore, NewScore = h.NewScore, OldBand = h.OldBand, NewBand = h.NewBand }).ToListAsync(ct);
    public Task<string?> AppealsAuditLogsAsync(CancellationToken ct, SkillAppealSummary row) => db.AuditLogs.Where(l => l.EntityType == "Assessment" && l.EntityId == row.AssessmentId && l.Action == "assessment_saved").OrderByDescending(l => l.Id).Select(l => l.NewValue).FirstOrDefaultAsync(ct);
    public Task<RatingAppeal?> ReviewAppealAsync(long id, long actor, CancellationToken ct) => VisibleAppeals(actor, "referee").Include(r => r.PlayerSportProfile).ThenInclude(p => p.Player).SingleOrDefaultAsync(r => r.Id == id, ct);
    public Task<RatingAppeal?> DecideAppealRatingAppealsAsync(long id, CancellationToken ct) => db.RatingAppeals.Include(r => r.PlayerSportProfile).ThenInclude(p => p.Player).Include(r => r.Assessment).ThenInclude(a => a!.Rubric).ThenInclude(r => r.Sport).Include(r => r.Assessment).ThenInclude(a => a!.Rubric).ThenInclude(r => r.RubricDimensions).SingleOrDefaultAsync(r => r.Id == id, ct);
    public Task<bool> DecideAppealAuditLogsExistsAsync(long id, CancellationToken ct) => db.AuditLogs.AnyAsync(l => l.EntityType == "RatingAppeal" && l.EntityId == id && l.Action == "referee_review", ct);
    public Task<PlayerRatingHistory?> DecideAppealPlayerRatingHistoriesAsync(CancellationToken ct, PlayerSportProfile profile) => db.PlayerRatingHistories.Where(h => h.PlayerSportProfileId == profile.Id).OrderByDescending(h => h.Id).FirstOrDefaultAsync(ct);
    public void AddPlayerRatingHistories(PlayerRatingHistory value) => db.PlayerRatingHistories.Add(value);
}
