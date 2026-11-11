using TKSC.Application.DTOs;
using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using System.Text.Json;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
// The baseline has no request metadata columns. Append-only AuditLogs retain reasons,
// referee discussion and decision times without rewriting the original request/evidence.
public sealed class SkillReviewService(ISkillReviewRepository repository, TimeProvider clock, ISkillAssessmentService assessments) : ISkillReviewService
{
    private static AssessmentException Error(int status, string message) => new(status, message);
    private static string Reason(string? value) => SkillReviewRules.Reason(value);
    private void Record(string type, long id, long actor, string action, string status, string reason) => repository.AddAuditLogs(new AuditLog { EntityType = type, EntityId = id, ActorId = actor, Action = action, NewValue = JsonSerializer.Serialize(new ReviewEvent(status, reason, clock.GetUtcNow(), actor)) });
    private async Task<List<ReviewEvent>> Events(string type, long id, bool internalNotes, CancellationToken ct)
    {
        var logs = await repository.EventsAuditLogsListAsync(type, id, internalNotes, ct);
        return logs.Where(x => x != null).Select(x => JsonSerializer.Deserialize<ReviewEvent>(x!)!).ToList();
    }

    private void Future(Session session) => SkillReviewRules.Future(session, clock.GetUtcNow());
    public Task<object> RequestException(long sessionId, long actor, ReviewInput input, CancellationToken ct) => repository.Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var player = await repository.RequestExceptionPlayersAsync(actor, ct) ?? throw Error(403, "Cần tài khoản Player đang hoạt động.");
        var session = await repository.RequestExceptionSessionsAsync(sessionId, ct) ?? throw Error(404, "Không tìm thấy buổi chơi.");
        Future(session);
        var existing = await repository.RequestExceptionSessionExceptionsAsync(sessionId, ct, player);
        if (existing != null)
            return new
            {
                existing.Id
            };
        var profile = await repository.RequestExceptionPlayerSportProfilesAsync(ct, player, session);
        if (profile?.CurrentScore == null || string.IsNullOrWhiteSpace(profile.CurrentBand))
            throw Error(409, "Hoàn tất hồ sơ kỹ năng của môn này trước khi xin ngoại lệ.");
        if (string.IsNullOrWhiteSpace(session.Band) || string.Equals(profile.CurrentBand, session.Band, StringComparison.OrdinalIgnoreCase))
            throw Error(409, "Bạn đã phù hợp Band của buổi này, hãy đăng ký bình thường.");
        if (reason.Length > 255)
            throw Error(400, "Lý do xin ngoại lệ tối đa 255 ký tự.");
        var row = new SessionException
        {
            SessionId = sessionId,
            PlayerId = player.Id,
            Reason = reason
        };
        repository.AddSessionExceptions(row);
        await repository.SaveAsync(ct);
        Record("SessionException", row.Id, actor, "exception_requested", "Pending", reason);
        return new
        {
            row.Id
        };
    }, ct);
    public async Task<object> Exceptions(long actor, string role, long? sessionId, CancellationToken ct)
    {
        var rows = await repository.ExceptionsSessionExceptionsListAsync(actor, role, sessionId, ct);
        var result = new List<object>();
        foreach (var row in rows)
        {
            var events = await Events("SessionException", row.Id, false, ct);
            result.Add(new { row.Id, row.SessionId, row.PlayerId, row.Player, row.Reason, row.SessionDate, row.Sport, row.SessionBand, row.CurrentBand, Status = row.ApprovedBy != null ? "Approved" : events.LastOrDefault()?.Status ?? "Pending", Events = events });
        }

        return result;
    }

    public Task<object> DecideException(long id, long actor, string role, ExceptionDecision input, CancellationToken ct) => repository.Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await repository.DecideExceptionSessionExceptionsAsync(id, ct) ?? throw Error(404, "Không tìm thấy yêu cầu.");
        if (row.Player.UserId == actor || (role != "manager" && !await repository.DecideExceptionSessionRefereesExistsAsync(actor, ct, row)))
            throw Error(403, "Bạn không có quyền duyệt yêu cầu này.");
        Future(row.Session);
        if (row.ApprovedBy != null || (await Events("SessionException", id, false, ct)).Any(e => e.Status is "Approved" or "Rejected"))
            throw Error(409, "Yêu cầu đã có quyết định. Tải lại để xem kết quả.");
        if (input.Approved)
            row.ApprovedBy = actor;
        Record("SessionException", id, actor, "exception_decided", input.Approved ? "Approved" : "Rejected", reason);
        return new
        {
            id
        };
    }, ct);
    public Task<object> RequestAppeal(long assessmentId, long actor, ReviewInput input, CancellationToken ct) => repository.Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var a = await repository.RequestAppealAssessmentsAsync(assessmentId, actor, ct) ?? throw Error(404, "Không tìm thấy kết quả đã công bố của bạn.");
        var existing = await repository.RequestAppealRatingAppealsAsync(assessmentId, ct);
        if (existing != null)
            return new
            {
                existing.Id
            };
        var row = new RatingAppeal
        {
            AssessmentId = a.Id,
            PlayerSportProfileId = a.PlayerSportProfileId,
            SubmittedBy = actor,
            Status = "Pending"
        };
        repository.AddRatingAppeals(row);
        await repository.SaveAsync(ct);
        var published = await repository.RequestAppealPlayerRatingHistoriesAsync(ct, a) ?? throw Error(409, "Kết quả chưa có lịch sử công bố.");
        repository.AddAuditLogs(new AuditLog { EntityType = "RatingAppeal", EntityId = row.Id, ActorId = actor, Action = "appeal_requested", NewValue = JsonSerializer.Serialize(new ReviewEvent("Pending", reason, clock.GetUtcNow(), actor, published.NewScore, published.NewBand)) });
        return new
        {
            row.Id
        };
    }, ct);
    public async Task<object> Appeals(long actor, string role, CancellationToken ct)
    {
        var rows = await repository.AppealsListAsync(actor, role, ct);
        var result = new List<object>();
        foreach (var row in rows)
        {
            var events = await Events("RatingAppeal", row.Id, role != "player", ct);
            var history = await repository.AppealsPlayerRatingHistoriesListAsync(ct, row);
            var evidence = role == "player" ? null : await repository.AppealsAuditLogsAsync(ct, row);
            result.Add(new { row.Id, row.AssessmentId, row.Status, row.Player, row.Sport, row.CurrentScore, row.CurrentBand, DisputedScore = events.FirstOrDefault()?.Score ?? row.DisputedScore, DisputedBand = events.FirstOrDefault()?.Band ?? row.DisputedBand, row.ExpectedHistoryId, Events = events, History = history, Evidence = evidence == null ? (JsonElement? )null : JsonSerializer.Deserialize<JsonElement>(evidence) });
        }

        return result;
    }

    public Task<object> ReviewAppeal(long id, long actor, ReviewInput input, CancellationToken ct) => repository.Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await repository.ReviewAppealAsync(id, actor, ct) ?? throw Error(404, "Không tìm thấy khiếu nại trong buổi được phân công.");
        if (row.SubmittedBy == actor || row.PlayerSportProfile.Player.UserId == actor)
            throw Error(403, "Không được tự review khiếu nại.");
        if (row.Status is not ("Pending" or "UnderReview"))
            throw Error(409, "Khiếu nại đã đóng.");
        row.Status = "UnderReview";
        Record("RatingAppeal", id, actor, "referee_review", "UnderReview", reason);
        return new
        {
            id
        };
    }, ct);
    public Task<object> DecideAppeal(long id, long actor, AppealDecision input, CancellationToken ct) => repository.Write<object>(async () =>
    {
        var reason = Reason(input.Reason);
        var row = await repository.DecideAppealRatingAppealsAsync(id, ct) ?? throw Error(404, "Không tìm thấy khiếu nại.");
        if (row.SubmittedBy == actor || row.PlayerSportProfile.Player.UserId == actor || row.Assessment?.AssessorId == actor)
            throw Error(403, "Không được tự phê duyệt kết quả của mình.");
        if (row.Status != "UnderReview" || !await repository.DecideAppealAuditLogsExistsAsync(id, ct))
            throw Error(409, "Cần Trọng tài review trước khi quyết định; khiếu nại đã đóng không thể duyệt lại.");
        if (input.Correct)
        {
            var profile = row.PlayerSportProfile;
            var latest = await repository.DecideAppealPlayerRatingHistoriesAsync(ct, profile);
            if (latest == null || latest.Id != input.ExpectedHistoryId || latest.AssessmentId != row.AssessmentId)
                throw Error(409, "Đã có kết quả mới hơn. Tải lại và xem xét kết quả hiện hành.");
            if (row.Assessment == null || !SportAssessmentScoring.Supports(row.Assessment.Rubric))
                throw Error(409, "Môn/rubric này chưa có quy tắc điều chỉnh điểm được cấu hình.");
            var correctedScore = SkillReviewRules.CorrectedScore(input.Score);
            var band = SportAssessmentScoring.Band(correctedScore);
            repository.AddPlayerRatingHistories(new PlayerRatingHistory { PlayerSportProfileId = profile.Id, AssessmentId = row.AssessmentId, ChangedBy = actor, OldScore = profile.CurrentScore, NewScore = input.Score, OldBand = profile.CurrentBand, NewBand = band });
            profile.CurrentScore = input.Score;
            profile.CurrentBand = band;
            profile.RatingStatus = "Rated";
            await assessments.CheckFutureTicketsAsync(profile, actor, ct);
        }

        row.Status = input.Correct ? "Corrected" : "Kept";
        row.ReviewedBy = actor;
        Record("RatingAppeal", id, actor, "appeal_decided", row.Status, reason);
        return new
        {
            id,
            row.Status
        };
    }, ct);
}
