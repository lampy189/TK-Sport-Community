using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Domain.Entities;
using TKSC.Domain.Payments;
using static TKSC.Application.Validators.PaymentValidator;

namespace TKSC.Application.Services;

public sealed class PaymentService(IPaymentRepository repository, IPaymentGateway gateway, TimeProvider clock) : IPaymentService
{
    // Business rule chosen by the product owner (28/10/2026): paid tickets can be cancelled
    // up to 12 hours before the session start for a 100% refund handled by staff.
    public static readonly TimeSpan PaidCancelWindow = TimeSpan.FromHours(12);
    // Session dates/slots are stored as Vietnam local time (UTC+7, no DST).
    private static readonly TimeSpan VenueOffset = TimeSpan.FromHours(7);

    public async Task<TicketCancelResult> CancelTicketAsync(long ticketId, long userId)
    {
        var context = await repository.GetTicketCancelContextAsync(ticketId)
            ?? throw new PaymentException(404, "Không tìm thấy vé.");
        var start = new DateTimeOffset(context.SessionDate.ToDateTime(context.StartTime), VenueOffset);
        return await repository.InTransactionAsync(context.OrderId, null, async order =>
        {
            OwnOrder(order, userId);
            var now = clock.GetUtcNow();
            var ticket = order!.Tickets.SingleOrDefault(t => t.Id == ticketId)
                ?? throw new PaymentException(404, "Không tìm thấy vé.");
            var status = ticket.Status.ToLowerInvariant();
            if (status == "cancelled") throw new PaymentException(409, "Vé đã được hủy trước đó.");
            if (status == "checkedin" || ticket.CheckedInBy is not null)
                throw new PaymentException(409, "Vé đã check-in, không thể hủy.");
            if (start <= now) throw new PaymentException(409, "Buổi chơi đã bắt đầu, không thể hủy vé.");
            var orderStatus = order.Status.ToLowerInvariant();
            if (orderStatus == "review_required")
                throw new PaymentException(409, "Đơn đang được kiểm tra thanh toán, chưa thể hủy vé.");

            if (orderStatus is not ("paid" or "pending") || status is not ("confirmed" or "reserved" or "pending"))
                throw new PaymentException(409, "Vé không còn đủ điều kiện hủy.");
            if (orderStatus == "pending" && (order.ExpiresAt is null || order.ExpiresAt <= now))
                throw new PaymentException(409, "Vé đã hết thời gian giữ chỗ.");
            var refundAmount = 0;
            if (orderStatus == "paid")
            {
                if (start - now < PaidCancelWindow)
                    throw new PaymentException(409, "Chỉ hủy được vé đã thanh toán trước giờ chơi ít nhất 12 giờ.");
                var payment = order.Payments.Where(p => p.Status == "paid").OrderByDescending(p => p.Id).FirstOrDefault()
                    ?? throw new PaymentException(409, "Không tìm thấy giao dịch đã thanh toán của vé.");
                refundAmount = ticket.AmountVnd;
                // Refund goes to the original payer, never automatically to the invited player.
                repository.AddRefund(new Refund { PaymentId = payment.Id, TicketId = ticket.Id, RecipientId = order.PayerId,
                    AmountVnd = refundAmount, Status = "requested", InitiatedBy = userId });
            }
            else
            {
                // Unpaid hold: release the place and keep the remaining tickets payable.
                var remaining = order.Tickets.Where(t => t.Id != ticket.Id
                    && !string.Equals(t.Status, "cancelled", StringComparison.OrdinalIgnoreCase)).ToList();
                order.TotalAmount = remaining.Sum(t => t.AmountVnd);
                if (remaining.Count == 0) order.Status = "cancelled";
                // An open payOS link carries the old amount; retire it so checkout creates a new one.
                // A late transfer on the old link is then routed to staff review by the webhook rules.
                foreach (var pending in order.Payments.Where(p => p.Status == "pending"))
                {
                    Audit(pending, "ticket_cancelled_retire_link", $"Vé {ticket.Id} bị hủy, tổng đơn đổi.", userId);
                    pending.Status = "failed";
                }
            }
            repository.AddAudit(new AuditLog { ActorId = userId, Action = "ticket_cancelled", EntityType = "Ticket",
                EntityId = ticket.Id, OldValue = ticket.Status,
                NewValue = refundAmount > 0 ? $"Yêu cầu hoàn {refundAmount} VND" : "Hủy giữ chỗ" });
            ticket.Status = "cancelled";
            await repository.SaveAsync();
            return new TicketCancelResult(ticket.Id, refundAmount > 0 ? "refund_requested" : "cancelled", refundAmount);
        });
    }
    public async Task<OrderSummary> DisplayAsync(long id, long userId) =>
        await repository.GetOrderAsync(id, userId) ?? throw new PaymentException(404, "Không tìm thấy đơn của bạn.");
    public Task<IReadOnlyList<TicketSummary>> TicketsAsync(long userId) => repository.GetTicketsAsync(userId);
    public Task<IReadOnlyList<ReviewSummary>> ReviewsAsync() => repository.GetReviewsAsync();

