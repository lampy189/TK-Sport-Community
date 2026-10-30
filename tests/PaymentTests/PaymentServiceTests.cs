using TKSC.Application.Services;
using TKSC.Application.Interfaces;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using Xunit;

public class PaymentServiceTests
{
    [Fact]
    public async Task SessionRegistrationPendingTicketsCanBePaid()
    {
        var (service, repo, _) = Setup();
        repo.Order.Status = "Pending";
        foreach (var ticket in repo.Order.Tickets)
        {
            ticket.Status = "Pending";
            ticket.TicketCode = $"TKT-{ticket.Id}";
        }
        var before = TKSC.Application.Mappings.PaymentMappings.ToSummary(repo.Order);
        Assert.Equal("pending", before.Status);
        Assert.All(before.Tickets, t => Assert.False(t.Valid));
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        var after = TKSC.Application.Mappings.PaymentMappings.ToSummary(repo.Order);
        Assert.Equal("paid", after.Status);
        Assert.All(after.Tickets, t => Assert.True(t.Valid));
    }
    [Fact]
    public async Task CancelUnpaidTicketReleasesPlaceAndKeepsRestPayable()
    {
        var (service, repo, _) = Setup();
        var result = await service.CancelTicketAsync(1, 7);
        Assert.Equal("cancelled", result.Status);
        Assert.Equal("cancelled", repo.Order.Tickets.First(t => t.Id == 1).Status);
        Assert.Equal(60000, repo.Order.TotalAmount);
        Assert.Empty(repo.Refunds);
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed-60000");
        Assert.Equal("cancelled", repo.Order.Tickets.First(t => t.Id == 1).Status);
    }
    [Fact]
    public async Task CancelPaidTicketCreatesRefundForPayer()
    {
        var (service, repo, _) = Setup();
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        var result = await service.CancelTicketAsync(2, 7);
        Assert.Equal("refund_requested", result.Status);
        var refund = Assert.Single(repo.Refunds);
        Assert.Equal(60000, refund.AmountVnd);
        Assert.Equal(7, refund.RecipientId);
        Assert.Equal("paid", repo.Order.Status);
        Assert.Equal("confirmed", repo.Order.Tickets.First(t => t.Id == 1).Status);
    }
    [Fact]
    public async Task CancelPaidTicketInsideTwelveHoursIsRejected()
    {
        var (service, repo, _) = Setup();
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        repo.SessionDate = DateOnly.FromDateTime(Now.UtcDateTime); // 18:00 VN = 11:00Z, Now = 10:00Z
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.CancelTicketAsync(1, 7));
        Assert.Equal(409, error.StatusCode);
        Assert.Empty(repo.Refunds);
    }
    [Fact]
    public async Task CancellationCannotCreateDuplicateRefundOrCancelOthersTicket()
    {
        var (service, repo, _) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => service.CancelTicketAsync(1, 99));
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        await service.CancelTicketAsync(1, 7);
        await Assert.ThrowsAsync<PaymentException>(() => service.CancelTicketAsync(1, 7));
        Assert.Single(repo.Refunds);
    }
    [Fact]
    public async Task ExpiredHoldCannotBeCancelled()
    {
        var (service, repo, _) = Setup();
        repo.Order.ExpiresAt = Now.AddMinutes(-1);
        await Assert.ThrowsAsync<PaymentException>(() => service.CancelTicketAsync(1, 7));
        Assert.Equal(120000, repo.Order.TotalAmount);
        Assert.Empty(repo.Refunds);
    }
    [Fact]
    public async Task QrInspectionDoesNotAdmitAndRepeatCheckInIsIdempotent()
    {
        var (_, repo, _) = Setup();
        repo.Order.Status = "paid";
        var ticket = repo.Order.Tickets.First(); ticket.Status = "confirmed"; ticket.TicketCode = "TK-test";
        repo.SessionDate = DateOnly.FromDateTime(Now.UtcDateTime);
        var service = new TicketCheckInService(repo, new Clock());
        var preview = await service.ExecuteAsync("TK-test", 10, false, false);
        Assert.False(preview.CheckedIn); Assert.Null(ticket.CheckedInBy); Assert.Empty(repo.Audits);
        var result = await service.ExecuteAsync("TK-test", 10, false, true);
        Assert.True(result.CheckedIn); Assert.Equal(10, ticket.CheckedInBy);
        await service.ExecuteAsync("TK-test", 11, true, true);
        Assert.Equal(10, ticket.CheckedInBy); Assert.Single(repo.Audits);
    }
    [Theory]
    [InlineData("pending", "Pending")]
    [InlineData("paid", "cancelled")]
    [InlineData("paid", "invalidated")]
    [InlineData("review_required", "confirmed")]
    public async Task InvalidTicketsCannotEnter(string orderStatus, string ticketStatus)
    {
        var (_, repo, _) = Setup();
        repo.Order.Status = orderStatus;
        var ticket = repo.Order.Tickets.First(); ticket.Status = ticketStatus; ticket.TicketCode = "TK-test";
        repo.SessionDate = DateOnly.FromDateTime(Now.UtcDateTime);
        var service = new TicketCheckInService(repo, new Clock());
        await Assert.ThrowsAsync<PaymentException>(() => service.ExecuteAsync("TK-test", 10, true, true));
        Assert.Null(ticket.CheckedInBy); Assert.Empty(repo.Audits);
    }
    [Fact]
    public async Task UnassignedRefereeAndWrongDayCannotCheckIn()
    {
        var (_, repo, _) = Setup(); repo.Order.Status = "paid";
        var ticket = repo.Order.Tickets.First(); ticket.Status = "confirmed"; ticket.TicketCode = "TK-test";
        repo.Assigned = false;
        var service = new TicketCheckInService(repo, new Clock());
        Assert.Equal(403, (await Assert.ThrowsAsync<PaymentException>(() => service.ExecuteAsync("TK-test", 10, false, true))).StatusCode);
        Assert.Equal(409, (await Assert.ThrowsAsync<PaymentException>(() => service.ExecuteAsync("TK-test", 10, true, true))).StatusCode);
        Assert.Null(ticket.CheckedInBy); Assert.Empty(repo.Audits);
    }
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-20T10:00:00Z");
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => Now; }
    private static (PaymentService Service, Repository Repo, Gateway Gateway) Setup()
    {
        var repo = new Repository();
        var gateway = new Gateway();
        return (new PaymentService(repo, gateway, new Clock()), repo, gateway);
    }
    [Fact]
    public async Task SuccessConfirmsEachFriendsTicketAndDuplicateDoesNotIssueAgain()
    {
        var (service, repo, _) = Setup();
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        var codes = repo.Order.Tickets.Select(t => t.TicketCode).ToList();
        await service.HandleWebhookAsync("signed");
        Assert.Equal("paid", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Equal("confirmed", t.Status));
        Assert.All(codes, c => Assert.NotNull(c));
        Assert.Equal(codes, repo.Order.Tickets.Select(t => t.TicketCode));
        Assert.Single(repo.Events);
    }
    [Theory]
    [InlineData(119999)]
    [InlineData(120001)]
    public async Task PartialOrSurplusTransferIsReviewedWithoutTickets(long amount)
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Event = gateway.Event with { Amount = amount };
        await service.HandleWebhookAsync("signed");
        Assert.Equal("review_required", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Null(t.TicketCode));
        Assert.Single(await service.ReviewsAsync());
    }
    [Fact]
    public async Task LateTransferCannotReactivateExpiredHold()
    {
        var (service, repo, _) = Setup();
        await service.ProcessAsync(1, 7);
        repo.Order.ExpiresAt = Now.AddSeconds(-1);
        await service.HandleWebhookAsync("signed");
        Assert.Equal("review_required", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Null(t.TicketCode));
    }
    [Fact]
    public async Task UnknownOrderIsStoredForReview()
    {
        var (service, repo, gateway) = Setup();
        gateway.Event = gateway.Event with { OrderCode = 999 };
        await service.HandleWebhookAsync("signed");
        Assert.Null(Assert.Single(repo.Events).MatchedPaymentId);
        Assert.Equal("review_required", repo.Events[0].MatchStatus);
        Assert.Equal("pending", repo.Order.Status);
    }
    [Fact]
    public async Task TimeoutRetryReusesSameAttempt()
    {
        var (service, repo, gateway) = Setup();
        gateway.FailCreate = true;
        await Assert.ThrowsAsync<GatewayException>(() => service.ProcessAsync(1, 7));
        gateway.FailCreate = false;
        await service.ProcessAsync(1, 7);
        Assert.Single(repo.Order.Payments);
        Assert.Equal(new long[] { 12, 12 }, gateway.CreatedCodes);
    }
    [Fact]
    public async Task AnotherPayerCannotStartOrFlagPayment()
    {
        var (service, repo, _) = Setup();
        Assert.Equal(404, (await Assert.ThrowsAsync<PaymentException>(() => service.ProcessAsync(1, 99))).StatusCode);
        await Assert.ThrowsAsync<PaymentException>(() => service.FlagAsync(1, 99, "Tôi đã chuyển tiền rồi."));
        Assert.Empty(repo.Order.Payments);
    }
    [Fact]
    public async Task FlaggedPaymentStaysInReviewWhenWebhookArrives()
    {
        var (service, repo, _) = Setup();
        await service.ProcessAsync(1, 7);
        await service.FlagAsync(1, 7, "Tôi đã chuyển tiền nhưng chưa có vé.");
        await service.HandleWebhookAsync("signed");
        Assert.Equal("review_required", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Null(t.TicketCode));
    }
    [Fact]
    public async Task ExtraTransferIsRecordedWithoutRevokingPreviouslyConfirmedTickets()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        gateway.Event = gateway.Event with { Reference = "BANK2" };
        await service.HandleWebhookAsync("signed");
        Assert.Equal(2, repo.Events.Count);
        Assert.Equal("paid", repo.Order.Status);
        Assert.Equal("review_required", repo.Order.Payments.Single().Status);
        Assert.All(repo.Order.Tickets, t => Assert.Equal("confirmed", t.Status));
    }
    [Fact]
    public async Task RemotePaidStatusWithoutBankEventDoesNotGrantTickets()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Remote = gateway.Remote with { Status = "PAID", AmountPaid = 120000 };
        await service.RefreshAsync(1, 7);
        Assert.Equal("pending", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Null(t.TicketCode));
    }
    [Fact]
    public async Task SignedCancellationAllowsNewAttemptOnlyAfterServerRefresh()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Remote = gateway.Remote with { Status = "CANCELLED" };
        await service.RefreshAsync(1, 7);
        Assert.Equal("failed", repo.Order.Payments.Single().Status);
        await service.ProcessAsync(1, 7);
        Assert.Equal(2, repo.Order.Payments.Count);
    }
    [Fact]
    public async Task MismatchedLinkCannotConfirmTicket()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Event = gateway.Event with { LinkId = "another-link" };
        await service.HandleWebhookAsync("signed");
        Assert.Equal("review_required", repo.Order.Status);
    }
    [Fact]
    public async Task VerifiedApiTransferConfirmsAndLaterWebhookIsDuplicate()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Remote = new(12, 120000, "PAID", "link12", [gateway.Event]);
        await service.RefreshAsync(1, 7);
        await service.RefreshAsync(1, 7);
        await service.HandleWebhookAsync("signed");
        Assert.Equal("paid", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Equal("confirmed", t.Status));
        Assert.Single(repo.Events);
    }

    [Fact]
    public async Task PaidApiStatusWithoutTransactionsDoesNotIssueTickets()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Remote = new(12, 120000, "PAID", "link12");
        await service.RefreshAsync(1, 7);
        Assert.Equal("pending", repo.Order.Status);
        Assert.Empty(repo.Events);
    }

    [Fact]
    public async Task ApiTransferWithWrongLinkRequiresReview()
    {
        var (service, repo, gateway) = Setup();
        await service.ProcessAsync(1, 7);
        gateway.Remote = new(12, 120000, "PAID", "wrong-link", [gateway.Event with { LinkId = "wrong-link" }]);
        await service.RefreshAsync(1, 7);
        Assert.Equal("review_required", repo.Order.Status);
        Assert.All(repo.Order.Tickets, t => Assert.Equal("reserved", t.Status));
    }

    private sealed class Gateway : IPaymentGateway
    {
        public bool FailCreate;
        public List<long> CreatedCodes = [];
        public VerifiedBankEvent Event = new(12, 120000, "BANK1", "link12", "VND", true, "signed");
        public GatewayStatus Remote = new(12, 0, "PENDING", "link12");
        public void ValidateConfiguration() { }
        public Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt)
        {
            CreatedCodes.Add(paymentId);
            if (FailCreate) throw new GatewayException("timeout");
            return Task.FromResult(new GatewayCheckout(paymentId, amount, $"link{paymentId}",
                $"https://pay.payos.vn/web/link{paymentId}", "PENDING"));
        }
        public Task<GatewayStatus> GetAsync(long paymentId) => Task.FromResult(Remote);
        public Task<VerifiedBankEvent> VerifyAsync(string payload) => Task.FromResult(Event);
    }
    private sealed class Repository : IPaymentRepository
    {
        public SocialBookingOrder Order = new() {
            Id = 1, PayerId = 7, OrderCode = "SOC1", Status = "pending", TotalAmount = 120000,
            ExpiresAt = Now.AddMinutes(15), Tickets = [
                new() { Id = 1, OrderId = 1, PlayerId = 1, Status = "reserved", AmountVnd = 60000 },
                new() { Id = 2, OrderId = 1, PlayerId = 2, Status = "reserved", AmountVnd = 60000 }]
        };
        public List<BankTransaction> Events = [];
        public List<AuditLog> Audits = [];
        public Task<OrderSummary?> GetOrderAsync(long orderId, long payerId) => Task.FromResult(
            orderId == Order.Id && payerId == Order.PayerId ? new OrderSummary(Order.Id, Order.OrderCode,
                Order.TotalAmount, Order.Status, Order.ExpiresAt, Order.Payments.OrderByDescending(p => p.Id)
                    .Select(p => new PaymentSummary(p.Id, p.ReferenceCode, p.Status, p.AmountVnd)).ToList(), []) : null);
        public Task<IReadOnlyList<TicketSummary>> GetTicketsAsync(long userId) => throw new NotImplementedException();
        public Task<IReadOnlyList<ReviewSummary>> GetReviewsAsync() => Task.FromResult<IReadOnlyList<ReviewSummary>>(
            Order.Payments.Where(p => p.Status == "review_required").Select(p =>
                new ReviewSummary(p.Id, p.OrderId, p.ReferenceCode, p.AmountVnd, p.Status, [])).ToList());
        public Task<long?> FindOrderForPaymentAsync(long paymentId) =>
            Task.FromResult(Order.Payments.Any(p => p.Id == paymentId) ? (long?)Order.Id : null);
        public Task<T> InTransactionAsync<T>(long? orderId, string? eventKey, Func<SocialBookingOrder?, Task<T>> action) =>
            action(orderId == Order.Id ? Order : null);
        public Task<bool> EventExistsAsync(string id) => Task.FromResult(Events.Any(e => e.ExternalId == id));
        public void AddPayment(Payment p) { p.Id = 12 + Order.Payments.Count; Order.Payments.Add(p); }
        public void AddEvent(BankTransaction e) => Events.Add(e);
        public void AddAudit(AuditLog a) => Audits.Add(a);
        public DateOnly SessionDate = DateOnly.FromDateTime(Now.UtcDateTime.AddDays(3));
        public List<Refund> Refunds = [];
        public Task<TicketCancelContext?> GetTicketCancelContextAsync(long ticketId) => Task.FromResult(
            Order.Tickets.Any(t => t.Id == ticketId) ? new TicketCancelContext(Order.Id, SessionDate, new TimeOnly(18, 0)) : null);
        public void AddRefund(Refund r) => Refunds.Add(r);
        public bool Assigned = true;
        public Task<long?> FindOrderForTicketCodeAsync(string code) => Task.FromResult<long?>(Order.Tickets.Any(t => t.TicketCode == code) ? Order.Id : null);
        public Task<CheckInContext?> GetCheckInContextAsync(string code, long actorId) => Task.FromResult<CheckInContext?>(
            new CheckInContext(1, Order.Id, SessionDate, new TimeOnly(20, 0), "Open", Assigned, "Người chơi", "Cầu lông", "Sân 1", new TimeOnly(18, 0)));
        public Task SaveAsync() => Task.CompletedTask;
    }
}
