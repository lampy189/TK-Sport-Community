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
public sealed class SkillAssessmentService(ISkillAssessmentRepository repository, TimeProvider clock) : ISkillAssessmentService
{
    public async Task<object> SessionsAsync(long actorId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        return await repository.SessionsSessionsListAsync(actorId, ct, today);
    }

    public async Task<object> PlayerContextAsync(long sessionId, long playerId, long actorId, CancellationToken ct)
    {
        var session = await AssignedSessionAsync(sessionId, actorId, ct);
        // Only expose context for an eligible attendee of the referee's assigned session.
        var attendee = (await GetAttendeesAsync(sessionId, actorId, ct)).SingleOrDefault(p => p.PlayerId == playerId) ?? throw Error(403, "Chỉ xem hồ sơ người chơi đã điểm danh trong buổi được phân công.");
        var venueId = await repository.PlayerContextCourtsAsync(ct, session);
        var profile = await repository.PlayerContextPlayerSportProfilesAsync(playerId, ct, session);
        var participation = await repository.PlayerContextTicketsListAsync(sessionId, playerId, ct, session, venueId);
        var ratings = await repository.PlayerContextPlayerRatingHistoriesListAsync(playerId, ct, session, venueId);
        return new
        {
            attendee.PlayerId,
            attendee.Name,
            Profile = profile,
            Participation = participation,
            Ratings = ratings
        };
    }

    public async Task<RubricView> GetRubricAsync(long sportId, CancellationToken ct) => Map(await ActiveRubricAsync(sportId, ct));
    private async Task<Rubric> ActiveRubricAsync(long sportId, CancellationToken ct)
    {
        var rubrics = await repository.ActiveRubricListAsync(sportId, ct);
        if (rubrics.Count == 0)
            throw Error(404, "Môn này chưa có bộ tiêu chí đang hoạt động.");
        if (rubrics.Count != 1)
            throw Error(409, "Môn này có nhiều rubric hoạt động. Cần Manager kiểm tra cấu hình.");
        if (rubrics[0].RubricDimensions.Count == 0)
            throw Error(409, "Rubric chưa có tiêu chí.");
        return rubrics[0];
    }

