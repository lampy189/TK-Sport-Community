using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;

namespace TKSC.Application.Services;
public sealed partial class TicketAftercareService
{
    private void RefundAudit(long actor, long id, string action, object value) => repository.AddAuditLogs(new AuditLog { ActorId = actor, EntityType = "Refund", EntityId = id, Action = action, NewValue = JsonSerializer.Serialize(value) });
    private async Task<string> RefundRole(long actor, bool decision = false)
    {
        var user = await repository.RefundActorAsync(actor);
        var role = user?.Role.Code.ToLowerInvariant();
        Require(role is "manager" or "owner" || !decision && role == "staff", "Không có quyền xử lý nghiệp vụ hoàn tiền.", 403);
        return role!;
    }

    private async Task<(Refund Refund, Ticket Ticket)> LockedRefund(long id)
    {
        var snapshot = await repository.CompleteRefundRefundsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy khoản hoàn.");
        var ticket = await Ticket(snapshot.TicketId);
        await repository.LockRefundPaymentAsync(snapshot.PaymentId);
        return (await repository.GetTrackedRefundForSettlementAsync(id), ticket);
    }

    private async Task RefundLimits(Refund r, Ticket ticket)
    {
        var payment = await repository.CompleteRefundPaymentsAsync(r);
        var allocation = await repository.RefundAllocationAsync(r.TicketId, r.PaymentId);
        // Legacy tickets without funding rows must belong directly to the paid order.
        if (allocation == 0 && payment.OrderId == ticket.OrderId)
            allocation = ticket.AmountVnd;
        Require(Paid(payment.Status) && payment.GoodsOrderId == null && r.RecipientId == payment.PayerId, "Khoản hoàn phải thuộc thanh toán vé đã trả và đúng người trả gốc.");
        Require(r.AmountVnd > 0 && r.AmountVnd <= allocation && await repository.RefundAllocationReservedAsync(r.TicketId, r.PaymentId) <= allocation && await repository.RefundTicketTotalAsync(r.TicketId) <= ticket.AmountVnd && (await repository.CompleteRefundRefundsTotalAsync(r) ?? 0) <= payment.AmountVnd, "Tổng tiền hoàn và đang giữ vượt số tiền được hoàn.");
    }

    private async Task<RefundDecisionRecord> ApprovedRefund(Refund r)
    {
        var audit = await repository.RefundAuditAsync(r.Id, "refund_decision");
        var decision = audit?.NewValue == null ? null : JsonSerializer.Deserialize<RefundDecisionRecord>(audit.NewValue);
        Require(decision != null && decision.AmountVnd == r.AmountVnd && decision.RecipientId == r.RecipientId, "Chưa có quyết định hợp lệ hoặc số tiền/người nhận đã thay đổi; cần duyệt lại.");
        return decision!;
    }

