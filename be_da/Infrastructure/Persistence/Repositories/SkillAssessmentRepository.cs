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
public sealed class SkillAssessmentRepository(Sep490Context db, TimeProvider clock) : ISkillAssessmentRepository
{
    private DateTimeOffset Now => clock.GetUtcNow();

    private static TKSC.Domain.Exceptions.AssessmentException Error(int status, string message) => new(status, message);
    private IQueryable<Rubric> Rubrics => db.Rubrics.Include(r => r.Sport).Include(r => r.RubricDimensions);
    private IQueryable<Assessment> Assessments => db.Assessments.Include(a => a.Rubric).ThenInclude(r => r.Sport).Include(a => a.Rubric).ThenInclude(r => r.RubricDimensions).Include(a => a.AssessmentScores).Include(a => a.PlayerSportProfile).ThenInclude(p => p.Player);

    public async Task<T> MutateAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // Serialize this workflow across API instances, including open/retry and publication.
            await db.Database.ExecuteSqlRawAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=N'TKSC:skill-assessments', @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            IF @result < 0 THROW 51000, 'Assessment is busy. Retry.', 1;
            """, ct);
            var result = await action();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (SqlException e)when (e.Number is 1205 or 51000)
        {
            throw Error(409, "Dữ liệu đang được cập nhật. Tải lại phiếu và thử lại.");
        }
    }

    public Task<List<AssignedAssessmentSession>> SessionsSessionsListAsync(long actorId, CancellationToken ct, DateOnly today) => db.Sessions.AsNoTracking().Where(s => s.SessionReferees.Any(r => r.RefereeId == actorId) && s.Status != "cancelled" && s.SessionDate >= today).OrderBy(s => s.SessionDate).ThenBy(s => s.TimeSlot.StartTime).Take(100).Select(s => new AssignedAssessmentSession { Id = s.Id, SessionDate = s.SessionDate, Name = s.Sport.Name, Court = s.Court.Name, StartTime = s.TimeSlot.StartTime }).ToListAsync(ct);
    public Task<long> PlayerContextCourtsAsync(CancellationToken ct, Session session) => db.Courts.Where(c => c.Id == session.CourtId).Select(c => c.VenueId).SingleAsync(ct);
    public Task<AssessmentPlayerProfile?> PlayerContextPlayerSportProfilesAsync(long playerId, CancellationToken ct, Session session) => db.PlayerSportProfiles.AsNoTracking().Where(p => p.PlayerId == playerId && p.SportId == session.SportId).Select(p => new AssessmentPlayerProfile { CurrentScore = p.CurrentScore, CurrentBand = p.CurrentBand, RatingStatus = p.RatingStatus, Sport = p.Sport.Name }).SingleOrDefaultAsync(ct);
    public Task<List<AssessmentParticipation>> PlayerContextTicketsListAsync(long sessionId, long playerId, CancellationToken ct, Session session, long venueId) => db.Tickets.AsNoTracking().Where(t => t.PlayerId == playerId && t.Session.SportId == session.SportId && t.Session.Court.VenueId == venueId && t.Status == "checkedin" && t.CheckedInBy != null && t.Order.Status == "paid" && t.SessionId != sessionId && t.Session.SessionDate <= session.SessionDate).Select(t => new AssessmentParticipation { SessionId = t.SessionId, SessionDate = t.Session.SessionDate, StartTime = t.Session.TimeSlot.StartTime, Court = t.Session.Court.Name }).Distinct().OrderByDescending(t => t.SessionDate).ThenByDescending(t => t.StartTime).Take(20).ToListAsync(ct);
    public Task<List<AssessmentRatingEntry>> PlayerContextPlayerRatingHistoriesListAsync(long playerId, CancellationToken ct, Session session, long venueId) => db.PlayerRatingHistories.AsNoTracking().Where(RatingPublicationRules.OfficialHistory).Where(h => h.PlayerSportProfile.PlayerId == playerId && h.PlayerSportProfile.SportId == session.SportId && h.Assessment != null && h.Assessment.Session != null && h.Assessment.Session.Court.VenueId == venueId).OrderByDescending(h => h.Id).Take(20).Select(h => new AssessmentRatingEntry { Id = h.Id, NewScore = h.NewScore, NewBand = h.NewBand, OldScore = h.OldScore, OldBand = h.OldBand, SessionDate = h.Assessment!.Session!.SessionDate, Rubric = h.Assessment.Rubric.Name }).ToListAsync(ct);
    public Task<List<Rubric>> ActiveRubricListAsync(long sportId, CancellationToken ct) => Rubrics.Where(r => r.SportId == sportId && r.IsActive == true).ToListAsync(ct);
    public Task<bool> AssignedSessionSessionRefereesExistsAsync(long sessionId, long actorId, CancellationToken ct) => db.SessionReferees.AnyAsync(r => r.SessionId == sessionId && r.RefereeId == actorId, ct);
    public Task<Session?> AssignedSessionSessionsAsync(long sessionId, CancellationToken ct) => db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId, ct);
    public Task<List<AssessmentAttendeeIdentity>> GetAttendeesTicketsListAsync(long sessionId, long actorId, CancellationToken ct) => db.Tickets.Where(t => t.SessionId == sessionId && t.Status == "checkedin" && t.CheckedInBy != null && t.Order.Status == "paid" && t.Player.UserId != actorId).Select(t => new AssessmentAttendeeIdentity { PlayerId = t.PlayerId, FullName = t.Player.User.FullName }).Distinct().OrderBy(t => t.FullName).ToListAsync(ct);
    public Task<List<PlayerSportProfile>> GetAttendeesPlayerSportProfilesListAsync(CancellationToken ct, Session session, long[] ids) => db.PlayerSportProfiles.Where(p => ids.Contains(p.PlayerId) && p.SportId == session.SportId).ToListAsync(ct);
    public Task<List<Assessment>> GetAttendeesAssessmentsListAsync(long sessionId, long actorId, CancellationToken ct) => db.Assessments.Where(a => a.SessionId == sessionId && a.AssessorId == actorId && a.AssessmentType == "on_court").ToListAsync(ct);
    public Task<PlayerSportProfile?> OpenPlayerSportProfilesAsync(long playerId, CancellationToken ct, Session session) => db.PlayerSportProfiles.Include(p => p.Player).SingleOrDefaultAsync(p => p.PlayerId == playerId && p.SportId == session.SportId, ct);
    public Task<Assessment?> OpenAsync(long sessionId, long actorId, CancellationToken ct, PlayerSportProfile profile) => Assessments.SingleOrDefaultAsync(a => a.SessionId == sessionId && a.PlayerSportProfileId == profile.Id && a.AssessorId == actorId && a.AssessmentType == "on_court", ct);
    public void AddAssessments(Assessment value) => db.Assessments.Add(value);
    public Task<int> SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task<bool> CheckAttendanceTicketsExistsAsync(long sessionId, PlayerSportProfile profile, CancellationToken ct) => db.Tickets.AnyAsync(t => t.SessionId == sessionId && t.PlayerId == profile.PlayerId && t.Status == "checkedin" && t.CheckedInBy != null && t.Order.Status == "paid", ct);
    public Task<Assessment?> LoadAsync(long id, CancellationToken ct) => Assessments.SingleOrDefaultAsync(a => a.Id == id, ct);
    public Task<Session> PublishSessionsAsync(CancellationToken ct, Assessment a) => db.Sessions.Include(s => s.TimeSlot).SingleAsync(s => s.Id == a.SessionId, ct);
    public Task<bool> PublishAssessmentsExistsAsync(CancellationToken ct, PlayerSportProfile profile, DateOnly observedDate, TimeOnly observedTime) => db.Assessments.AnyAsync(x => x.PlayerSportProfileId == profile.Id && x.Status == "published" && x.SessionId != null && (x.Session!.SessionDate > observedDate || (x.Session.SessionDate == observedDate && x.Session.TimeSlot.StartTime > observedTime)), ct);
    public Task<Assessment?> PublishAssessmentsAsync(CancellationToken ct, Assessment a, PlayerSportProfile profile, DateOnly observedDate, TimeOnly observedTime) => db.Assessments.Where(x => x.PlayerSportProfileId == profile.Id && x.Id != a.Id && x.SessionId != a.SessionId && x.RubricId == a.RubricId && x.SessionId != null && (x.Session!.SessionDate < observedDate || (x.Session.SessionDate == observedDate && x.Session.TimeSlot.StartTime < observedTime)) && x.AssessmentType == "on_court" && x.TotalScore != null).OrderByDescending(x => x.Session!.SessionDate).ThenByDescending(x => x.Session!.TimeSlot.StartTime).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
    public Task<bool> PublishPlayerRatingHistoriesExistsAsync(CancellationToken ct, PlayerSportProfile profile) => db.PlayerRatingHistories.Where(RatingPublicationRules.OfficialHistory).AnyAsync(h => h.PlayerSportProfileId == profile.Id, ct);
    public void AddPlayerRatingHistories(PlayerRatingHistory value) => db.PlayerRatingHistories.Add(value);
    public Task<List<Ticket>> ListFuturePaidTicketsAsync(PlayerSportProfile profile, CancellationToken ct, DateOnly today, TimeOnly time) => db.Tickets.Include(t => t.Session).ThenInclude(s => s.TimeSlot).Include(t => t.Order).ThenInclude(o => o.Payments).Where(t => t.PlayerId == profile.PlayerId && t.Session.SportId == profile.SportId && t.Status == "confirmed" && t.CheckedInBy == null && t.Order.Status == "paid" && (t.Session.SessionDate > today || (t.Session.SessionDate == today && t.Session.TimeSlot.StartTime > time))).ToListAsync(ct);
    public Task<bool> CheckFutureTicketsSessionExceptionsExistsAsync(PlayerSportProfile profile, CancellationToken ct, Ticket ticket) => db.SessionExceptions.AnyAsync(e => e.SessionId == ticket.SessionId && e.PlayerId == profile.PlayerId && e.ApprovedBy != null, ct);
    public Task<bool> CheckFutureTicketsRefundsExistsAsync(CancellationToken ct, Ticket ticket) => db.Refunds.AnyAsync(r => r.TicketId == ticket.Id, ct);
    public void AddRefunds(Refund value) => db.Refunds.Add(value);
    public void AddAuditLogs(AuditLog value) => db.AuditLogs.Add(value);
    public Task<long?> RevisionAuditLogsMaxAsync(long id, CancellationToken ct) => db.AuditLogs.Where(l => l.EntityType == "Assessment" && l.EntityId == id).Select(l => (long? )l.Id).MaxAsync(ct);
    public Task<string?> EvidenceAuditLogsAsync(long id, CancellationToken ct) => db.AuditLogs.Where(l => l.EntityType == "Assessment" && l.EntityId == id && l.Action == "assessment_saved").OrderByDescending(l => l.Id).Select(l => l.NewValue).FirstOrDefaultAsync(ct);
}
