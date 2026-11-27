using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace TKSC.Api.Tests.InventoryWorkflows;

public sealed class SessionSupplyWorkflowTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 100001)]
    public async Task InvalidRequestDoesNotReachStore(long productId, int quantity)
    {
        var store = new Store();
        var result = await Service(store).CreateRequestAsync(5, new(productId, quantity), 9, default);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ValidRequestForwardsRefereeSessionAndUnit()
    {
        var store = new Store();
        var request = new CreateSupplyRequest(1, 12, "Tube", "Evening");
        using var cts = new CancellationTokenSource();
        var result = await Service(store).CreateRequestAsync(5, request, 9, cts.Token);
        Assert.Same(store.Result, result);
        Assert.Equal((5, 0, 9), (store.SessionId, store.OtherId, store.ActorId));
        Assert.Same(request, store.Input);
        Assert.Equal(cts.Token, store.Token);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(100001, null)]
    [InlineData(null, "Tube")]
    public async Task InvalidIssueFromRequestDoesNotReachStore(int? quantity, string? unit)
    {
        var store = new Store();
        var result = await Service(store).IssueRequestAsync(5, 7, new(quantity, unit), 22, default);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task IssueRequestForwardsOptionalQuantityAndStoreConflict()
    {
        var store = new Store { Result = SupplyResult.Fail("INSUFFICIENT_STOCK", "not enough") };
        var request = new IssueSupplyFromRequest(6, "Tube");
        var result = await Service(store).IssueRequestAsync(5, 7, request, 22, default);
        Assert.Same(store.Result, result);
        Assert.Equal((5, 7, 22), (store.SessionId, store.OtherId, store.ActorId));
        Assert.Same(request, store.Input);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RejectRequiresReasonBeforeStore(string? note)
    {
        var store = new Store();
        var result = await Service(store).RejectRequestAsync(5, 7, new(note), 22, default);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task RejectTrimsReasonAndForwardsIdentity()
    {
        var store = new Store();
        var result = await Service(store).RejectRequestAsync(5, 7, new("  Out of stock  "), 22, default);
        Assert.Same(store.Result, result);
        Assert.Equal((5, 7, 22), (store.SessionId, store.OtherId, store.ActorId));
        Assert.Equal("Out of stock", store.Input);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(100001, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 100001)]
    public async Task InvalidUsageDoesNotReachStore(int used, int returned)
    {
        var store = new Store();
        var result = await Service(store).ReportUseAsync(5, 1, new(used, returned), 9, default);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task UsageForwardsOptionalReturnAndRepositoryLimitError()
    {
        var store = new Store { Result = SupplyResult.Fail("USE_EXCEEDS_ISSUED", "exceeds") };
        var request = new ReportSupplyUseRequest(9, null);
        var result = await Service(store).ReportUseAsync(5, 1, request, 9, default);
        Assert.Same(store.Result, result);
        Assert.Equal((5, 1, 9), (store.SessionId, store.OtherId, store.ActorId));
        Assert.Same(request, store.Input);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100001)]
    public async Task InvalidReturnDoesNotReachStore(int quantity)
    {
        var store = new Store();
        var result = await Service(store).ReconcileAsync(5, 1, new(quantity), 22, default);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task ReconcileForwardsPhysicalReturnAndStoreResult(int quantity)
    {
        var store = new Store { Result = SupplyResult.Fail("RETURN_EXCEEDS_ISSUED", "exceeds") };
        var request = new ReturnSupplyRequest(quantity);
        var result = await Service(store).ReconcileAsync(5, 1, request, 22, default);
        Assert.Same(store.Result, result);
        Assert.Equal((5, 1, 22), (store.SessionId, store.OtherId, store.ActorId));
        Assert.Same(request, store.Input);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData("issue")]
    [InlineData("reject")]
    [InlineData("reconcile")]
    public async Task StaffMutationsRejectInvalidCsrfBeforeStore(string action)
    {
        var store = new Store();
        var controller = Controller(store, false);
        var response = await Invoke(controller, action);
        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData("issue")]
    [InlineData("reject")]
    [InlineData("reconcile")]
    public async Task StaffMutationsReturnStoreDetailsOnSuccess(string action)
    {
        var details = new SessionSupplyDetails(new(5, new(2026, 11, 26), "18:00", "Badminton", "Court", "Open", 0, 0), [], []);
        var store = new Store { Result = SupplyResult.Ok(details) };
        var response = Assert.IsType<OkObjectResult>(await Invoke(Controller(store, true), action));
        Assert.Same(details, response.Value);
        Assert.Equal(22, store.ActorId);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData("REQUEST_NOT_FOUND", 404)]
    [InlineData("VALIDATION_FAILED", 400)]
    [InlineData("INSUFFICIENT_STOCK", 409)]
    [InlineData("REQUEST_HANDLED", 409)]
    public async Task IssueControllerMapsStoreFailureToHttp(string code, int status)
    {
        var store = new Store { Result = SupplyResult.Fail(code, "failure") };
        var response = Assert.IsAssignableFrom<ObjectResult>(await Controller(store, true).IssueRequest(5, 7, new(), default));
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(1, store.Calls);
        Assert.Equal((5, 7, 22), (store.SessionId, store.OtherId, store.ActorId));
    }

    private static SessionSupplyService Service(Store store) => new(store, TimeProvider.System);
    private static SessionSuppliesController Controller(Store store, bool csrf) => new(Service(store), new InventoryTestContext.Antiforgery(csrf))
    {
        ControllerContext = InventoryTestContext.ForActor()
    };
    private static Task<IActionResult> Invoke(SessionSuppliesController controller, string action) => action switch
    {
        "issue" => controller.IssueRequest(5, 7, new(), default),
        "reject" => controller.RejectRequest(5, 7, new("reason"), default),
        "reconcile" => controller.Reconcile(5, 1, new(3), default),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private sealed class Store : ISessionSupplyStore
    {
        public int Calls;
        public long SessionId, OtherId, ActorId;
        public object? Input;
        public CancellationToken Token;
        public SupplyResult Result = SupplyResult.Fail("TEST_RESULT", "test");
        private Task<SupplyResult> Record(long session, long other, long actor, object? input, CancellationToken ct)
        {
            Calls++; SessionId = session; OtherId = other; ActorId = actor; Input = input; Token = ct;
            return Task.FromResult(Result);
        }
        public Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken ct) => Record(sessionId, 0, refereeId, request, ct);
        public Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken ct) => Record(sessionId, requestId, staffUserId, request, ct);
        public Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, string note, long staffUserId, CancellationToken ct) => Record(sessionId, requestId, staffUserId, note, ct);
        public Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken ct) => Record(sessionId, productId, refereeId, request, ct);
        public Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken ct) => Record(sessionId, productId, staffUserId, request, ct);
        public Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken ct) => throw new NotSupportedException();
        public Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, long? refereeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken ct) => throw new NotSupportedException();
    }
}
