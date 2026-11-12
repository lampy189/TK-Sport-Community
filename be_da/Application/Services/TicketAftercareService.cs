using TKSC.Domain.Exceptions;
using be_da.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;

namespace TKSC.Application.Services;
public sealed class TicketAftercareService(ITicketAftercareRepository repository, TimeProvider clock) : ITicketAftercareService
{
    private DateTimeOffset Now => clock.GetUtcNow();

    private static void Require(bool ok, string message, int code = 409)
    {
        if (!ok)
            throw new PaymentException(code, message);
    }

    private static string Evidence(string? text) => TicketAftercareRules.Evidence(text);
    private static bool Paid(string status) => status.Equals("paid", StringComparison.OrdinalIgnoreCase);
    private void Audit(long actor, string action, string entity, long id, string? old, string value) => repository.AddAuditLogs(new AuditLog { ActorId = actor, Action = action, EntityType = entity, EntityId = id, OldValue = old, NewValue = $"{Now:O} | {value}" });
    private void Notify(Ticket t, string kind) => repository.AddNotifications(TicketNotifications.Create(kind, t.Id, t.Player.UserId, t.Order.PayerId));
    private async Task<Session> Session(long id, bool locked = true)
    {
        if (locked)
            _ = await repository.SessionSessionsAsync(id);
        return await repository.GetSessionDetailsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy buổi chơi.");
    }

    private async Task<Ticket> Ticket(long id, long? user = null)
    {
        var orderId = await repository.TicketTicketsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy vé.");
        await repository.LockOrder(orderId);
        var t = await repository.GetTicketDetailsAsync(id);
        if (user.HasValue)
            Require(t.Player.UserId == user || t.Order.PayerId == user, "Không tìm thấy vé của bạn.", 404);
        t.Session = await Session(t.SessionId);
        return t;
    }

    private async Task Eligibility(long playerId, Session session, long except)
    {
        Require(await repository.EligibilityPlayersExistsAsync(playerId), "Tài khoản người chơi không còn hoạt động.");
        if (!string.IsNullOrWhiteSpace(session.Band) && !await repository.EligibilitySessionExceptionsExistsAsync(playerId, session))
        {
            var profile = await repository.EligibilityPlayerSportProfilesAsync(playerId, session);
            Require(profile?.CurrentScore != null && !string.IsNullOrWhiteSpace(profile.CurrentBand) && profile.RatingStatus?.ToLowerInvariant()is not ("unrated" or "pendingsurvey") && string.Equals(profile.CurrentBand.Trim(), session.Band.Trim(), StringComparison.OrdinalIgnoreCase), "Người chơi không đáp ứng trình độ của buổi mới.");
        }

        Require(!await repository.EligibilityTicketsExistsAsync(playerId, session, except, Now), "Người chơi đã có vé hoặc giữ chỗ trùng giờ.");
    }

    private void Resolvable(Ticket t)
    {
        Require(Paid(t.Order.Status) && t.Status.ToLowerInvariant() == "confirmed" && t.CheckedInBy == null, "Chỉ xử lý vé đã thanh toán, chưa check-in và chưa hủy/đổi.");
        Require(TicketAftercareRules.Disrupted(t.Session.Status), "Buổi chơi chưa được báo sự cố.");
        Require(!t.Refunds.Any(r => r.Status != "failed"), "Vé đang có yêu cầu hoàn tiền.");
    }

    private async Task<List<TicketFunding>> Funding(Ticket t)
    {
        if (t.AmountVnd == 0)
            return[];
        var funds = await repository.FundingTicketFundingListAsync(t);
        if (funds.Count == 0)
        {
            var p = t.Order.Payments.OrderBy(p => p.Id).FirstOrDefault(p => Paid(p.Status));
            if (p == null)
                p = await repository.FundingPaymentsAsync(t);
            Require(p != null && p.AmountVnd >= t.AmountVnd, "Chưa xác định được nguồn tiền đã trả. Cần đối soát trước khi xử lý.");
            funds.Add(new TicketFunding { TicketId = t.Id, PaymentId = p!.Id, AmountVnd = t.AmountVnd });
            repository.AddTicketFunding(funds[0]);
        }

        Require(funds.Sum(f => f.AmountVnd) == t.AmountVnd, "Nguồn tiền vé không khớp, cần kiểm tra.");
        return funds;
    }

    private async Task Refund(Ticket t, long paymentId, int amount, long actor)
    {
        var p = await repository.RefundPaymentsAsync(paymentId);
        var already = await repository.RefundRefundsTotalAsync(paymentId) ?? 0;
        already += repository.RefundChangeTrackerSum(paymentId);
        Require(amount > 0 && already + amount <= p.AmountVnd, "Số tiền hoàn vượt khoản đã thanh toán.");
        repository.AddRefunds(new Refund { TicketId = t.Id, PaymentId = paymentId, RecipientId = t.Order.PayerId, AmountVnd = amount, Status = "requested", InitiatedBy = actor });
    }

