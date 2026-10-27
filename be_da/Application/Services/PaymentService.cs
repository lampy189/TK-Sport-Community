using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Domain.Entities;
using TKSC.Domain.Payments;
using static TKSC.Application.Validators.PaymentValidator;

namespace TKSC.Application.Services;

public sealed class PaymentService(IPaymentRepository repository, IPaymentGateway gateway, TimeProvider clock) : IPaymentService
{
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
                    foreach (var ticket in order.Tickets)
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

    private static bool Eligible(SocialBookingOrder order) => order.Tickets.Count > 0
        && order.Tickets.All(t => (t.Status == "reserved" || t.Status == "Pending"))
        && order.Tickets.Sum(t => (long)t.AmountVnd) == order.TotalAmount;
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