    private async Task<Session> AssignedSessionAsync(long sessionId, long actorId, CancellationToken ct)
    {
        if (!await repository.AssignedSessionSessionRefereesExistsAsync(sessionId, actorId, ct))
            throw Error(403, "Bạn không được phân công chấm điểm buổi chơi này.");
        var session = await repository.AssignedSessionSessionsAsync(sessionId, ct) ?? throw Error(404, "Không tìm thấy buổi chơi.");
        if (string.Equals(session.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            throw Error(409, "Buổi chơi đã bị hủy.");
        return session;
    }

    public async Task<IReadOnlyList<AttendeeView>> GetAttendeesAsync(long sessionId, long actorId, CancellationToken ct)
    {
        var session = await AssignedSessionAsync(sessionId, actorId, ct);
        var players = await repository.GetAttendeesTicketsListAsync(sessionId, actorId, ct);
        var ids = players.Select(p => p.PlayerId).ToArray();
        var profiles = await repository.GetAttendeesPlayerSportProfilesListAsync(ct, session, ids);
        var forms = await repository.GetAttendeesAssessmentsListAsync(sessionId, actorId, ct);
        return players.Select(p =>
        {
            var profile = profiles.SingleOrDefault(x => x.PlayerId == p.PlayerId);
            return new AttendeeView(p.PlayerId, p.FullName, profile?.Id, profile?.CurrentBand, profile?.RatingStatus, forms.SingleOrDefault(a => a.PlayerSportProfileId == profile?.Id)?.Id);
        }).ToList();
    }

    public Task<AssessmentView> OpenAsync(long sessionId, long playerId, long actorId, CancellationToken ct) => repository.MutateAsync(async () =>
    {
        var session = await AssignedSessionAsync(sessionId, actorId, ct);
        var profile = await repository.OpenPlayerSportProfilesAsync(playerId, ct, session) ?? throw Error(409, "Người chơi chưa có hồ sơ cho môn của buổi chơi.");
        await CheckAttendanceAsync(sessionId, profile, actorId, ct);
        var existing = await repository.OpenAsync(sessionId, actorId, ct, profile);
        if (existing != null)
            return await ViewAsync(existing, ct);
        var rubric = await ActiveRubricAsync(session.SportId, ct);
        var assessment = new Assessment
        {
            SessionId = sessionId,
            PlayerSportProfile = profile,
            Rubric = rubric,
            AssessorId = actorId,
            AssessmentType = "on_court",
            Status = "draft",
            AssessmentScores = rubric.RubricDimensions.Select(d => new AssessmentScore { DimensionId = d.Id }).ToList()
        };
        repository.AddAssessments(assessment);
        await repository.SaveAsync(ct);
        Audit(assessment, "assessment_opened", new { at = clock.GetUtcNow(), rubric.Version });
        await repository.SaveAsync(ct);
        return await ViewAsync(assessment, ct);
    }, ct);
    private async Task CheckAttendanceAsync(long sessionId, PlayerSportProfile profile, long actorId, CancellationToken ct)
    {
        if (profile.Player.UserId == actorId)
            throw Error(403, "Không được tự chấm điểm hồ sơ của mình.");
        if (!await repository.CheckAttendanceTicketsExistsAsync(sessionId, profile, ct))
            throw Error(409, "Chỉ mở phiếu cho người chơi có vé đã thanh toán và đã điểm danh.");
    }

    private async Task<Assessment> LoadAsync(long id, long actorId, CancellationToken ct)
    {
        var a = await repository.LoadAsync(id, ct) ?? throw Error(404, "Không tìm thấy phiếu chấm điểm.");
        if (a.AssessorId != actorId || a.SessionId is null || a.AssessmentType != "on_court")
            throw Error(403, "Bạn không có quyền truy cập phiếu này.");
        var session = await AssignedSessionAsync(a.SessionId.Value, actorId, ct);
        if (session.SportId != a.Rubric.SportId || session.SportId != a.PlayerSportProfile.SportId)
            throw Error(409, "Môn của phiếu, rubric và buổi chơi không khớp.");
        return a;
    }

    public async Task<AssessmentView> ReadAsync(long id, long actorId, CancellationToken ct) => await ViewAsync(await LoadAsync(id, actorId, ct), ct);
    public Task<AssessmentView> SaveAsync(long id, long actorId, SaveAssessmentRequest request, CancellationToken ct) => repository.MutateAsync(async () =>
    {
        var a = await LoadAsync(id, actorId, ct);
        await EditableAsync(a, request.Revision, ct);
        await CheckAttendanceAsync(a.SessionId!.Value, a.PlayerSportProfile, actorId, ct);
        var dimensions = a.Rubric.RubricDimensions.Select(d => d.Id).OrderBy(x => x).ToArray();
        if (request.Marks is null || request.Marks.Any(m => m is null) || !request.Marks.Select(m => m.DimensionId).OrderBy(x => x).SequenceEqual(dimensions))
            throw Error(400, "Gửi mỗi tiêu chí của rubric đúng một lần; dùng null cho tiêu chí chưa quan sát.");
        if (request.Marks.Any(m => m.Evidence?.Length > 2000))
            throw Error(400, "Ghi chú mỗi tiêu chí tối đa 2000 ký tự.");
        if (!SportAssessmentScoring.Supports(a.Rubric) && request.Marks.Any(m => m.Score != null))
            throw Error(409, "Chưa có quy tắc thang điểm cho rubric này; có thể lưu ghi chú với điểm null.");
        if (request.Marks.Any(m => m.Score is < 0 or > 4))
            throw Error(400, "Điểm mỗi tiêu chí phải từ 0 đến 4.");
        var marks = request.Marks.Select(NormalizeMark).ToArray();
        foreach (var mark in marks)
            a.AssessmentScores.Single(s => s.DimensionId == mark.DimensionId).Score = mark.Score;
        a.TotalScore = SportAssessmentScoring.Supports(a.Rubric) ? CalculateTotal(marks) : null;
        a.BandResult = a.TotalScore is short total ? SportAssessmentScoring.Band(total) : null;
        Audit(a, "assessment_saved", new AssessmentEvidence(clock.GetUtcNow(), marks));
        await repository.SaveAsync(ct);
        return await ViewAsync(a, ct);
    }, ct);
    public Task<AssessmentView> PublishAsync(long id, long actorId, PublishAssessmentRequest request, CancellationToken ct) => repository.MutateAsync(async () =>
    {
        var a = await LoadAsync(id, actorId, ct);
        // Retry after a lost response must never append history or refund twice.
        if (a.Status == "published")
            return await ViewAsync(a, ct);
        await EditableAsync(a, request.Revision, ct);
        await CheckAttendanceAsync(a.SessionId!.Value, a.PlayerSportProfile, actorId, ct);
        if (!SportAssessmentScoring.Supports(a.Rubric))
            throw Error(409, "Rubric chưa có quy tắc công bố điểm.");
        var total = CalculateTotal(await MarksAsync(a, ct)) ?? throw Error(409, "Phải quan sát đủ các tiêu chí trước khi công bố.");
        var profile = a.PlayerSportProfile;
        if (profile.CurrentScore != request.ExpectedScore || profile.CurrentBand != request.ExpectedBand)
            throw Error(409, "Hồ sơ đã thay đổi. Tải lại trước khi công bố.");
        var observedSession = await repository.PublishSessionsAsync(ct, a);
        var observedDate = observedSession.SessionDate;
        var observedTime = observedSession.TimeSlot.StartTime;
        if (await repository.PublishAssessmentsExistsAsync(ct, profile, observedDate, observedTime))
            throw Error(409, "Đã có kết quả từ buổi chơi mới hơn. Phiếu cũ cần review thay vì ghi đè hồ sơ.");
        var previous = await repository.PublishAssessmentsAsync(ct, a, profile, observedDate, observedTime);
        var confirmed = string.Equals(profile.RatingStatus, "rated", StringComparison.OrdinalIgnoreCase) || await repository.PublishPlayerRatingHistoriesExistsAsync(ct, profile);
        if (confirmed)
        {
            if (previous?.TotalScore is not short prior)
                throw Error(409, "Đánh giá định kỳ cần hai lần quan sát đầy đủ ở hai buổi chơi khác nhau.");
            total = SportAssessmentScoring.ReviewScore(total, prior);
        }

        var band = SportAssessmentScoring.Band(total);
        // Appendix A leaves essential movement/coordination thresholds unspecified.
        // Do not bypass those checks by automatically approving a confirmed band change.
        if (confirmed && !string.Equals(profile.CurrentBand, band, StringComparison.OrdinalIgnoreCase))
            throw Error(409, "Đổi hạng đã xác nhận cần quy trình review của Manager và kiểm tra vận động/phối hợp đôi. Phiếu vẫn được lưu nháp.");
        var oldScore = profile.CurrentScore;
        var oldBand = profile.CurrentBand;
        repository.AddPlayerRatingHistories(new PlayerRatingHistory { PlayerSportProfileId = profile.Id, OldScore = oldScore, NewScore = total, OldBand = oldBand, NewBand = band, ChangedBy = actorId, AssessmentId = a.Id });
        profile.CurrentScore = total;
        profile.CurrentBand = band;
        profile.RatingStatus = "Rated";
        a.Status = "published";
        // Keep the raw observation on Assessment; the smoothed published value lives in history/profile.
        a.TotalScore = CalculateTotal(await MarksAsync(a, ct));
        a.BandResult = SportAssessmentScoring.Band(a.TotalScore!.Value);
        if (!string.Equals(oldBand, band, StringComparison.OrdinalIgnoreCase))
            await CheckFutureTicketsAsync(profile, actorId, ct);
        Audit(a, "assessment_published", new { at = clock.GetUtcNow(), oldScore, oldBand, score = total, band, reason = "Published complete on-court observation", supportingAssessmentId = confirmed ? previous?.Id : null });
        await repository.SaveAsync(ct);
        return await ViewAsync(a, ct);
    }, ct);
    public async Task CheckFutureTicketsAsync(PlayerSportProfile profile, long actorId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
        var today = DateOnly.FromDateTime(now.DateTime);
        var time = TimeOnly.FromDateTime(now.DateTime);
        var tickets = await repository.ListFuturePaidTicketsAsync(profile, ct, today, time);
        foreach (var ticket in tickets)
        {
            if (string.IsNullOrWhiteSpace(ticket.Session.Band) || string.Equals(ticket.Session.Band, profile.CurrentBand, StringComparison.OrdinalIgnoreCase) || await repository.CheckFutureTicketsSessionExceptionsExistsAsync(profile, ct, ticket))
                continue;
            var payment = ticket.Order.Payments.Where(p => p.Status == "paid" && p.GoodsOrderId == null).OrderByDescending(p => p.Id).FirstOrDefault() ?? throw Error(409, "Vé tương lai thiếu giao dịch thanh toán; cần Staff kiểm tra trước khi công bố.");
            if (!await repository.CheckFutureTicketsRefundsExistsAsync(ct, ticket))
                repository.AddRefunds(new Refund { PaymentId = payment.Id, TicketId = ticket.Id, RecipientId = ticket.Order.PayerId, AmountVnd = ticket.AmountVnd, Status = "requested", InitiatedBy = actorId });
            repository.AddAuditLogs(new AuditLog { ActorId = actorId, Action = "rating_ticket_invalidated", EntityType = "Ticket", EntityId = ticket.Id, OldValue = ticket.Status, NewValue = JsonSerializer.Serialize(new { at = clock.GetUtcNow(), band = profile.CurrentBand, refund = "requested" }) });
            ticket.Status = "cancelled";
        }
    }

    private async Task EditableAsync(Assessment a, long revision, CancellationToken ct)
    {
        if (a.Status != "draft")
            throw Error(409, "Không được sửa phiếu đã công bố.");
        if (await RevisionAsync(a.Id, ct) != revision)
            throw Error(409, "Phiếu đã thay đổi. Tải lại để tránh ghi đè.");
    }

    private async Task<long> RevisionAsync(long id, CancellationToken ct) => await repository.RevisionAuditLogsMaxAsync(id, ct) ?? 0;
    private async Task<IReadOnlyList<MarkInput>> EvidenceAsync(long id, CancellationToken ct)
    {
        var json = await repository.EvidenceAuditLogsAsync(id, ct);
        return json is null ? Array.Empty<MarkInput>() : JsonSerializer.Deserialize<AssessmentEvidence>(json)!.Marks;
    }

    private static MarkInput NormalizeMark(MarkInput mark)
    {
        try
        {
            _ = SportAssessmentScoring.CriterionPoints(mark.Score, mark.SliderScores);
        }
        catch (ArgumentException e)
        {
            throw Error(400, e.Message);
        }

        return mark.SliderScores is null ? mark : mark with
        {
            Score = (short)Math.Round(mark.SliderScores.Sum(v => (decimal)v) / 10m, MidpointRounding.AwayFromZero)
        };
    }

    private static short? CalculateTotal(IEnumerable<MarkInput> marks) => SportAssessmentScoring.CalculatePoints(marks.Select(m => SportAssessmentScoring.CriterionPoints(m.Score, m.SliderScores)));
    private async Task<IReadOnlyList<MarkInput>> MarksAsync(Assessment a, CancellationToken ct)
    {
        var evidence = await EvidenceAsync(a.Id, ct);
        return a.AssessmentScores.OrderBy(s => s.DimensionId).Select(s => new MarkInput(s.DimensionId, s.Score, evidence.FirstOrDefault(m => m.DimensionId == s.DimensionId)?.Evidence, evidence.FirstOrDefault(m => m.DimensionId == s.DimensionId)?.SliderScores)).ToList();
    }

    private async Task<AssessmentView> ViewAsync(Assessment a, CancellationToken ct) => new(a.Id, a.SessionId!.Value, a.PlayerSportProfile.PlayerId, a.Status!, await RevisionAsync(a.Id, ct), Map(a.Rubric), await MarksAsync(a, ct), a.TotalScore, a.BandResult, a.PlayerSportProfile.CurrentScore, a.PlayerSportProfile.CurrentBand);
    private static RubricView Map(Rubric r) => new(r.Id, r.SportId, r.Version, r.Name, SportAssessmentScoring.Supports(r), r.RubricDimensions.OrderBy(d => d.Code).Select(d => new CriterionView(d.Id, d.Code, d.Name, d.WeightPercent)).ToList());
    private void Audit(Assessment a, string action, object value) => repository.AddAuditLogs(new AuditLog { ActorId = a.AssessorId, Action = action, EntityType = "Assessment", EntityId = a.Id, NewValue = JsonSerializer.Serialize(value) });
    private static AssessmentException Error(int status, string message) => new(status, message);
}
