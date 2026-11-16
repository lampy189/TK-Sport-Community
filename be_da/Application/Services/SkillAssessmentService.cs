using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TKSC.Application.Assessments;
using TKSC.Domain.Assessments;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;

namespace TKSC.Application.Services;

public sealed class SkillAssessmentService(Sep490Context db, TimeProvider clock) : ISkillAssessmentService
{
    public async Task<object> SessionsAsync(long actorId, CancellationToken ct) => await db.Sessions.AsNoTracking()
        .Where(s => s.SessionReferees.Any(r => r.RefereeId == actorId) && s.Status != "cancelled")
        .OrderByDescending(s => s.SessionDate).ThenByDescending(s => s.TimeSlot.StartTime).Take(100)
        .Select(s => new { s.Id, s.SessionDate, s.Sport.Name, Court = s.Court.Name, s.TimeSlot.StartTime }).ToListAsync(ct);

    private IQueryable<Rubric> Rubrics => db.Rubrics.Include(r => r.Sport).Include(r => r.RubricDimensions);
    private IQueryable<Assessment> Assessments => db.Assessments
        .Include(a => a.Rubric).ThenInclude(r => r.Sport)
        .Include(a => a.Rubric).ThenInclude(r => r.RubricDimensions)
        .Include(a => a.AssessmentScores)
        .Include(a => a.PlayerSportProfile).ThenInclude(p => p.Player);

    public async Task<RubricView> GetRubricAsync(long sportId, CancellationToken ct) =>
        Map(await ActiveRubricAsync(sportId, ct));

    private async Task<Rubric> ActiveRubricAsync(long sportId, CancellationToken ct)
    {
        var rubrics = await Rubrics.Where(r => r.SportId == sportId && r.IsActive == true).ToListAsync(ct);
        if (rubrics.Count == 0) throw Error(404, "Môn này chưa có bộ tiêu chí đang hoạt động.");
        if (rubrics.Count != 1) throw Error(409, "Môn này có nhiều rubric hoạt động. Cần Manager kiểm tra cấu hình.");
        if (rubrics[0].RubricDimensions.Count == 0) throw Error(409, "Rubric chưa có tiêu chí.");
        return rubrics[0];
    }

