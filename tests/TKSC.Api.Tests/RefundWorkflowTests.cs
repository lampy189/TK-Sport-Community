using System.Reflection;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;

public class RefundWorkflowTests
{
    private static (TicketAftercareService Service, RefundRepositoryStub Store) Setup()
    {
        var repository = DispatchProxy.Create<ITicketAftercareRepository, RefundRepositoryStub>();
        return (new TicketAftercareService(repository, TimeProvider.System), (RefundRepositoryStub)(object)repository);
    }
    private static RefundDecisionInput Decision => new(true, 100, 7, "Duyệt hoàn đúng khoản đã trả");
    private static RefundPayoutInput Payout => new(100, 7, "OUT-001", "Chứng từ ngân hàng tiền ra 001", DateTimeOffset.UtcNow.AddMinutes(-5));

    [Theory]
    [InlineData("Staff")]
    [InlineData("SystemAdmin")]
    [InlineData("Referee")]
    public async Task BusinessDecisionRequiresManagerOrOwner(string role)
    {
        var (service, store) = Setup(); store.Role = role;
        await Assert.ThrowsAsync<PaymentException>(() => service.DecideRefundAsync(1, 20, Decision));
        Assert.Empty(store.Logs);
    }
    [Fact]
    public async Task LinkedPersonCannotApprove()
    {
        var (service, store) = Setup(); store.LinkedPerson = true;
        await Assert.ThrowsAsync<PaymentException>(() => service.DecideRefundAsync(1, 20, Decision));
        Assert.Equal("requested", store.Refund.Status);
    }
    [Fact]
    public async Task CompleteNeedsApprovalAndSeparateVerifiedPayout()
    {
        var (service, store) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => service.CompleteRefundAsync(1, 22, "Đã kiểm tra tiền ra", "OUT-001"));
        await service.DecideRefundAsync(1, 20, Decision);
        await Assert.ThrowsAsync<PaymentException>(() => service.CompleteRefundAsync(1, 22, "Đã kiểm tra tiền ra", "OUT-001"));
        var payout = Payout;
        await service.RecordRefundPayoutAsync(1, 21, payout);
        await service.RecordRefundPayoutAsync(1, 21, payout);
        await Assert.ThrowsAsync<PaymentException>(() => service.CompleteRefundAsync(1, 21, "Đã kiểm tra tiền ra", "OUT-001"));
        await service.CompleteRefundAsync(1, 22, "Đã kiểm tra tiền ra", "OUT-001");
        await service.CompleteRefundAsync(1, 22, "Đã kiểm tra tiền ra", "OUT-001");
        Assert.Equal("completed", store.Refund.Status);
        Assert.Equal(20, store.Refund.ApprovedBy);
        Assert.Single(store.Logs, a => a.Action == "refund_completed");
        Assert.Single(store.Logs, a => a.Action == "refund_payout");
    }
    [Fact]
    public async Task ChangedApprovedValuesAndWrongRecipientAreBlocked()
    {
        var (service, store) = Setup();
        await service.DecideRefundAsync(1, 20, Decision);
        await Assert.ThrowsAsync<PaymentException>(() => service.RecordRefundPayoutAsync(1, 21, Payout with { RecipientId = 8 }));
        store.Refund.AmountVnd = 99;
        await Assert.ThrowsAsync<PaymentException>(() => service.RecordRefundPayoutAsync(1, 21, Payout with { AmountVnd = 99 }));
        Assert.DoesNotContain(store.Logs, a => a.Action == "refund_payout");
    }
    [Theory]
    [InlineData("payment")]
    [InlineData("ticket")]
    [InlineData("allocation")]
    [InlineData("payer")]
    public async Task RefundCannotExceedAnyPaidBoundary(string boundary)
    {
        var (service, store) = Setup();
        if (boundary == "payment") store.Payment.AmountVnd = 99;
        if (boundary == "ticket") store.Ticket.AmountVnd = 99;
        if (boundary == "allocation") store.Allocation = 99;
        if (boundary == "payer") store.Payment.PayerId = 8;
        await Assert.ThrowsAsync<PaymentException>(() => service.DecideRefundAsync(1, 20, Decision));
        Assert.Empty(store.Logs);
    }
    [Fact]
    public async Task FailedAttemptUsesSameRefundAndKeepsDecision()
    {
        var (service, store) = Setup();
        await service.DecideRefundAsync(1, 20, Decision);
        await service.FailRefundAsync(1, 21, "Ngân hàng chưa xử lý giao dịch");
        Assert.Equal("failed", store.Refund.Status);
        await service.RecordRefundPayoutAsync(1, 21, Payout);
        await service.CompleteRefundAsync(1, 22, "Đã kiểm tra tiền ra", "OUT-001");
        Assert.Equal("completed", store.Refund.Status);
        Assert.Single(store.Logs, a => a.Action == "refund_decision");
    }
    [Fact]
    public async Task IncomingOrReusedBankReferenceCannotProvePayout()
    {
        var (service, store) = Setup();
        await service.DecideRefundAsync(1, 20, Decision);
        store.ReferenceUsed = true;
        await Assert.ThrowsAsync<PaymentException>(() => service.RecordRefundPayoutAsync(1, 21, Payout));
        Assert.DoesNotContain(store.Logs, a => a.Action == "refund_payout");
    }
}

