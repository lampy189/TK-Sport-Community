using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;

public sealed class TicketCheckInService(IPaymentRepository repository, TimeProvider clock)
    : IRefereeSessionsService, ITicketCheckInService
{
    public async Task<IReadOnlyList<AssignedSessionSummary>> GetAssignedSessionsAsync(long actorId)
    {
        var sessions = await repository.GetAssignedSessionsAsync(actorId);
        var localNow = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var nowTime = TimeOnly.FromDateTime(localNow.DateTime);

        return sessions.Select(session =>
        {
            var isActive = !string.Equals(session.SessionStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.SessionStatus, "completed", StringComparison.OrdinalIgnoreCase)
                && session.SessionDate == today
                && nowTime >= session.StartTime
                && nowTime < session.EndTime;

            var conditionCode = isActive ? "ready" : session.SessionDate < today || nowTime >= session.EndTime ? "completed" : "not_ready";
            var conditionMessage = conditionCode switch
            {
                "ready" => "Buổi đang mở, có thể điểm danh người chơi.",
                "completed" => "Buổi đã kết thúc hoặc đã ngoài ngày hoạt động.",
                _ => string.Equals(session.SessionStatus, "cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "Buổi đã bị hủy; không thực hiện check-in."
                    : "Buổi chưa đến giờ hoặc chưa phù hợp điều kiện sân."
            };

            return session with { ConditionCode = conditionCode, ConditionMessage = conditionMessage };
        }).ToList();
    }

    public async Task<CheckInResult> ExecuteAsync(string? ticketCode, long actorId, bool manager, bool confirm)
    {
        var code = ticketCode?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100)
            throw new PaymentException(400, "Mã QR hoặc mã vé không hợp lệ.");
        var orderId = await repository.FindOrderForTicketCodeAsync(code)
            ?? throw new PaymentException(404, "Không tìm thấy vé. Hãy quét QR vé vào sân, không phải QR thanh toán.");
        // The same order lock is used by payment and cancellation, so admission cannot race a cancellation.
        return await repository.InTransactionAsync(orderId, null, async order =>
        {
            var context = await repository.GetCheckInContextAsync(code, actorId)
                ?? throw new PaymentException(404, "Không tìm thấy vé.");
            if (!manager && !context.Assigned)
                throw new PaymentException(403, "Bạn không được phân công điểm danh buổi chơi này.");
            var ticket = order?.Tickets.SingleOrDefault(t => t.Id == context.TicketId && t.TicketCode == code)
                ?? throw new PaymentException(404, "Không tìm thấy vé.");
            if (!string.Equals(order!.Status, "paid", StringComparison.OrdinalIgnoreCase)
                || !(string.Equals(ticket.Status, "confirmed", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ticket.Status, "checkedin", StringComparison.OrdinalIgnoreCase)))
                throw new PaymentException(409, "Vé chưa thanh toán, đã hủy hoặc không còn hiệu lực. Không thể vào sân.");
            var already = ticket.CheckedInBy is not null || string.Equals(ticket.Status, "checkedin", StringComparison.OrdinalIgnoreCase);
            if (TKSC.Domain.Payments.LateEntryRules.IsLate(code))
            {
                if(ticket.LateEntryEndsAt is null || ticket.LateEntryEndsAt<=clock.GetUtcNow())
                    throw new PaymentException(409,"Vé phần buổi còn lại đã hết hiệu lực.");
                if(!already&&!ticket.LateEntryEligible)
                    throw new PaymentException(409,"Vé không còn đủ điều kiện giờ chơi, trình độ hoặc sức chứa. Liên hệ Staff xử lý.");
            }
            if (!already)
            {
                var localNow = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
                if (context.SessionDate != DateOnly.FromDateTime(localNow.DateTime)
                    || TimeOnly.FromDateTime(localNow.DateTime) >= context.EndTime
                    || context.SessionStatus.ToLowerInvariant() is "cancelled" or "completed" or "suspended")
                    throw new PaymentException(409, "Vé chỉ được check-in trong ngày chơi và trước khi buổi chơi kết thúc.");
                if (confirm)
                {
                    repository.AddAudit(new AuditLog { ActorId = actorId, Action = "ticket_checked_in", EntityType = "Ticket",
                        EntityId = ticket.Id, OldValue = ticket.Status,
                        NewValue = $"checkedin; {clock.GetUtcNow():O}; session={ticket.SessionId}" });
                    ticket.Status = "checkedin";
                    ticket.CheckedInBy = actorId;
                    await repository.SaveAsync();
                }
            }
            return new CheckInResult(ticket.Id, code, context.HolderName, context.SportName, context.CourtName,
                context.SessionDate, context.StartTime, context.EndTime, already || confirm, ticket.CheckedInBy);
        });
    }
}