    private async Task<Session> AssignedSessionAsync(long sessionId, long actorId, CancellationToken ct)
    {
        if (!await db.SessionReferees.AnyAsync(r => r.SessionId == sessionId && r.RefereeId == actorId, ct))
            throw Error(403, "Bạn không được phân công chấm điểm buổi chơi này.");
        var session = await db.Sessions.SingleOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw Error(404, "Không tìm thấy buổi chơi.");
        if (string.Equals(session.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            throw Error(409, "Buổi chơi đã bị hủy.");
        return session;
    }

    public async Task<IReadOnlyList<AttendeeView>> GetAttendeesAsync(long sessionId, long actorId, CancellationToken ct)
    {
        var session = await AssignedSessionAsync(sessionId, actorId, ct);
        var players = await db.Tickets.Where(t => t.SessionId == sessionId && t.Status == "checkedin"
                && t.CheckedInBy != null && t.Order.Status == "paid" && t.Player.UserId != actorId)
            .Select(t => new { t.PlayerId, t.Player.User.FullName }).Distinct().OrderBy(t => t.FullName).ToListAsync(ct);
        var ids = players.Select(p => p.PlayerId).ToArray();
        var profiles = await db.PlayerSportProfiles.Where(p => ids.Contains(p.PlayerId) && p.SportId == session.SportId)
            .ToListAsync(ct);
        var forms = await db.Assessments.Where(a => a.SessionId == sessionId && a.AssessorId == actorId
            && a.AssessmentType == "on_court").ToListAsync(ct);
        return players.Select(p =>
        {
            var profile = profiles.SingleOrDefault(x => x.PlayerId == p.PlayerId);
            return new AttendeeView(p.PlayerId, p.FullName, profile?.Id, profile?.CurrentBand, profile?.RatingStatus,
                forms.SingleOrDefault(a => a.PlayerSportProfileId == profile?.Id)?.Id);
        }).ToList();
    }

    public Task<AssessmentView> OpenAsync(long sessionId, long playerId, long actorId, CancellationToken ct) =>
        MutateAsync(async () =>
        {
            var session = await AssignedSessionAsync(sessionId, actorId, ct);
            var profile = await db.PlayerSportProfiles.Include(p => p.Player)
                .SingleOrDefaultAsync(p => p.PlayerId == playerId && p.SportId == session.SportId, ct)
                ?? throw Error(409, "Người chơi chưa có hồ sơ cho môn của buổi chơi.");
            await CheckAttendanceAsync(sessionId, profile, actorId, ct);
            var existing = await Assessments.SingleOrDefaultAsync(a => a.SessionId == sessionId
                && a.PlayerSportProfileId == profile.Id && a.AssessorId == actorId && a.AssessmentType == "on_court", ct);
            if (existing != null) return await ViewAsync(existing, ct);
            var rubric = await ActiveRubricAsync(session.SportId, ct);
            var assessment = new Assessment
            {
                SessionId = sessionId, PlayerSportProfile = profile, Rubric = rubric, AssessorId = actorId,
                AssessmentType = "on_court", Status = "draft",
                AssessmentScores = rubric.RubricDimensions.Select(d => new AssessmentScore { DimensionId = d.Id }).ToList()
            };
            db.Assessments.Add(assessment);
            await db.SaveChangesAsync(ct);
            Audit(assessment, "assessment_opened", new { at = clock.GetUtcNow(), rubric.Version });
            await db.SaveChangesAsync(ct);
            return await ViewAsync(assessment, ct);
        }, ct);

    private async Task CheckAttendanceAsync(long sessionId, PlayerSportProfile profile, long actorId, CancellationToken ct)
    {
        if (profile.Player.UserId == actorId) throw Error(403, "Không được tự chấm điểm hồ sơ của mình.");
        if (!await db.Tickets.AnyAsync(t => t.SessionId == sessionId && t.PlayerId == profile.PlayerId
            && t.Status == "checkedin" && t.CheckedInBy != null && t.Order.Status == "paid", ct))
            throw Error(409, "Chỉ mở phiếu cho người chơi có vé đã thanh toán và đã điểm danh.");
    }

    private async Task<Assessment> LoadAsync(long id, long actorId, CancellationToken ct)
    {
        var a = await Assessments.SingleOrDefaultAsync(a => a.Id == id, ct)
            ?? throw Error(404, "Không tìm thấy phiếu chấm điểm.");
        if (a.AssessorId != actorId || a.SessionId is null || a.AssessmentType != "on_court")
            throw Error(403, "Bạn không có quyền truy cập phiếu này.");
        var session = await AssignedSessionAsync(a.SessionId.Value, actorId, ct);
        if (session.SportId != a.Rubric.SportId || session.SportId != a.PlayerSportProfile.SportId)
            throw Error(409, "Môn của phiếu, rubric và buổi chơi không khớp.");
        return a;
    }

    public async Task<AssessmentView> ReadAsync(long id, long actorId, CancellationToken ct) =>
        await ViewAsync(await LoadAsync(id, actorId, ct), ct);

    public Task<AssessmentView> SaveAsync(long id, long actorId, SaveAssessmentRequest request, CancellationToken ct) =>
        MutateAsync(async () =>
        {
            var a = await LoadAsync(id, actorId, ct);
            await EditableAsync(a, request.Revision, ct);
            await CheckAttendanceAsync(a.SessionId!.Value, a.PlayerSportProfile, actorId, ct);
            var dimensions = a.Rubric.RubricDimensions.Select(d => d.Id).OrderBy(x => x).ToArray();
            if (request.Marks is null || request.Marks.Any(m => m is null)
                || !request.Marks.Select(m => m.DimensionId).OrderBy(x => x).SequenceEqual(dimensions))
                throw Error(400, "Gửi mỗi tiêu chí của rubric đúng một lần; dùng null cho tiêu chí chưa quan sát.");
            if (request.Marks.Any(m => m.Evidence?.Length > 2000)) throw Error(400, "Ghi chú mỗi tiêu chí tối đa 2000 ký tự.");
            if (!SportAssessmentScoring.Supports(a.Rubric) && request.Marks.Any(m => m.Score != null))
                throw Error(409, "Chưa có quy tắc thang điểm cho rubric này; có thể lưu ghi chú với điểm null.");
            if (request.Marks.Any(m => m.Score is < 0 or > 4)) throw Error(400, "Điểm mỗi tiêu chí phải từ 0 đến 4.");
            foreach (var mark in request.Marks)
                a.AssessmentScores.Single(s => s.DimensionId == mark.DimensionId).Score = mark.Score;
            a.TotalScore = SportAssessmentScoring.Supports(a.Rubric) ? SportAssessmentScoring.Calculate(request.Marks.Select(m => m.Score)) : null;
            a.BandResult = a.TotalScore is short total ? SportAssessmentScoring.Band(total) : null;
            Audit(a, "assessment_saved", new AssessmentEvidence(clock.GetUtcNow(), request.Marks));
            await db.SaveChangesAsync(ct);
            return await ViewAsync(a, ct);
        }, ct);

    public Task<AssessmentView> PublishAsync(long id, long actorId, PublishAssessmentRequest request, CancellationToken ct) =>
        MutateAsync(async () =>
        {
            var a = await LoadAsync(id, actorId, ct);
            // Retry after a lost response must never append history or refund twice.
            if (a.Status == "published") return await ViewAsync(a, ct);
            await EditableAsync(a, request.Revision, ct);
            await CheckAttendanceAsync(a.SessionId!.Value, a.PlayerSportProfile, actorId, ct);
            if (!SportAssessmentScoring.Supports(a.Rubric)) throw Error(409, "Rubric chưa có quy tắc công bố điểm.");
            var total = SportAssessmentScoring.Calculate(a.AssessmentScores.Select(s => s.Score))
                ?? throw Error(409, "Phải quan sát đủ các tiêu chí trước khi công bố.");
            var evidence = await EvidenceAsync(a.Id, ct);
            if (a.AssessmentScores.Any(s => (s.Score == 0 || s.Score == 4)
                && string.IsNullOrWhiteSpace(evidence.FirstOrDefault(m => m.DimensionId == s.DimensionId)?.Evidence)))
                throw Error(409, "Điểm 0 hoặc 4 cần ghi chú bằng chứng quan sát.");
            var profile = a.PlayerSportProfile;
            if (profile.CurrentScore != request.ExpectedScore || profile.CurrentBand != request.ExpectedBand)
                throw Error(409, "Hồ sơ đã thay đổi. Tải lại trước khi công bố.");
            var observedSession = await db.Sessions.Include(s => s.TimeSlot).SingleAsync(s => s.Id == a.SessionId, ct);
            var observedDate = observedSession.SessionDate;
            var observedTime = observedSession.TimeSlot.StartTime;
            if (await db.Assessments.AnyAsync(x => x.PlayerSportProfileId == profile.Id && x.Status == "published"
                && x.SessionId != null && (x.Session!.SessionDate > observedDate
                    || (x.Session.SessionDate == observedDate && x.Session.TimeSlot.StartTime > observedTime)), ct))
                throw Error(409, "Đã có kết quả từ buổi chơi mới hơn. Phiếu cũ cần review thay vì ghi đè hồ sơ.");
            var previous = await db.Assessments.Where(x => x.PlayerSportProfileId == profile.Id
                    && x.Id != a.Id && x.SessionId != a.SessionId && x.RubricId == a.RubricId
                    && x.SessionId != null && (x.Session!.SessionDate < observedDate
                        || (x.Session.SessionDate == observedDate && x.Session.TimeSlot.StartTime < observedTime))
                    && x.AssessmentType == "on_court" && x.TotalScore != null)
                .OrderByDescending(x => x.Session!.SessionDate).ThenByDescending(x => x.Session!.TimeSlot.StartTime)
                .ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
            var confirmed = string.Equals(profile.RatingStatus, "rated", StringComparison.OrdinalIgnoreCase)
                || await db.PlayerRatingHistories.Where(RatingPublicationRules.OfficialHistory)
                    .AnyAsync(h => h.PlayerSportProfileId == profile.Id, ct);
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
            db.PlayerRatingHistories.Add(new PlayerRatingHistory
            {
                PlayerSportProfileId = profile.Id, OldScore = oldScore, NewScore = total,
                OldBand = oldBand, NewBand = band, ChangedBy = actorId, AssessmentId = a.Id
            });
            profile.CurrentScore = total;
            profile.CurrentBand = band;
            profile.RatingStatus = "Rated";
            a.Status = "published";
            // Keep the raw observation on Assessment; the smoothed published value lives in history/profile.
            a.TotalScore = SportAssessmentScoring.Calculate(a.AssessmentScores.Select(s => s.Score));
            a.BandResult = SportAssessmentScoring.Band(a.TotalScore!.Value);
            if (!string.Equals(oldBand, band, StringComparison.OrdinalIgnoreCase))
                await CheckFutureTicketsAsync(profile, actorId, ct);
            Audit(a, "assessment_published", new { at = clock.GetUtcNow(), oldScore, oldBand, score = total, band,
                reason = "Published complete on-court observation", supportingAssessmentId = confirmed ? previous?.Id : null });
            await db.SaveChangesAsync(ct);
            return await ViewAsync(a, ct);
        }, ct);

    public async Task CheckFutureTicketsAsync(PlayerSportProfile profile, long actorId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
        var today = DateOnly.FromDateTime(now.DateTime);
        var time = TimeOnly.FromDateTime(now.DateTime);
        var tickets = await db.Tickets.Include(t => t.Session).ThenInclude(s => s.TimeSlot)
            .Include(t => t.Order).ThenInclude(o => o.Payments)
            .Where(t => t.PlayerId == profile.PlayerId && t.Session.SportId == profile.SportId
                && t.Status == "confirmed" && t.CheckedInBy == null && t.Order.Status == "paid"
                && (t.Session.SessionDate > today || (t.Session.SessionDate == today && t.Session.TimeSlot.StartTime > time)))
            .ToListAsync(ct);
        foreach (var ticket in tickets)
        {
            if (string.IsNullOrWhiteSpace(ticket.Session.Band)
                || string.Equals(ticket.Session.Band, profile.CurrentBand, StringComparison.OrdinalIgnoreCase)
                || await db.SessionExceptions.AnyAsync(e => e.SessionId == ticket.SessionId
                    && e.PlayerId == profile.PlayerId && e.ApprovedBy != null, ct)) continue;
            var payment = ticket.Order.Payments.Where(p => p.Status == "paid" && p.GoodsOrderId == null)
                .OrderByDescending(p => p.Id).FirstOrDefault()
                ?? throw Error(409, "Vé tương lai thiếu giao dịch thanh toán; cần Staff kiểm tra trước khi công bố.");
            if (!await db.Refunds.AnyAsync(r => r.TicketId == ticket.Id, ct))
                db.Refunds.Add(new Refund { PaymentId = payment.Id, TicketId = ticket.Id,
                    RecipientId = ticket.Order.PayerId, AmountVnd = ticket.AmountVnd, Status = "requested", InitiatedBy = actorId });
            db.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = "rating_ticket_invalidated", EntityType = "Ticket",
                EntityId = ticket.Id, OldValue = ticket.Status,
                NewValue = JsonSerializer.Serialize(new { at = clock.GetUtcNow(), band = profile.CurrentBand, refund = "requested" }) });
            ticket.Status = "cancelled";
        }
    }