public class RefundRepositoryStub : DispatchProxy
{
    public string Role = "Manager";
    public bool LinkedPerson, ReferenceUsed;
    public long Allocation = 100;
    public Refund Refund = new() { Id = 1, TicketId = 2, PaymentId = 3, AmountVnd = 100, RecipientId = 7, InitiatedBy = 9, Status = "requested" };
    public Payment Payment = new() { Id = 3, OrderId = 4, AmountVnd = 100, PayerId = 7, Status = "paid" };
    public Ticket Ticket = new() { Id = 2, OrderId = 4, SessionId = 5, AmountVnd = 100, Order = new() { PayerId = 7 }, Player = new() { UserId = 8 } };
    public List<AuditLog> Logs = [];
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        var a = args!;
        switch (method!.Name)
        {
            case "Transaction": return ((Func<Task<object>>)a[0]!)();
            case "RefundActorAsync": return Task.FromResult<User?>(new User { Role = new Role { Code = Role } });
            case "SameRefundPersonAsync": return Task.FromResult(LinkedPerson || (long)a[0]! == (long)a[1]!);
            case "CompleteRefundRefundsAsync": return Task.FromResult<Refund?>(Refund);
            case "GetTrackedRefundForSettlementAsync": return Task.FromResult(Refund);
            case "TicketTicketsAsync": return Task.FromResult<long?>(4);
            case "GetTicketDetailsAsync": return Task.FromResult(Ticket);
            case "SessionSessionsAsync": case "GetSessionDetailsAsync": return Task.FromResult<Session?>(new Session { Id = 5 });
            case "LockOrder": case "LockRefundPaymentAsync": return Task.CompletedTask;
            case "CompleteRefundPaymentsAsync": return Task.FromResult(Payment);
            case "RefundAllocationAsync": return Task.FromResult(Allocation);
            case "RefundAllocationReservedAsync": case "RefundTicketTotalAsync": return Task.FromResult((long)Refund.AmountVnd!.Value);
            case "CompleteRefundRefundsTotalAsync": return Task.FromResult<long?>(Refund.AmountVnd);
            case "RefundAuditAsync": return Task.FromResult(Logs.LastOrDefault(l => l.Action == (string)a[1]!));
            case "CompleteRefundAuditLogsExistsAsync": case "CompleteChangeBankTransactionsExistsAsync": case "CompleteChangePaymentsExistsAsync": return Task.FromResult(ReferenceUsed);
            case "AddAuditLogs": Logs.Add((AuditLog)a[0]!); return null;
            case "AddRefundApproval": case "AddNotifications": return null;
            default: throw new NotSupportedException(method.Name);
        }
    }
}
