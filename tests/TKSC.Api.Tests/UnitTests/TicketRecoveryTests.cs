using System.Reflection;
using be_da.Application.Interfaces.Services;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public class TicketRecoveryTests
{
    private static (TicketRecoveryService Service, RecoveryStore Store, Gateway Gateway, Counter Approvals, Counter Rechecks) Setup()
    {
        var r = new RecoveryStore
        {
            Current = new("in_progress", 1, "Staff", Version: 1)
        };
        r.Current = new(Status: "in_progress", AssignedTo: 1);
        var g = new Gateway();
        var a = DispatchProxy.Create<ITicketAftercareService, Counter>();
        var p = DispatchProxy.Create<IPaymentService, Counter>();
        return (new(r, g, p, a), r, g, (Counter)(object)a, (Counter)(object)p);
    }

    private static PaymentRecoveryInput Input => new("Bank evidence checked independently", 11);

    [Theory]
    [InlineData(90)]
    [InlineData(110)]
    [InlineData(0)]
    public async Task AmountMismatchNeverActivatesTickets(int paid)
    {
        var(s, r, g, a, p) = Setup();
        g.Remote = g.Remote with
        {
            AmountPaid = paid
        };
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, 41, "approve", Input));
        Assert.Equal(0, a.Calls);
        Assert.Contains("payment_recovery_failed", r.Logs);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("")]
    public async Task NonVndProofNeverActivatesTickets(string currency)
    {
        var(s, r, g, a, p) = Setup();
        g.Remote = g.Remote with
        {
            Transactions = [g.Remote.Transactions![0] with
            {
                Currency = currency
            }

            ]
        };
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, 41, "approve", Input));
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public async Task ExactlyMatchedBankProofUsesExistingBusinessActivation()
    {
        var(s, r, g, a, p) = Setup();
        await s.Act(1, 41, "approve", Input);
        Assert.Equal(1, a.Calls);
        Assert.Contains("payment_recovery_completed", r.Logs);
    }

    [Theory]
    [InlineData("PAID")]
    [InlineData("PENDING")]
    [InlineData("PROCESSING")]
    public async Task UncertainBankStateCannotBeClosedAsUnpaid(string state)
    {
        var(s, r, g, a, p) = Setup();
        g.Remote = new(41, 0, state, "LINK", []);
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, 41, "close-unpaid", Input));
        Assert.False(r.Closed);
    }

    [Theory]
    [InlineData("CANCELLED")]
    [InlineData("EXPIRED")]
    [InlineData("FAILED")]
    public async Task VerifiedNoMoneyTerminalStateCanClose(string state)
    {
        var(s, r, g, a, p) = Setup();
        g.Remote = new(41, 0, state, "LINK", []);
        r.Payment.BankTransactions.Clear();
        await s.Act(1, 41, "close-unpaid", Input);
        Assert.True(r.Closed);
    }

    [Theory]
    [InlineData("force-paid")]
    [InlineData("force-refund")]
    [InlineData("override")]
    public async Task InvalidActionsNeverContactTheGateway(string action)
    {
        var(s, r, g, a, p) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, 41, action, Input));
        Assert.Equal(0, g.Calls);
        Assert.Empty(r.Logs);
    }

    [Fact]
    public async Task PayerCannotApproveOwnPayment()
    {
        var(s, r, g, a, p) = Setup();
        r.Payment.PayerId = 1;
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, 41, "approve", Input));
        Assert.Equal(0, g.Calls);
    }

    [Fact]
    public async Task WrongCenterCannotProcessPayment()
    {
        var(s, r, g, a, p) = Setup();
        var e = await Assert.ThrowsAsync<PaymentException>(() => s.Act(5, 41, "approve", Input));
        Assert.Equal(404, e.StatusCode);
        Assert.Equal(0, g.Calls);
    }

    [Fact]
    public async Task ProviderOutageRecordsWaitingEvidenceWithoutForcingPaid()
    {
        var(s, r, g, a, p) = Setup();
        g.Fail = true;
        await Assert.ThrowsAsync<GatewayException>(() => s.Act(1, 41, "approve", Input));
        Assert.Equal("review_required", r.Payment.Status);
        Assert.Contains("payment_recovery_failed", r.Logs);
        Assert.Equal(0, a.Calls);
    }

    public class Counter : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? m, object? []? args)
        {
            Calls++;
            return m!.ReturnType == typeof(Task<object>) ? Task.FromResult<object>(new { status = "paid" }) : Task.CompletedTask;
        }
    }

    public class Gateway : IPaymentGateway
    {
        public int Calls;
        public bool Fail;
        public GatewayStatus Remote = new(41, 100, "PAID", "LINK", [new(41, 100, "BANK", "LINK", "VND", true, "fixture")]);
        public void ValidateConfiguration()
        {
        }

        public Task<GatewayCheckout> CreateAsync(long p, long o, int amount, DateTimeOffset expires) => throw new NotSupportedException();
        public Task<GatewayStatus> GetAsync(long id)
        {
            Calls++;
            if (Fail)
                throw new GatewayException("Provider unavailable");
            return Task.FromResult(Remote);
        }

        public Task<VerifiedBankEvent> VerifyAsync(string payload) => throw new NotSupportedException();
    }

    public class RecoveryStore : IncidentWorkflowTests.MemoryIncidentStore
    {
        public Payment Payment = new()
        {
            Id = 41,
            PayerId = 99,
            OrderId = 7,
            Order = new()
            {
                Id = 7,
                PayerId = 99
            },
            AmountVnd = 100,
            Method = "payos",
            Status = "review_required",
            GatewayPaymentLinkId = "LINK",
            BankTransactions = [new()
            {
                Id = 11,
                AmountVnd = 100,
                ExternalId = "payos:BANK"
            }

            ]
        };
        public bool Closed;
        public List<string> Logs = [];
        public override Task<Payment?> RecoveryPayment(long id) => Task.FromResult<Payment?>(Payment);
        public override Task RecordRecovery(long id, long actor, string action, string evidence, string result)
        {
            Logs.Add(action);
            return Task.CompletedTask;
        }

        public override Task CloseUnpaid(long id)
        {
            Closed = true;
            return Task.CompletedTask;
        }
    }
}
