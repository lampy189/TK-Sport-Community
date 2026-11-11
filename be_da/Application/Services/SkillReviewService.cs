using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TKSC.Application.Assessments;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;

namespace TKSC.Application.Services;

public sealed record ReviewInput(string Reason);
public sealed record ExceptionDecision(string Reason, bool Approved);
public sealed record AppealDecision(string Reason, bool Correct, short? Score, long ExpectedHistoryId);
public sealed record ReviewEvent(string Status, string Reason, DateTimeOffset At, long ActorId, short? Score = null, string? Band = null);

// The baseline has no request metadata columns. Append-only AuditLogs retain reasons,
// referee discussion and decision times without rewriting the original request/evidence.
public sealed class SkillReviewService(Sep490Context db, TimeProvider clock, SkillAssessmentService assessments)
    : ISkillReviewService
{
    private static AssessmentException Error(int status, string message) => new(status, message);
    private static string Reason(string? value) => SkillReviewRules.Reason(value);
    private void Record(string type, long id, long actor, string action, string status, string reason) =>
        db.AuditLogs.Add(new AuditLog { EntityType = type, EntityId = id, ActorId = actor, Action = action,
            NewValue = JsonSerializer.Serialize(new ReviewEvent(status, reason, clock.GetUtcNow(), actor)) });
    private async Task<List<ReviewEvent>> Events(string type, long id, bool internalNotes, CancellationToken ct)
    {
        var logs = await db.AuditLogs.Where(l => l.EntityType == type && l.EntityId == id
            && (internalNotes || l.Action != "referee_review")).OrderBy(l => l.Id).Select(l => l.NewValue).ToListAsync(ct);
        return logs.Where(x => x != null).Select(x => JsonSerializer.Deserialize<ReviewEvent>(x!)!).ToList();
    }
    private async Task<T> Write<T>(Func<Task<T>> action, CancellationToken ct)
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
        catch (SqlException e) when (e.Number is 1205 or 51000)
        { throw Error(409, "Dữ liệu đang được cập nhật. Vui lòng tải lại và thử lại."); }
    }
    private void Future(Session session) => SkillReviewRules.Future(session, clock.GetUtcNow());
    public Task<object> RequestException(long sessionId, long actor, ReviewInput input, CancellationToken ct) => Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var player = await db.Players.SingleOrDefaultAsync(p => p.UserId == actor && p.User.Status == "active" && p.User.DeletedAt == null, ct)
            ?? throw Error(403, "Cần tài khoản Player đang hoạt động.");
        var session = await db.Sessions.Include(s => s.TimeSlot).SingleOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw Error(404, "Không tìm thấy buổi chơi.");
        Future(session);
        var existing = await db.SessionExceptions.Where(e => e.PlayerId == player.Id && e.SessionId == sessionId).OrderByDescending(e => e.Id).FirstOrDefaultAsync(ct);
        if (existing != null) return new { existing.Id };
        var profile = await db.PlayerSportProfiles.SingleOrDefaultAsync(p => p.PlayerId == player.Id && p.SportId == session.SportId, ct);
        if (profile?.CurrentScore == null || string.IsNullOrWhiteSpace(profile.CurrentBand)) throw Error(409, "Hoàn tất hồ sơ kỹ năng của môn này trước khi xin ngoại lệ.");
        if (string.IsNullOrWhiteSpace(session.Band) || string.Equals(profile.CurrentBand, session.Band, StringComparison.OrdinalIgnoreCase))
            throw Error(409, "Bạn đã phù hợp Band của buổi này, hãy đăng ký bình thường.");
        if (reason.Length > 255) throw Error(400, "Lý do xin ngoại lệ tối đa 255 ký tự.");
        var row = new SessionException { SessionId = sessionId, PlayerId = player.Id, Reason = reason };
        db.SessionExceptions.Add(row);
        await db.SaveChangesAsync(ct);
        Record("SessionException", row.Id, actor, "exception_requested", "Pending", reason);
        return new { row.Id };
    }, ct);
    public async Task<object> Exceptions(long actor, string role, long? sessionId, CancellationToken ct)
    {
        var rows = await db.SessionExceptions.AsNoTracking().Where(e =>
            (sessionId == null || e.SessionId == sessionId) && (role == "manager"
                || (role == "player" && e.Player.UserId == actor)
                || (role == "referee" && e.Session.SessionReferees.Any(r => r.RefereeId == actor))))
            .OrderByDescending(e => e.Id).Select(e => new { e.Id, e.SessionId, e.PlayerId, Player = e.Player.User.FullName,
                e.Reason, e.ApprovedBy, e.Session.SessionDate, Sport = e.Session.Sport.Name, SessionBand = e.Session.Band,
                CurrentBand = e.Player.PlayerSportProfiles.Where(p => p.SportId == e.Session.SportId).Select(p => p.CurrentBand).FirstOrDefault() }).ToListAsync(ct);
        var result = new List<object>();
        foreach (var row in rows)
        {
            var events = await Events("SessionException", row.Id, false, ct);
            result.Add(new { row.Id, row.SessionId, row.PlayerId, row.Player, row.Reason, row.SessionDate, row.Sport,
                row.SessionBand, row.CurrentBand, Status = row.ApprovedBy != null ? "Approved" : events.LastOrDefault()?.Status ?? "Pending", Events = events });
        }
        return result;
    }
    public Task<object> DecideException(long id, long actor, string role, ExceptionDecision input, CancellationToken ct) => Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await db.SessionExceptions.Include(e => e.Player).Include(e => e.Session).ThenInclude(s => s.TimeSlot)
            .SingleOrDefaultAsync(e => e.Id == id, ct) ?? throw Error(404, "Không tìm thấy yêu cầu.");
        if (row.Player.UserId == actor || (role != "manager" && !await db.SessionReferees.AnyAsync(r => r.SessionId == row.SessionId && r.RefereeId == actor, ct)))
            throw Error(403, "Bạn không có quyền duyệt yêu cầu này.");
        Future(row.Session);
        if (row.ApprovedBy != null || (await Events("SessionException", id, false, ct)).Any(e => e.Status is "Approved" or "Rejected"))
            throw Error(409, "Yêu cầu đã có quyết định. Tải lại để xem kết quả.");
        if (input.Approved) row.ApprovedBy = actor;
        Record("SessionException", id, actor, "exception_decided", input.Approved ? "Approved" : "Rejected", reason);
        return new { id };
    }, ct);
    public Task<object> RequestAppeal(long assessmentId, long actor, ReviewInput input, CancellationToken ct) => Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var a = await db.Assessments.SingleOrDefaultAsync(a => a.Id == assessmentId && a.PlayerSportProfile.Player.UserId == actor
            && a.Status == "published" && a.SessionId != null, ct) ?? throw Error(404, "Không tìm thấy kết quả đã công bố của bạn.");
        var existing = await db.RatingAppeals.FirstOrDefaultAsync(r => r.AssessmentId == assessmentId && (r.Status == "Pending" || r.Status == "UnderReview"), ct);
        if (existing != null) return new { existing.Id };
        var row = new RatingAppeal { AssessmentId = a.Id, PlayerSportProfileId = a.PlayerSportProfileId, SubmittedBy = actor, Status = "Pending" };
        db.RatingAppeals.Add(row);
        await db.SaveChangesAsync(ct);
        var published = await db.PlayerRatingHistories.Where(h => h.AssessmentId == a.Id).OrderByDescending(h => h.Id).FirstOrDefaultAsync(ct)
            ?? throw Error(409, "Kết quả chưa có lịch sử công bố.");
        db.AuditLogs.Add(new AuditLog { EntityType = "RatingAppeal", EntityId = row.Id, ActorId = actor, Action = "appeal_requested",
            NewValue = JsonSerializer.Serialize(new ReviewEvent("Pending", reason, clock.GetUtcNow(), actor, published.NewScore, published.NewBand)) });
        return new { row.Id };
    }, ct);
    private IQueryable<RatingAppeal> VisibleAppeals(long actor, string role) => db.RatingAppeals.Where(r => role == "manager"
        || (role == "player" && r.SubmittedBy == actor)
        || (role == "referee" && r.Assessment != null && r.Assessment.Session!.SessionReferees.Any(s => s.RefereeId == actor)));
    public async Task<object> Appeals(long actor, string role, CancellationToken ct)
    {
        var rows = await VisibleAppeals(actor, role).AsNoTracking().OrderByDescending(r => r.Id).Select(r => new {
            r.Id, r.AssessmentId, r.Status, Player = r.PlayerSportProfile.Player.User.FullName, Sport = r.PlayerSportProfile.Sport.Name,
            r.PlayerSportProfile.CurrentScore, r.PlayerSportProfile.CurrentBand,
            DisputedScore = r.Assessment!.TotalScore, DisputedBand = r.Assessment.BandResult,
            ExpectedHistoryId = r.PlayerSportProfile.PlayerRatingHistories.OrderByDescending(h => h.Id).Select(h => h.Id).FirstOrDefault()
        }).ToListAsync(ct);
        var result = new List<object>();
        foreach (var row in rows)
        {
            var events = await Events("RatingAppeal", row.Id, role != "player", ct);
            var history = await db.PlayerRatingHistories.Where(h => h.AssessmentId == row.AssessmentId)
                .OrderBy(h => h.Id).Select(h => new { h.Id, h.OldScore, h.NewScore, h.OldBand, h.NewBand }).ToListAsync(ct);
            var evidence = role == "player" ? null : await db.AuditLogs.Where(l => l.EntityType == "Assessment"
                && l.EntityId == row.AssessmentId && l.Action == "assessment_saved").OrderByDescending(l => l.Id)
                .Select(l => l.NewValue).FirstOrDefaultAsync(ct);
            result.Add(new { row.Id, row.AssessmentId, row.Status, row.Player, row.Sport, row.CurrentScore, row.CurrentBand,
                DisputedScore = events.FirstOrDefault()?.Score ?? row.DisputedScore,
                DisputedBand = events.FirstOrDefault()?.Band ?? row.DisputedBand,
                row.ExpectedHistoryId, Events = events, History = history, Evidence = evidence == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(evidence) });
        }
        return result;
    }
    public Task<object> ReviewAppeal(long id, long actor, ReviewInput input, CancellationToken ct) => Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await VisibleAppeals(actor, "referee").Include(r => r.PlayerSportProfile).ThenInclude(p => p.Player)
            .SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw Error(404, "Không tìm thấy khiếu nại trong buổi được phân công.");
        if (row.SubmittedBy == actor || row.PlayerSportProfile.Player.UserId == actor) throw Error(403, "Không được tự review khiếu nại.");
        if (row.Status is not ("Pending" or "UnderReview")) throw Error(409, "Khiếu nại đã đóng.");
        row.Status = "UnderReview";
        Record("RatingAppeal", id, actor, "referee_review", "UnderReview", reason);
        return new { id };
    }, ct);
    public Task<object> DecideAppeal(long id, long actor, AppealDecision input, CancellationToken ct) => Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await db.RatingAppeals.Include(r => r.PlayerSportProfile).ThenInclude(p => p.Player)
            .Include(r => r.Assessment).ThenInclude(a => a!.Rubric).ThenInclude(r => r.Sport)
            .Include(r => r.Assessment).ThenInclude(a => a!.Rubric).ThenInclude(r => r.RubricDimensions)
            .SingleOrDefaultAsync(r => r.Id == id, ct) ?? throw Error(404, "Không tìm thấy khiếu nại.");
        if (row.SubmittedBy == actor || row.PlayerSportProfile.Player.UserId == actor || row.Assessment?.AssessorId == actor)
            throw Error(403, "Không được tự phê duyệt kết quả của mình.");
        if (row.Status != "UnderReview" || !await db.AuditLogs.AnyAsync(l => l.EntityType == "RatingAppeal" && l.EntityId == id && l.Action == "referee_review", ct))
            throw Error(409, "Cần Trọng tài review trước khi quyết định; khiếu nại đã đóng không thể duyệt lại.");
        if (input.Correct)
        {
            var profile = row.PlayerSportProfile;
            var latest = await db.PlayerRatingHistories.Where(h => h.PlayerSportProfileId == profile.Id).OrderByDescending(h => h.Id).FirstOrDefaultAsync(ct);
            if (latest == null || latest.Id != input.ExpectedHistoryId || latest.AssessmentId != row.AssessmentId)
                throw Error(409, "Đã có kết quả mới hơn. Tải lại và xem xét kết quả hiện hành.");
            if (row.Assessment == null || !SportAssessmentScoring.Supports(row.Assessment.Rubric))
                throw Error(409, "Môn/rubric này chưa có quy tắc điều chỉnh điểm được cấu hình.");
            var correctedScore = SkillReviewRules.CorrectedScore(input.Score);
            var band = SportAssessmentScoring.Band(correctedScore);
            db.PlayerRatingHistories.Add(new PlayerRatingHistory { PlayerSportProfileId = profile.Id, AssessmentId = row.AssessmentId,
                ChangedBy = actor, OldScore = profile.CurrentScore, NewScore = input.Score, OldBand = profile.CurrentBand, NewBand = band });
            profile.CurrentScore = input.Score;
            profile.CurrentBand = band;
            profile.RatingStatus = "Rated";
            await assessments.CheckFutureTicketsAsync(profile, actor, ct);
        }
        row.Status = input.Correct ? "Corrected" : "Kept";
        row.ReviewedBy = actor;
        Record("RatingAppeal", id, actor, "appeal_decided", row.Status, reason);
        return new { id, row.Status };
    }, ct);
}
