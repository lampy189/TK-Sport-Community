using System.Text.Json;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests.CounterSales;

public sealed class CounterRecoveryTests
{
    private const string Evidence = "Verified bank transaction";

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task InvalidEvidenceDoesNotAccessPaymentOrGateway(string evidence)
    {
        var (service, repo, _, gateway) = Setup();
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.ActAsync(4, 22, "note", new(evidence, null), default));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(0, repo.FindCalls);
        Assert.Equal(0, gateway.Calls);
        Assert.Empty(repo.AuditActions);
    }

    [Fact]
    public async Task PayerCannotApproveOwnTransaction()
    {
        var (service, repo, _, gateway) = Setup();
        repo.Payment.PayerId = 22;
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.ActAsync(4, 22, "approve", new(Evidence, 1), default));
        Assert.Equal(403, error.StatusCode);
        Assert.Empty(repo.AuditActions);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task UnsupportedActionIsRejectedBeforeGatewayOrAudit()
    {
        var (service, repo, _, gateway) = Setup();
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.ActAsync(4, 22, "force-paid", new(Evidence, 1), default));
        Assert.Equal(400, error.StatusCode);
        Assert.Empty(repo.AuditActions);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task NoteRecordsRequestAndNoteWithoutChangingPayment()
    {
        var (service, repo, _, gateway) = Setup();
        await service.ActAsync(4, 22, "note", new(Evidence, null), default);
        Assert.Equal(new[] { "payment_recovery_requested", "payment_recovery_note" }, repo.AuditActions);
        Assert.Equal("review_required", repo.Payment.Status);
        Assert.Equal(0, gateway.Calls);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("link")]
    [InlineData("status")]
    public async Task ApprovalRejectsUnverifiedOrMismatchedBankEvidence(string scenario)
    {
        var (service, repo, goodsRepo, gateway) = Setup();
        var transfer = gateway.Remote.Transactions!.Single();
        gateway.Remote = scenario switch
        {
            "amount" => gateway.Remote with { AmountPaid = 49000, Transactions = [transfer with { Amount = 49000 }] },
            "currency" => gateway.Remote with { Transactions = [transfer with { Currency = "USD" }] },
            "link" => gateway.Remote with { LinkId = "other" },
            _ => gateway.Remote with { Status = "PENDING" }
        };
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.ActAsync(4, 22, "approve", new(Evidence, 1), default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(new[] { "payment_recovery_requested", "payment_recovery_failed" }, repo.AuditActions);
        Assert.Equal("review_required", repo.Payment.Status);
        Assert.Equal("NeedsReview", goodsRepo.Order.PaymentState);
        Assert.Equal(0, goodsRepo.Consumed);
    }

    [Fact]
    public async Task VerifiedGoodsRecoveryApprovesMatchingPaymentAndRecordsOutcome()
    {
        var (service, repo, goodsRepo, _) = Setup();
        await service.ActAsync(4, 22, "approve", new(Evidence, 1), default);

        Assert.Equal("Paid", goodsRepo.Order.PaymentState);
        Assert.Equal("paid", repo.Payment.Status);
        Assert.Equal("matched", Assert.Single(goodsRepo.Events).MatchStatus);
        Assert.Equal(0, goodsRepo.Consumed);
        Assert.Equal(new[] { "payment_recovery_requested", "payment_recovery_completed" }, repo.AuditActions);
        Assert.Contains(goodsRepo.Audits, a => a.Action == "goods_review_approved");
    }

    [Fact]
    public async Task GatewayFailureIsRecordedAndPropagated()
    {
        var (service, repo, _, gateway) = Setup();
        gateway.Fail = true;
        await Assert.ThrowsAsync<GatewayException>(() => service.ActAsync(4, 22, "approve", new(Evidence, 1), default));
        Assert.Equal(new[] { "payment_recovery_requested", "payment_recovery_failed" }, repo.AuditActions);
        Assert.Equal("review_required", repo.Payment.Status);
    }

    private static (PaymentRecoveryService Service, RecoveryRepository Recovery, PlayerGoodsPaymentTests.Repo Goods, Gateway Gateway) Setup()
    {
        var goodsRepo = new PlayerGoodsPaymentTests.Repo();
        goodsRepo.Order.BuyerId = null;
        goodsRepo.Order.Status = "PendingPayment";
        goodsRepo.Order.PaymentState = "NeedsReview";
        var payment = new Payment
        {
            Id = 4, GoodsOrderId = 3, Method = "payos", Status = "review_required",
            AmountVnd = 50000, GatewayPaymentLinkId = "link", ReferenceCode = "GOODS-4", GoodsOrder = goodsRepo.Order
        };
        var bank = new BankTransaction { Id = 1, MatchedPaymentId = 4, ExternalId = "payos:bank-1", AmountVnd = 50000, MatchStatus = "review_required" };
        payment.BankTransactions.Add(bank);
        goodsRepo.Order.Payments.Add(payment);
        goodsRepo.Events.Add(bank);
        var snapshot = new GoodsCheckoutSnapshot(DateTimeOffset.UtcNow.AddMinutes(15), 50000, [new GoodsLine(2, "Cầu", 2, 25000, 50000)]);
        goodsRepo.Audits.Add(new AuditLog { EntityType = "GoodsOrder", EntityId = 3, Action = "goods_checkout", NewValue = JsonSerializer.Serialize(snapshot) });
        var gateway = new Gateway();
        var repository = new RecoveryRepository(payment);
        var goods = new GoodsPaymentService(goodsRepo, gateway, TimeProvider.System);
        return (new PaymentRecoveryService(repository, gateway, null!, goods, null!), repository, goodsRepo, gateway);
    }

    private sealed class RecoveryRepository(Payment payment) : IPaymentRecoveryRepository
    {
        public Payment Payment = payment;
        public int FindCalls;
        public List<string> AuditActions = [];
        public Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentRecoveryItem>>([]);
        public Task<Payment?> FindAsync(long id, CancellationToken ct)
        {
            FindCalls++;
            return Task.FromResult<Payment?>(id == Payment.Id ? Payment : null);
        }
        public Task RecordAsync(long id, long actor, string action, string evidence, object result, CancellationToken ct)
        {
            AuditActions.Add(action);
            return Task.CompletedTask;
        }
        public Task CloseUnpaidAsync(long id, long actor, string evidence, GatewayStatus remote, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class Gateway : IPaymentGateway
    {
        public int Calls;
        public bool Fail;
        public GatewayStatus Remote = new(4, 50000, "PAID", "link", [new(4, 50000, "bank-1", "link", "VND", true, "{}")]);
        public void ValidateConfiguration() { }
        public Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt) => throw new NotSupportedException();
        public Task<GatewayStatus> GetAsync(long paymentId)
        {
            Calls++;
            return Fail ? Task.FromException<GatewayStatus>(new GatewayException("Gateway unavailable")) : Task.FromResult(Remote);
        }
        public Task<VerifiedBankEvent> VerifyAsync(string payload) => throw new NotSupportedException();
    }
}
