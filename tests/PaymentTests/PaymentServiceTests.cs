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
        public Task SaveAsync() => Task.CompletedTask;
    }
}