    public async Task<object> SessionsAsync(bool issuesOnly = false)
    {
        var today = DateOnly.FromDateTime(Now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        return await repository.SessionsSessionsListAsync(issuesOnly, today);
    }

    public Task<object> ReportAsync(long id, long actor, string? reason, bool cancel) => repository.Transaction<object>(async () =>
    {
        var note = Evidence(reason);
        var s = await Session(id);
        var status = TicketAftercareRules.VenueDecision(s.Status, cancel);
        if (s.Status.ToLowerInvariant() == status)
            return new
            {
                status = s.Status
            };
        var old = s.Status;
        s.Status = status;
        Audit(actor, "venue_condition_issue", "Session", id, old, note);
        var tickets = await repository.ReportTicketsListAsync(id);
        foreach (var t in tickets)
            Notify(t, "ticket_venue_issue");
        return new
        {
            status,
            affected = tickets.Count
        };
    });
    public async Task<object> OptionsAsync(long id, long user)
    {
        // The final submission rechecks all conditions under session/order locks.
        var ticket = await repository.OptionsTicketsAsync(id, user) ?? throw new PaymentException(404, "Không tìm thấy vé.");
        Resolvable(ticket);
        var today = DateOnly.FromDateTime(Now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var targets = await repository.OptionsSessionsListAsync(ticket, today);
        var result = new List<object>();
        foreach (var target in targets)
        {
            try
            {
                var count = await repository.OptionsCountAsync(target);
                TicketAftercareRules.CheckTarget(ticket.Session, target, count, Now);
                await Eligibility(ticket.PlayerId, target, ticket.Id);
                result.Add(new { target.Id, target.SessionDate, target.TimeSlot.StartTime, target.TimeSlot.EndTime, Court = target.Court.Name, Sport = target.Sport.Name, target.PriceVnd, DifferenceVnd = target.PriceVnd - ticket.AmountVnd, Available = TicketAftercareRules.Limit(target.Capacity) - count });
            }
            catch (PaymentException)
            {
            }
        }

        return result;
    }

    public Task<object> RequestChangeAsync(long id, long targetId, long user) => repository.Transaction<object>(async () =>
    {
        var t = await Ticket(id, user);
        Resolvable(t);
        Require(!await repository.RequestChangeTicketChangesExistsAsync(id), "Vé đã có yêu cầu đổi đang chờ.");
        var target = await Session(targetId);
        TicketAftercareRules.CheckTarget(t.Session, target, await repository.RequestChangeCountAsync(targetId), Now);
        await Eligibility(t.PlayerId, target, id);
        var change = new TicketChange
        {
            TicketId = id,
            TargetSessionId = targetId,
            OldPriceVnd = t.AmountVnd,
            NewPriceVnd = target.PriceVnd,
            RequestedBy = user,
            CreatedAt = Now
        };
        repository.AddTicketChanges(change);
        Audit(user, "ticket_change_requested", "Ticket", id, t.SessionId.ToString(), $"target={targetId}; difference={target.PriceVnd - t.AmountVnd}");
        Notify(t, "ticket_change_requested");
        await repository.SaveAsync();
        return new
        {
            change.Id,
            change.Status,
            DifferenceVnd = target.PriceVnd - t.AmountVnd
        };
    });
    public Task<object> CancelChangeAsync(long id, long user) => repository.Transaction<object>(async () =>
    {
        var lookup = await repository.CancelChangeTicketChangesAsync(id) ?? throw new PaymentException(404, "Không tìm thấy yêu cầu đổi.");
        var t = await Ticket(lookup.TicketId, user);
        var change = await repository.GetTrackedChangeForCancellationAsync(id);
        Require(change.Status == "requested", "Yêu cầu đã được xử lý.");
        change.Status = "cancelled";
        change.CompletedAt = Now;
        Audit(user, "ticket_change_cancelled", "Ticket", t.Id, null, $"change={id}");
        return new
        {
            change.Status
        };
    });
    public Task<object> RequestRefundAsync(long id, long user) => repository.Transaction<object>(async () =>
    {
        var t = await Ticket(id, user);
        Resolvable(t);
        Require(!await repository.RequestRefundTicketChangesExistsAsync(id), "Hãy rút yêu cầu đổi trước khi yêu cầu hoàn tiền.");
        foreach (var f in await Funding(t))
        {
            await Refund(t, f.PaymentId, f.AmountVnd, user);
            f.Retired = true;
        }

        t.Status = "cancelled";
        Audit(user, "venue_refund_requested", "Ticket", id, "confirmed", $"amount={t.AmountVnd}; recipient={t.Order.PayerId}");
        Notify(t, "ticket_refund_requested");
        return new
        {
            status = "requested",
            amountVnd = t.AmountVnd
        };
    });
    public async Task<object> ChangesAsync() => await repository.ChangesTicketChangesListAsync();
    public Task<object> CompleteChangeAsync(long id, long actor, string? evidence, string? bankReference) => repository.Transaction<object>(async () =>
    {
        var note = Evidence(evidence);
        var snapshot = await repository.CompleteChangeTicketChangesAsync(id) ?? throw new PaymentException(404, "Không tìm thấy yêu cầu đổi.");
        var t = await Ticket(snapshot.TicketId);
        var change = await repository.GetTrackedChangeForSettlementAsync(id);
        Require(change.Status == "requested", "Yêu cầu đã xử lý.");
        Resolvable(t);
        Require(actor != t.Order.PayerId && actor != t.Player.UserId, "Không tự duyệt đổi vé của chính mình.", 403);
        var target = await Session(change.TargetSessionId);
        TicketAftercareRules.CheckTarget(t.Session, target, await repository.CompleteChangeCountAsync(target), Now);
        await Eligibility(t.PlayerId, target, t.Id);
        Require(target.PriceVnd == change.NewPriceVnd && t.AmountVnd == change.OldPriceVnd, "Giá đã thay đổi. Hãy rút yêu cầu và chọn lại.");
        var funds = await Funding(t);
        var delta = change.NewPriceVnd - change.OldPriceVnd;
        if (delta > 0)
        {
            bankReference = bankReference?.Trim();
            Require(bankReference is { Length: >= 6 and <= 100 }, "Nhập mã giao dịch khoản thu thêm.", 400);
            Require(!await repository.CompleteChangeBankTransactionsExistsAsync(bankReference) && !await repository.CompleteChangePaymentsExistsAsync(bankReference), "Mã giao dịch đã được sử dụng.");
        }

        var order = new SocialBookingOrder
        {
            PayerId = t.Order.PayerId,
            OrderCode = $"EX-{Guid.NewGuid():N}",
            TotalAmount = target.PriceVnd,
            Status = "paid"
        };
        repository.AddSocialBookingOrders(order);
        var replacement = new Ticket
        {
            Order = order,
            SessionId = target.Id,
            PlayerId = t.PlayerId,
            Player = t.Player,
            Status = "confirmed",
            AmountVnd = target.PriceVnd,
            TicketCode = $"TK-{Guid.NewGuid():N}"};
        repository.AddTickets(replacement);
        await repository.SaveAsync();
        foreach (var part in TicketAftercareRules.Split(funds, target.PriceVnd))
        {
            if (part.Transfer > 0)
                repository.AddTicketFunding(new TicketFunding { TicketId = replacement.Id, PaymentId = part.PaymentId, AmountVnd = part.Transfer });
            if (part.Refund > 0)
                await Refund(t, part.PaymentId, part.Refund, change.RequestedBy);
        }

        if (delta > 0)
        {
            var payment = new Payment
            {
                OrderId = order.Id,
                PayerId = order.PayerId,
                AmountVnd = delta,
                Method = "bank_transfer",
                Status = "paid",
                ReferenceCode = bankReference!,
                ConfirmedBy = actor
            };
            repository.AddPayments(payment);
            await repository.SaveAsync();
            repository.AddBankTransactions(new BankTransaction { ExternalId = "staff:" + bankReference, ReferenceCode = bankReference, AmountVnd = delta, MatchedPaymentId = payment.Id, MatchStatus = "matched", RawPayload = note });
            repository.AddTicketFunding(new TicketFunding { TicketId = replacement.Id, PaymentId = payment.Id, AmountVnd = delta });
        }

        foreach (var f in funds)
            f.Retired = true;
        t.Status = "exchanged";
        change.Status = "completed";
        change.ProcessedBy = actor;
        change.NewTicketId = replacement.Id;
        change.CompletedAt = Now;
        var partners = await repository.CompleteChangePartnerRequestsListAsync(t);
        foreach (var p in partners)
            p.Status = "cancelled";
        Audit(actor, "ticket_changed", "Ticket", t.Id, $"session={t.SessionId}; code={t.TicketCode}", $"new_ticket={replacement.Id}; new_session={target.Id}; difference={delta}; {note}");
        Notify(replacement, "ticket_changed");
        if (delta < 0)
            Notify(t, "ticket_difference_refund");
        return new
        {
            status = "completed",
            ticketId = replacement.Id,
            differenceVnd = delta
        };
    });
    public async Task<object> RefundsAsync() => await repository.RefundsRefundsListAsync();
    public Task<object> CompleteRefundAsync(long id, long actor, string? evidence, string? reference) => repository.Transaction<object>(async () =>
    {
        var note = Evidence(evidence);
        reference = reference?.Trim();
        Require(reference is { Length: >= 6 and <= 100 }, "Nhập mã giao dịch hoàn tiền.", 400);
        var snapshot = await repository.CompleteRefundRefundsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy khoản hoàn.");
        var ticket = await Ticket(snapshot.TicketId);
        var r = await repository.GetTrackedRefundForSettlementAsync(id);
        Require(r.Status is "requested" or "processing", "Khoản hoàn đã được xử lý.");
        Require(actor != r.RecipientId && actor != r.InitiatedBy, "Không tự xác nhận hoàn tiền của chính mình.", 403);
        Require(!await repository.CompleteRefundAuditLogsExistsAsync(reference), "Mã hoàn tiền đã được dùng cho khoản khác.");
        var payment = await repository.CompleteRefundPaymentsAsync(r);
        var total = await repository.CompleteRefundRefundsTotalAsync(r) ?? 0;
        Require(r.AmountVnd > 0 && total <= payment.AmountVnd, "Số tiền hoàn không hợp lệ.");
        r.Status = "completed";
        r.ApprovedBy = actor;
        repository.AddAuditLogs(new AuditLog { ActorId = actor, Action = "refund_transfer_reference", EntityType = "Refund", EntityId = id, NewValue = reference });
        Audit(actor, "refund_completed", "Refund", id, "requested", $"amount={r.AmountVnd}; recipient={r.RecipientId}; {note}");
        Notify(ticket, "ticket_refund_completed");
        return new
        {
            status = "completed"
        };
    });
    public async Task<object> ReviewsAsync() => await repository.ReviewsPaymentsListAsync();
    public Task<object> ApprovePaymentAsync(long id, long bankId, long actor, string? evidence) => repository.Transaction<object>(async () =>
    {
        var note = Evidence(evidence);
        var lookup = await repository.ApprovePaymentPaymentsAsync(id) ?? throw new PaymentException(404, "Không tìm thấy thanh toán vé.");
        await repository.LockOrder(lookup.OrderId!.Value);
        var order = await repository.ApprovePaymentSocialBookingOrdersAsync(lookup);
        var payment = order.Payments.Single(p => p.Id == id);
        Require(payment.Status == "review_required" && order.Status.ToLower() == "review_required", "Đơn không còn chờ duyệt hoặc đã thanh toán.");
        Require(actor != order.PayerId, "Không tự duyệt thanh toán của mình.", 403);
        var bank = await repository.ApprovePaymentBankTransactionsAsync(id, bankId);
        Require(bank != null && bank.AmountVnd == order.TotalAmount && payment.AmountVnd == order.TotalAmount, "Chọn đúng giao dịch đã đối soát và đủ số tiền. Không tự duyệt khoản thiếu/thừa.");
        var tickets = await repository.ApprovePaymentTicketsListAsync(order);
        Require(tickets.Count > 0 && tickets.All(t => t.Status.ToLower()is "pending" or "reserved") && tickets.Sum(t => t.AmountVnd) == order.TotalAmount, "Vé không còn đủ điều kiện kích hoạt.");
        foreach (var group in tickets.GroupBy(t => t.SessionId).OrderBy(g => g.Key))
        {
            var session = await Session(group.Key);
            Require(session.Status.ToLower()is "open" or "published" or "ongoing", "Buổi chơi đang dừng hoặc chưa mở.");
            if (group.Any(t => !TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode)))
                Require(new DateTimeOffset(session.SessionDate.ToDateTime(session.TimeSlot.StartTime), TimeSpan.FromHours(7)) > Now, "Buổi chơi đã bắt đầu.");
            var other = await repository.ApprovePaymentCountAsync(order, session);
            Require(other + group.Count() <= TicketAftercareRules.Limit(session.Capacity), "Buổi chơi không còn đủ chỗ.");
            foreach (var t in group)
            {
                if (TKSC.Domain.Payments.LateEntryRules.IsLate(t.TicketCode))
                {
                    await repository.LoadLateEntryEligibilityAsync(t, Now);
                    Require(t.LateEntryEligible, "Vé giờ lẻ đã hết hiệu lực hoặc không còn đủ điều kiện kích hoạt.");
                }
                else
                    await Eligibility(t.PlayerId, session, t.Id);
                t.Order = order;
            }
        }

        payment.Status = "paid";
        payment.ConfirmedBy = actor;
        order.Status = "paid";
        bank!.MatchStatus = "matched";
        foreach (var t in tickets)
        {
            t.Status = "confirmed";
            t.TicketCode ??= $"TK-{Guid.NewGuid():N}";
            Notify(t, "ticket_paid");
        }

        Audit(actor, "flagged_payment_approved", "Payment", id, "review_required", $"bank={bankId}; {note}");
        return new
        {
            status = "paid"
        };
    });
}