    public async Task<CheckoutResult> ProcessAsync(long id, long userId)
    {
        gateway.ValidateConfiguration();
        var attempt = await repository.InTransactionAsync(id, null, async order =>
        {
            OwnOrder(order, userId);
            if (!string.Equals(order!.Status, "pending", StringComparison.OrdinalIgnoreCase) || order.TotalAmount <= 0 || order.ExpiresAt is null
                || order.ExpiresAt <= clock.GetUtcNow() || !Eligible(order))
                throw new PaymentException(409, "Đơn hết hạn hoặc không còn đủ điều kiện thanh toán.");
            if (order.Payments.Any(p => p.Status is "paid" or "review_required"))
                throw new PaymentException(409, "Đơn đã thanh toán hoặc đang chờ kiểm tra.");
            var payment = order.Payments.SingleOrDefault(p => p.Status == "pending");
            if (payment is null)
            {
                payment = new Payment { OrderId = id, PayerId = userId, AmountVnd = order.TotalAmount,
                    ReferenceCode = $"TKSC{Guid.NewGuid():N}", Method = "payos", Status = "pending" };
                repository.AddPayment(payment);
                await repository.SaveAsync();
            }
            if (payment.Method != "payos")
                throw new PaymentException(409, "Đơn có giao dịch thuộc cổng khác; cần kiểm tra trước khi tiếp tục.");
            return (payment.Id, payment.AmountVnd, Expiry: order.ExpiresAt.Value);
        });
        // Commit the attempt BEFORE the external call: timeout/retry must reuse its numeric orderCode.
        var link = await gateway.CreateAsync(attempt.Id, id, attempt.AmountVnd, attempt.Expiry);
        return await repository.InTransactionAsync(id, null, async order =>
        {
            OwnOrder(order, userId);
            var payment = order!.Payments.Single(p => p.Id == attempt.Id);
            if (link.OrderCode != payment.Id || link.Amount != payment.AmountVnd
                || !Uri.TryCreate(link.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https"
                || string.IsNullOrWhiteSpace(link.LinkId))
                throw new GatewayException("Thông tin link thanh toán không khớp.");
            payment.GatewayPaymentLinkId ??= link.LinkId;
            if (payment.GatewayPaymentLinkId != link.LinkId)
                throw new GatewayException("Mã link thanh toán không khớp.");
            payment.CheckoutUrl = link.Url;
            await repository.SaveAsync();
            // Cancellation is reconciled by RefreshAsync; never trust browser query parameters.
            if (link.Status is not "PENDING" and not "PROCESSING" || payment.Status != "pending"
                || !string.Equals(order.Status, "pending", StringComparison.OrdinalIgnoreCase) || order.ExpiresAt <= clock.GetUtcNow())
                return new CheckoutResult(payment.Id, payment.ReferenceCode, "refresh_required", "");
            return new CheckoutResult(payment.Id, payment.ReferenceCode, payment.Status, link.Url);
        });
    }

    public async Task<OrderSummary> RefreshAsync(long id, long userId)
    {
        var snapshot = await DisplayAsync(id, userId);
        var latest = snapshot.Payments.FirstOrDefault();
        if (latest?.Status == "pending")
        {
            var remote = await gateway.GetAsync(latest.Id);
            if (remote.OrderCode != latest.Id) throw new GatewayException("Mã giao dịch không khớp.");
            if (remote.Status == "PAID" && remote.Transactions is { Count: > 0 })
            {
                if (remote.Transactions.Sum(t => t.Amount) != remote.AmountPaid)
                    throw new GatewayException("Chi tiết giao dịch payOS chưa khớp tổng tiền.");
                foreach (var transfer in remote.Transactions)
                    await HandleVerifiedEventAsync(transfer, remote);
                return await DisplayAsync(id, userId);
            }
            await repository.InTransactionAsync(id, null, async order =>
            {
                OwnOrder(order, userId);
                var payment = order!.Payments.Single(p => p.Id == latest.Id);
                if (payment.Status == "pending")
                {
                    if (remote.AmountPaid > 0 && (remote.AmountPaid != payment.AmountVnd
                        || order.ExpiresAt <= clock.GetUtcNow()))
                        Flag(order, payment, "payos_status_exception", "Số tiền hoặc thời hạn cần đối soát.");
                    else if (remote.AmountPaid == 0 && remote.Status is "CANCELLED" or "EXPIRED" or "FAILED")
                    {
                        Audit(payment, "payos_status", remote.Status);
                        payment.Status = "failed";
                    }
                    // PAID alone does not issue tickets; the verified webhook records the bank event.
                    await repository.SaveAsync();
                }
                return true;
            });
        }
        return await DisplayAsync(id, userId);
    }

    public Task<bool> FlagAsync(long id, long userId, string? reason)
    {
        ReviewReason(reason);
        return repository.InTransactionAsync(id, null, async order =>
        {
            OwnOrder(order, userId);
            var payment = order!.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
            if (payment is null || order.Status == "paid")
                throw new PaymentException(409, "Không có giao dịch cần kiểm tra.");
            if (payment.Status != "review_required")
            {
                Flag(order, payment, "flag_payment_for_review", reason!.Trim(), userId);
                await repository.SaveAsync();
            }
            return true;
        });
    }

    public async Task HandleWebhookAsync(string payload)
    {
        var data = await gateway.VerifyAsync(payload);
        await HandleVerifiedEventAsync(data);
    }

    private async Task HandleVerifiedEventAsync(VerifiedBankEvent data, GatewayStatus? verifiedRemote = null)
    {
        if (data.Amount is <= 0 or > int.MaxValue || string.IsNullOrWhiteSpace(data.Reference)
            || data.Reference.Length > 180 || string.IsNullOrWhiteSpace(data.LinkId))
            throw new PaymentException(400, "Dữ liệu giao dịch không hợp lệ.");
        var orderId = await repository.FindOrderForPaymentAsync(data.OrderCode);
        // Confirm link identity independently, including when the webhook beats CreateAsync's response.
        // This also prevents payOS's signed registration-test payload from matching a coincidental local ID.
        var remote = orderId is null ? null : verifiedRemote ?? await gateway.GetAsync(data.OrderCode);
        var externalId = $"payos:{data.Reference}";
        await repository.InTransactionAsync(orderId, externalId, async order =>
        {
            if (await repository.EventExistsAsync(externalId)) return true;
            var payment = order?.Payments.SingleOrDefault(p => p.Id == data.OrderCode && p.Method == "payos");
            var bankEvent = new BankTransaction { ExternalId = externalId, AmountVnd = (int)data.Amount,
                ReferenceCode = payment?.ReferenceCode ?? data.OrderCode.ToString(),
                MatchedPaymentId = payment?.Id, MatchStatus = "review_required", RawPayload = data.RawPayload };
            repository.AddEvent(bankEvent);
            if (payment is not null && order is not null)
            {
                var state = PaymentRules.Decide(payment.AmountVnd, data.Amount, data.Success, order.ExpiresAt,
                    clock.GetUtcNow(), string.Equals(order.Status, "pending", StringComparison.OrdinalIgnoreCase) && payment.Status == "pending"
                        && Eligible(order) && payment.AmountVnd == order.TotalAmount
                        && data.Currency == "VND"
                        && remote?.OrderCode == data.OrderCode && remote.LinkId == data.LinkId
                        && (payment.GatewayPaymentLinkId is null || payment.GatewayPaymentLinkId == data.LinkId));
                payment.GatewayPaymentLinkId ??= data.LinkId;
                if (state == "paid")
                {
                    Audit(payment, "payos_payment_confirmed", data.Reference);
                    payment.Status = "paid"; order.Status = "paid"; bankEvent.MatchStatus = "matched";
                    foreach (var ticket in order.Tickets.Where(t => !string.Equals(t.Status, "cancelled", StringComparison.OrdinalIgnoreCase)))
                    {
                        ticket.Status = "confirmed";
                        ticket.TicketCode ??= $"TK-{Guid.NewGuid():N}";
                    }
                }
                else
                {
                    // Each new bank reference is retained. Extra transfers never issue another ticket.
                    Flag(order, payment, "payos_payment_exception",
                        $"Giao dịch {data.Reference}: cần kiểm tra số tiền, thời hạn hoặc điều kiện vé.");
                }
            }
            await repository.SaveAsync();
            return true;
        });
    }

    private static IEnumerable<Ticket> Active(SocialBookingOrder order) =>
        order.Tickets.Where(t => !string.Equals(t.Status, "cancelled", StringComparison.OrdinalIgnoreCase));
    private static bool Eligible(SocialBookingOrder order) => Active(order).Any()
        && Active(order).All(t => (t.Status == "reserved" || t.Status == "Pending"))
        && Active(order).Sum(t => (long)t.AmountVnd) == order.TotalAmount;
    private void Flag(SocialBookingOrder order, Payment payment, string action, string detail, long? actor = null)
    {
        Audit(payment, action, detail, actor);
        payment.Status = "review_required";
        // A surplus transfer is a financial exception, not a reason to revoke an already valid ticket.
        if (order.Status != "paid") order.Status = "review_required";
    }
    private void Audit(Payment payment, string action, string detail, long? actor = null) =>
        repository.AddAudit(new AuditLog { ActorId = actor, Action = action, EntityType = "Payment",
            EntityId = payment.Id, OldValue = payment.Status, NewValue = detail });
}