    public Task<object> DecideRefundAsync(long id, long actor, RefundDecisionInput input) => repository.Transaction<object>(async () =>
    {
        var role = await RefundRole(actor, true);
        var(r, ticket) = await LockedRefund(id);
        var note = Evidence(input.Reason);
        Require(!await repository.SameRefundPersonAsync(actor, r.InitiatedBy) && !await repository.SameRefundPersonAsync(actor, r.RecipientId), "Không tự phê duyệt khoản hoàn của cùng một người.", 403);
        Require(input.AmountVnd == r.AmountVnd && input.RecipientId == r.RecipientId, "Quyết định phải khớp số tiền và người nhận của yêu cầu.");
        var previousDecision = await repository.RefundAuditAsync(id, "refund_decision");
        if (r.Status == "processing" && input.Approve && previousDecision != null)
        {
            var saved = await ApprovedRefund(r);
            Require(saved.Reason == note && r.ApprovedBy == actor, "Khoản hoàn đã có quyết định khác.");
            return new
            {
                status = r.Status
            };
        }

        Require(r.Status == "requested" || (r.Status is "processing" or "failed" && previousDecision == null && await repository.RefundAuditAsync(id, "refund_payout") == null), "Yêu cầu không còn chờ duyệt.");
        await RefundLimits(r, ticket);
        r.Status = input.Approve ? "processing" : "rejected";
        r.ApprovedBy = actor;
        repository.AddRefundApproval(new Approval { EntityType = "Refund", EntityId = id, RequestedBy = r.InitiatedBy, ApprovedBy = actor, Status = input.Approve ? "approved" : "rejected" });
        RefundAudit(actor, id, input.Approve ? "refund_decision" : "refund_rejected", new RefundDecisionRecord(input.AmountVnd, input.RecipientId, note, role, Now));
        Notify(ticket, input.Approve ? "ticket_refund_approved" : "ticket_refund_rejected");
        return new
        {
            status = r.Status
        };
    });
    public Task<object> RecordRefundPayoutAsync(long id, long actor, RefundPayoutInput input) => repository.Transaction<object>(async () =>
    {
        await RefundRole(actor);
        var(r, ticket) = await LockedRefund(id);
        await ApprovedRefund(r);
        await RefundLimits(r, ticket);
        Require(!await repository.SameRefundPersonAsync(actor, r.RecipientId), "Không tự thực hiện hoàn tiền cho chính mình.", 403);
        Require(r.Status is "processing" or "failed", "Khoản hoàn chưa được duyệt hoặc đã kết thúc.");
        var reference = input.BankReference?.Trim().ToUpperInvariant();
        Require(reference is { Length: >= 6 and <= 100 }, "Nhập mã giao dịch tiền ra từ 6 đến 100 ký tự.", 400);
        var document = Evidence(input.Document);
        Require(input.PaidAt > DateTimeOffset.UnixEpoch && input.PaidAt <= Now, "Thời điểm chuyển tiền không hợp lệ.", 400);
        Require(input.AmountVnd == r.AmountVnd && input.RecipientId == r.RecipientId, "Chứng từ không khớp phạm vi được duyệt.");
        var payout = new RefundPayoutRecord(input.AmountVnd, input.RecipientId, reference!, document, input.PaidAt, actor);
        var existing = await repository.RefundAuditAsync(id, "refund_payout");
        if (existing != null)
        {
            Require(JsonSerializer.Deserialize<RefundPayoutRecord>(existing.NewValue!) == payout, "Đã lưu chứng từ tiền ra; cần đối soát chứng từ hiện có, không chuyển lại.");
            return new
            {
                status = r.Status
            };
        }

        Require(!await repository.CompleteRefundAuditLogsExistsAsync(reference) && !await repository.CompleteChangeBankTransactionsExistsAsync(reference) && !await repository.CompleteChangePaymentsExistsAsync(reference), "Mã giao dịch đã sử dụng hoặc thuộc khoản tiền vào.");
        repository.AddAuditLogs(new AuditLog { ActorId = actor, Action = "refund_transfer_reference", EntityType = "Refund", EntityId = id, NewValue = reference });
        RefundAudit(actor, id, "refund_payout", payout);
        r.Status = "processing";
        Notify(ticket, "ticket_refund_payout_pending");
        return new
        {
            status = r.Status,
            verificationRequired = true
        };
    });
    public Task<object> CompleteRefundAsync(long id, long actor, string? evidence, string? reference) => repository.Transaction<object>(async () =>
    {
        await RefundRole(actor);
        var(r, ticket) = await LockedRefund(id);
        await ApprovedRefund(r);
        await RefundLimits(r, ticket);
        var note = Evidence(evidence);
        var saved = await repository.RefundAuditAsync(id, "refund_payout");
        var payout = saved?.NewValue == null ? null : JsonSerializer.Deserialize<RefundPayoutRecord>(saved.NewValue);
        Require(payout != null && payout.BankReference == reference?.Trim().ToUpperInvariant() && payout.AmountVnd == r.AmountVnd && payout.RecipientId == r.RecipientId, "Phải kiểm tra chứng từ tiền ra đã lưu, đúng mã, số tiền và người nhận.");
        Require(!await repository.SameRefundPersonAsync(actor, payout!.RecordedBy) && !await repository.SameRefundPersonAsync(actor, r.RecipientId), "Người kiểm tra phải độc lập với người ghi chứng từ và người nhận.", 403);
        if (r.Status == "completed")
            return new
            {
                status = r.Status
            };
        Require(r.Status is "processing" or "failed", "Khoản hoàn chưa được duyệt.");
        r.Status = "completed";
        RefundAudit(actor, id, "refund_completed", new { at = Now, payout, verification = note, approvedBy = r.ApprovedBy });
        Notify(ticket, "ticket_refund_completed");
        return new
        {
            status = r.Status
        };
    });
    public Task<object> FailRefundAsync(long id, long actor, string? reason) => repository.Transaction<object>(async () =>
    {
        await RefundRole(actor);
        var(r, ticket) = await LockedRefund(id);
        await ApprovedRefund(r);
        Require(r.Status is "processing" or "failed", "Không thể ghi lỗi cho khoản hoàn chưa duyệt hoặc đã kết thúc.");
        var note = Evidence(reason);
        RefundAudit(actor, id, "refund_failed", new { at = Now, reason = note });
        if (r.Status != "failed")
            Notify(ticket, "ticket_refund_failed");
        r.Status = "failed";
        return new
        {
            status = r.Status
        };
    });
    public async Task<object> RefundHistoryAsync(long id)
    {
        _ = await repository.CompleteRefundRefundsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy khoản hoàn.");
        return (await repository.RefundHistoryAsync(id)).Select(a => new { a.Id, a.ActorId, a.Action, a.NewValue });
    }
}
