using TKSC.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public class UnallocatedPaymentReturnTests
{
    private static (IncidentWorkflowService Service, Store Store, TicketRecoveryTests.Gateway Gateway) Setup()
    {
        var r = new Store();
        var g = new TicketRecoveryTests.Gateway();
        return (new(r, TimeProvider.System, g), r, g);
    }

    private static OperationalRequest Request => new("reviews", 41, "return_unallocated", "Cannot activate tickets; return verified unallocated transfer", BankTransactionId: 11);
    private static RefundPayoutInput Payout => new(100, 99, "OUT-RETURN-01", "Bank statement confirms the actual outgoing transfer", DateTimeOffset.UtcNow.AddMinutes(-1));

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    public async Task FullUnallocatedTransferCanBeReturnedWithoutActivatingTickets(int amount)
    {
        var(s, r, g) = Setup();
        r.Payment.BankTransactions.First().AmountVnd = amount;
        g.Remote = g.Remote with
        {
            AmountPaid = amount,
            Transactions = [g.Remote.Transactions![0] with
            {
                Amount = amount
            }

            ]
        };
        var proposal = (OperationalCase)await s.Request(1, Request);
        Assert.Equal(amount, proposal.AmountVnd);
        await s.Decide(3, 100, new(true, "Independent decision to return full unallocated funds"));
        var payout = Payout with
        {
            AmountVnd = amount
        };
        await s.RecordReturn(1, 100, payout);
        await s.RecordReturn(1, 100, payout);
        await s.VerifyReturn(3, 100, "Manager independently verifies the actual outgoing transfer");
        await s.VerifyReturn(3, 100, "Retry after successful independent verification");
        Assert.Equal(1, r.Returns);
        Assert.Equal("executed", r.CasesData[100].Status);
        Assert.Equal("review_required", r.Payment.Status);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("FAILED")]
    [InlineData("CANCELLED")]
    public async Task UnverifiedIncomingMoneyCannotCreateReturnRequest(string status)
    {
        var(s, r, g) = Setup();
        g.Remote = g.Remote with
        {
            Status = status
        };
        await Assert.ThrowsAsync<PaymentException>(() => s.Request(1, Request));
        Assert.Empty(r.CasesData);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task StaffCannotIndependentlyVerifyReturn(int verifier)
    {
        var(s, r, g) = Setup();
        await s.Request(1, Request);
        await s.Decide(3, 100, new(true, "Independent decision based on bank proof"));
        await s.RecordReturn(1, 100, Payout);
        await Assert.ThrowsAsync<PaymentException>(() => s.VerifyReturn(verifier, 100, "Trying to mark money returned without proper authority"));
        Assert.Equal(0, r.Returns);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(110)]
    [InlineData(0)]
    public async Task PayoutCannotChangeTheApprovedAmount(int amount)
    {
        var(s, r, g) = Setup();
        await s.Request(1, Request);
        await s.Decide(3, 100, new(true, "Independent decision based on bank proof"));
        await Assert.ThrowsAsync<PaymentException>(() => s.RecordReturn(1, 100, Payout with { AmountVnd = amount }));
        Assert.Null(r.Payout);
    }

    [Fact]
    public async Task MatchedTransferProtectingAnActiveOrderCannotBeReturnedAsUnallocated()
    {
        var(s, r, g) = Setup();
        r.Payment.Order!.Status = "paid";
        await Assert.ThrowsAsync<PaymentException>(() => s.Request(1, Request));
        Assert.Empty(r.CasesData);
    }

    [Fact]
    public async Task TransferRecorderCannotVerifyTheirOwnOutgoingEvidence()
    {
        var(s, r, g) = Setup();
        await s.Request(1, Request);
        await s.Decide(3, 100, new(true, "Independent decision based on bank proof"));
        await s.RecordReturn(3, 100, Payout);
        await Assert.ThrowsAsync<PaymentException>(() => s.VerifyReturn(3, 100, "Cannot verify transfer I recorded myself"));
        Assert.Equal(0, r.Returns);
    }

    public class Store : IncidentWorkflowTests.MemoryIncidentStore
    {
        public Payment Payment = new()
        {
            Id = 41,
            OrderId = 7,
            PayerId = 99,
            Status = "review_required",
            Method = "payos",
            GatewayPaymentLinkId = "LINK",
            AmountVnd = 100,
            Order = new()
            {
                Id = 7,
                PayerId = 99,
                TotalAmount = 100,
                Status = "review_required"
            },
            BankTransactions = [new()
            {
                Id = 11,
                ExternalId = "payos:BANK",
                AmountVnd = 100,
                MatchStatus = "review_required"
            }

            ]
        };
        public RefundPayoutRecord? Payout;
        public int Returns;
        public override Task<Payment?> RecoveryPayment(long id) => Task.FromResult<Payment?>(Payment);
        public override Task RecordReturnPayout(long id, long actor, RefundPayoutInput input)
        {
            Payout ??= new(input.AmountVnd, input.RecipientId, input.BankReference, input.Document, input.PaidAt, actor);
            return Task.CompletedTask;
        }

        public override Task<RefundPayoutRecord?> ReturnPayout(long id) => Task.FromResult(Payout);
        public override Task CompleteReturn(OperationalCase item, long actor)
        {
            Returns++;
            return Task.CompletedTask;
        }
    }
}
