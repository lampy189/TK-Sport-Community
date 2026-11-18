using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public class IncidentWorkflowTests
{
    private static (IncidentWorkflowService Service, MemoryIncidentStore Store) Setup()
    {
        var r = new MemoryIncidentStore();
        return (new(r, TimeProvider.System), r);
    }

    [Theory]
    [InlineData("wait_bank")]
    [InlineData("wait_approval")]
    [InlineData("resolve")]
    public async Task CannotUpdateUnclaimedIssue(string action)
    {
        var(s, r) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, "inbox", 41, new(action, "A sufficiently long reason", 0)));
        Assert.Equal(0, r.Current.Version);
    }

    [Theory]
    [InlineData("wait_bank", "waiting_bank")]
    [InlineData("wait_approval", "waiting_approval")]
    [InlineData("resolve", "resolved")]
    public async Task AssignedHandlerCanRecordActualNextStep(string action, string expected)
    {
        var(s, r) = Setup();
        await s.Act(1, "inbox", 41, new("claim", "Taking responsibility for this case", 0));
        await s.Act(1, "inbox", 41, new(action, "Documented business result or next step", 1));
        Assert.Equal(expected, r.Current.Status);
        Assert.Equal(1, r.Current.AssignedTo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(99)]
    public async Task StaleVersionNeverOverwritesAnotherDecision(long version)
    {
        var(s, r) = Setup();
        await s.Act(1, "inbox", 41, new("claim", "Taking responsibility for this case", 0));
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, "inbox", 41, new("wait_bank", "Waiting for verified banking result", version)));
        Assert.Equal(1, r.Current.Version);
    }

    [Theory]
    [InlineData("reviews")]
    [InlineData("refunds")]
    [InlineData("sessions")]
    public async Task FinancialOrVenueIssueCannotCloseBeforeSourceIsResolved(string kind)
    {
        var(s, r) = Setup();
        r.CanResolve = false;
        await s.Act(1, kind, 41, new("claim", "Taking responsibility for this case", 0));
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, kind, 41, new("resolve", "Claiming completion while still pending", 1)));
        Assert.Equal("in_progress", r.Current.Status);
    }

    [Theory]
    [InlineData("inbox")]
    [InlineData("reviews")]
    [InlineData("refunds")]
    public async Task ForeignVenueCannotBeReadOrClaimed(string kind)
    {
        var(s, r) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => s.Detail(1, kind, 99));
        await Assert.ThrowsAsync<PaymentException>(() => s.Act(1, kind, 99, new("claim", "Attempting another venue's resources", 0)));
        Assert.Equal(0, r.Current.Version);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task StaffCannotApproveOperationalException(int actor)
    {
        var(s, r) = Setup();
        await s.Request(1, new("inbox", 41, "other", "A business exception requiring approval"));
        await Assert.ThrowsAsync<PaymentException>(() => s.Decide(actor, 100, new(true, "Approving without appropriate authority")));
        Assert.Empty(r.Approvals);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IndependentManagerDecisionIsRecordedWithoutForcingTheResource(bool approve)
    {
        var(s, r) = Setup();
        await s.Request(1, new("refunds", 41, "refund_funds", "Bank transfer is delayed; need review"));
        await s.Decide(3, 100, new(approve, "Independent review with supporting evidence"));
        Assert.Equal(approve ? "approved" : "rejected", r.CasesData[100].Status);
        Assert.Single(r.Approvals);
    }

    [Fact]
    public async Task ManagerRequestRequiresOwnerAndCannotSelfApprove()
    {
        var(s, r) = Setup();
        await s.Request(3, new("inbox", 41, "other", "Manager needs a higher-level business decision"));
        await Assert.ThrowsAsync<PaymentException>(() => s.Decide(3, 100, new(true, "Trying to approve own request")));
        await s.Decide(4, 100, new(true, "Owner independently approves the proposed plan"));
        Assert.Equal(4, r.CasesData[100].DecidedBy);
    }

    [Theory]
    [InlineData("payment_mismatch")]
    [InlineData("funding")]
    [InlineData("refund_funds")]
    public async Task GenericFinancialClosureRequiresIndependentManagerOrOwner(string category)
    {
        var(s, r) = Setup();
        await s.Request(1, new("venues", 0, category, "Unallocated financial exception for this venue"));
        await s.Decide(3, 100, new(true, "Manager approves the documented handling plan"));
        await Assert.ThrowsAsync<PaymentException>(() => s.Close(1, 100, "I claim the money is fully settled"));
        await s.Close(3, 100, "Independently verified the external evidence");
        Assert.Equal("closed", r.CasesData[100].Status);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(1)]
    [InlineData(100)]
    public async Task StockAdjustmentRequiresApprovalExecutesExactlyOnceAndThenCloses(int delta)
    {
        var(s, r) = Setup();
        await s.Request(1, new("venues", 0, "stock_adjustment", "Counted damaged inventory with attached evidence", 41, delta));
        await Assert.ThrowsAsync<PaymentException>(() => s.Execute(1, 100, "Attempt before independent approval"));
        await s.Decide(3, 100, new(true, "Verified counted inventory independently"));
        await Assert.ThrowsAsync<PaymentException>(() => s.Close(3, 100, "Cannot close before stock execution"));
        await s.Execute(1, 100, "Applied exactly the approved stock correction");
        await s.Execute(1, 100, "Retry after completed stock correction");
        Assert.Equal(1, r.StockExecutions);
        await s.Close(3, 100, "Manager verifies stock movement and closes case");
        Assert.Equal("closed", r.CasesData[100].Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100001)]
    [InlineData(100001)]
    public async Task StockAdjustmentRejectsInvalidQuantitiesBeforeApproval(int delta)
    {
        var(s, r) = Setup();
        await Assert.ThrowsAsync<PaymentException>(() => s.Request(1, new("venues", 0, "stock_adjustment", "Evidence for an invalid stock adjustment", 41, delta)));
        Assert.Empty(r.CasesData);
    }

    public class MemoryIncidentStore : IIncidentRepository
    {
        public IncidentState Current = new();
        public bool CanResolve = true;
        public Dictionary<long, OperationalCase> CasesData = [];
        public List<Approval> Approvals = [];
        public int StockExecutions;
        public Task<T> Transaction<T>(Func<Task<T>> action) => action();
        public Task<IncidentActor> Actor(long id) => Task.FromResult(id switch
        {
            1 => new IncidentActor(1, "Staff one", "staff", 1),
            2 => new(2, "Staff two", "staff", 1),
            3 => new(3, "Manager", "manager", 1),
            4 => new(4, "Owner", "owner", null),
            5 => new(5, "Foreign staff", "staff", 2),
            _ => throw new PaymentException(403, "Unknown actor")});
        public Task<IReadOnlyList<IncidentActor>> People(long venue) => Task.FromResult<IReadOnlyList<IncidentActor>>([]);
        public Task<IncidentResource?> Resource(string kind, long id) => Task.FromResult<IncidentResource?>(new(kind, id, id == 99 ? 2 : 1, "Fixture", "pending", CanResolve));
        public Task<IncidentState> State(string kind, long id) => Task.FromResult(Current);
        public Task<IncidentState> SaveState(string kind, long id, long actor, IncidentState state)
        {
            Current = state with
            {
                Version = Current.Version + 1
            };
            return Task.FromResult(Current);
        }

        public Task<IReadOnlyList<(string Kind, long Id)>> Keys() => Task.FromResult<IReadOnlyList<(string, long)>>([]);
        public Task<IReadOnlyList<AuditLog>> History(string type, long id) => Task.FromResult<IReadOnlyList<AuditLog>>([]);
        public Task<OperationalCase> CreateCase(OperationalCase item)
        {
            item = item with
            {
                Id = 100
            };
            CasesData[item.Id] = item;
            return Task.FromResult(item);
        }

        public Task<OperationalCase?> Case(long id) => Task.FromResult(CasesData.GetValueOrDefault(id));
        public Task<IReadOnlyList<OperationalCase>> Cases() => Task.FromResult<IReadOnlyList<OperationalCase>>(CasesData.Values.ToList());
        public Task<OperationalCase> SaveCase(OperationalCase item, long actor, string action)
        {
            CasesData[item.Id] = item;
            return Task.FromResult(item);
        }

        public virtual Task RecordReturnPayout(long id, long actor, RefundPayoutInput input) => Task.CompletedTask;
        public virtual Task<RefundPayoutRecord?> ReturnPayout(long id) => Task.FromResult<RefundPayoutRecord?>(null);
        public virtual Task CompleteReturn(OperationalCase item, long actor) => Task.CompletedTask;
        public Task ExecuteStock(OperationalCase item, long actor)
        {
            StockExecutions++;
            return Task.CompletedTask;
        }

        public Task AddApproval(Approval approval)
        {
            Approvals.Add(approval);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PaymentRecoveryItem>> RecoveryPayments() => Task.FromResult<IReadOnlyList<PaymentRecoveryItem>>([]);
        public virtual Task<Payment?> RecoveryPayment(long id) => Task.FromResult<Payment?>(null);
        public virtual Task RecordRecovery(long id, long actor, string action, string evidence, string result) => Task.CompletedTask;
        public virtual Task CloseUnpaid(long id) => Task.CompletedTask;
    }
}