    private async Task EditableAsync(Assessment a, long revision, CancellationToken ct)
    {
        if (a.Status != "draft") throw Error(409, "Không được sửa phiếu đã công bố.");
        if (await RevisionAsync(a.Id, ct) != revision) throw Error(409, "Phiếu đã thay đổi. Tải lại để tránh ghi đè.");
    }

    private async Task<long> RevisionAsync(long id, CancellationToken ct) => await db.AuditLogs
        .Where(l => l.EntityType == "Assessment" && l.EntityId == id)
        .Select(l => (long?)l.Id).MaxAsync(ct) ?? 0;

    private async Task<IReadOnlyList<MarkInput>> EvidenceAsync(long id, CancellationToken ct)
    {
        var json = await db.AuditLogs.Where(l => l.EntityType == "Assessment" && l.EntityId == id && l.Action == "assessment_saved")
            .OrderByDescending(l => l.Id).Select(l => l.NewValue).FirstOrDefaultAsync(ct);
        return json is null ? Array.Empty<MarkInput>() : JsonSerializer.Deserialize<AssessmentEvidence>(json)!.Marks;
    }

    private async Task<AssessmentView> ViewAsync(Assessment a, CancellationToken ct)
    {
        var evidence = await EvidenceAsync(a.Id, ct);
        return new(a.Id, a.SessionId!.Value, a.PlayerSportProfile.PlayerId, a.Status!, await RevisionAsync(a.Id, ct),
            Map(a.Rubric), a.AssessmentScores.OrderBy(s => s.DimensionId).Select(s => new MarkInput(s.DimensionId, s.Score,
                evidence.FirstOrDefault(m => m.DimensionId == s.DimensionId)?.Evidence)).ToList(),
            a.TotalScore, a.BandResult, a.PlayerSportProfile.CurrentScore, a.PlayerSportProfile.CurrentBand);
    }

    private static RubricView Map(Rubric r) => new(r.Id, r.SportId, r.Version, r.Name, SportAssessmentScoring.Supports(r),
        r.RubricDimensions.OrderBy(d => d.Code).Select(d => new CriterionView(d.Id, d.Code, d.Name, d.WeightPercent)).ToList());

    private void Audit(Assessment a, string action, object value) => db.AuditLogs.Add(new AuditLog
    { ActorId = a.AssessorId, Action = action, EntityType = "Assessment", EntityId = a.Id, NewValue = JsonSerializer.Serialize(value) });

    private async Task<T> MutateAsync<T>(Func<Task<T>> action, CancellationToken ct)
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
        catch (SqlException e) when (e.Number is 1205 or 51000)
        { throw Error(409, "Dữ liệu đang được cập nhật. Tải lại phiếu và thử lại."); }
    }

    private static AssessmentException Error(int status, string message) => new(status, message);
}
