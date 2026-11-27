using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace TKSC.Api.Tests.InventoryWorkflows;

public sealed class StockCountWorkflowTests
{
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 100001, 1)]
    [InlineData(1, 1, -1)]
    public async Task InvalidCountDoesNotReachStore(long product, int quantity, long version)
    {
        var store = new Store();
        var result = await Service(store).CreateAsync(new(product, quantity, version), 22, default);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ValidCountForwardsVersionActorAndStoreConflict()
    {
        var store = new Store { Result = StockCountResult.Fail("STOCK_CHANGED", "recount") };
        var request = new CreateStockCountRequest(1, 9, 17, "Box", "Difference", "Photo");
        using var cts = new CancellationTokenSource();
        var result = await Service(store).CreateAsync(request, 22, cts.Token);
        Assert.Same(store.Result, result);
        Assert.Same(request, store.Request);
        Assert.Equal(22, store.Actor);
        Assert.Equal(cts.Token, store.Token);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task ApprovalRequiresExpectedDifferenceBeforeStore()
    {
        var store = new Store();
        var result = await Service(store).ApproveAsync(7, 33, new("ok"), default);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ApprovalForwardsExpectedDifferenceAndStoreConflict()
    {
        var store = new Store { Result = StockCountResult.Fail("ADJUSTMENT_BELOW_RESERVED", "held") };
        using var cts = new CancellationTokenSource();
        var result = await Service(store).ApproveAsync(7, 33, new("checked", -3), cts.Token);
        Assert.Same(store.Result, result);
        Assert.Equal((7, 33, true, -3), (store.Id, store.Actor, store.Approve, store.Expected));
        Assert.Equal("checked", store.Note);
        Assert.Equal(cts.Token, store.Token);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RejectionRequiresReasonBeforeStore(string? note)
    {
        var store = new Store();
        var result = await Service(store).RejectAsync(7, 33, new(note), default);
        Assert.Equal("VALIDATION_FAILED", result.ErrorCode);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task RejectionForwardsDecisionAndNoExpectedDifference()
    {
        var store = new Store();
        var result = await Service(store).RejectAsync(7, 33, new("recount"), default);
        Assert.Same(store.Result, result);
        Assert.Equal((7, 33, false, (int?)null), (store.Id, store.Actor, store.Approve, store.Expected));
        Assert.Equal("recount", store.Note);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task CountSheetForwardsProductAndCancellation()
    {
        var store = new Store();
        using var cts = new CancellationTokenSource();
        var result = await Service(store).GetSheetAsync(1, cts.Token);
        Assert.Same(store.Sheet, result);
        Assert.Equal(1, store.Id);
        Assert.Equal(cts.Token, store.Token);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task CountMutationsRejectInvalidCsrfBeforeStore(string action)
    {
        var store = new Store();
        var response = await Invoke(Controller(store, false), action);
        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task CreateControllerReturnsCreatedCount()
    {
        var count = new StockCountView(7, 1, "SKU", "Water", 10, 9, -1, null, 9, "Pending", "missing", null, 22, "Staff", null, null, null, null, null, null, "Piece", 10, 17);
        var store = new Store { Result = StockCountResult.Ok(count) };
        var response = Assert.IsType<CreatedResult>(await Controller(store, true).Create(new(1, 9, 17, Reason: "missing"), default));
        Assert.Equal("/api/stock/counts/7", response.Location);
        Assert.Same(count, response.Value);
        Assert.Equal(22, store.Actor);
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    public async Task DecisionControllerReturnsStoredCountOnSuccess(string action)
    {
        var count = new StockCountView(7, 1, "SKU", "Water", 10, 9, -1, null, 9, action == "approve" ? "Applied" : "Rejected", "missing", null, 22, "Staff", null, 33, "Manager", null, null, null, "Piece", 9, 17);
        var store = new Store { Result = StockCountResult.Ok(count) };
        var response = Assert.IsType<OkObjectResult>(await Invoke(Controller(store, true), action));
        Assert.Same(count, response.Value);
        Assert.Equal(action == "approve", store.Approve);
        Assert.Equal(1, store.Calls);
    }

    [Theory]
    [InlineData("SELF_APPROVAL", 403)]
    [InlineData("COUNT_NOT_FOUND", 404)]
    [InlineData("COUNT_CHANGED", 409)]
    [InlineData("ADJUSTMENT_BELOW_RESERVED", 409)]
    public async Task ApprovalControllerMapsStoreFailure(string code, int status)
    {
        var store = new Store { Result = StockCountResult.Fail(code, "failure") };
        var response = Assert.IsAssignableFrom<ObjectResult>(await Controller(store, true).Approve(7, new("checked", -1), default));
        Assert.Equal(status, response.StatusCode);
        Assert.Equal((7, 22, true, -1), (store.Id, store.Actor, store.Approve, store.Expected));
        Assert.Equal(1, store.Calls);
    }

    private static StockCountService Service(Store store) => new(store, TimeProvider.System);
    private static StockCountsController Controller(Store store, bool csrf) => new(Service(store), new InventoryTestContext.Antiforgery(csrf))
    {
        ControllerContext = InventoryTestContext.ForActor()
    };
    private static Task<IActionResult> Invoke(StockCountsController controller, string action) => action switch
    {
        "create" => controller.Create(new(1, 9, 17), default),
        "approve" => controller.Approve(7, new("ok", -1), default),
        "reject" => controller.Reject(7, new("recount"), default),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    private sealed class Store : IStockCountStore
    {
        public int Calls;
        public long Id, Actor;
        public bool Approve;
        public int? Expected;
        public string? Note;
        public CancellationToken Token;
        public CreateStockCountRequest? Request;
        public StockCountResult Result = StockCountResult.Fail("TEST_RESULT", "test");
        public StockCountSheet Sheet = new(1, "SKU", "Water", 10, 2, 17, "Piece", []);
        public Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken ct)
        {
            Calls++; Id = productId; Token = ct;
            return Task.FromResult<StockCountSheet?>(Sheet);
        }
        public Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken ct)
        {
            Calls++; Request = request; Actor = staffUserId; Token = ct;
            return Task.FromResult(Result);
        }
        public Task<StockCountResult> DecideAsync(long id, long deciderId, bool approve, string? note, int? expectedDifference, CancellationToken ct)
        {
            Calls++; Id = id; Actor = deciderId; Approve = approve; Note = note; Expected = expectedDifference; Token = ct;
            return Task.FromResult(Result);
        }
        public Task<IReadOnlyList<StockCountView>> ListAsync(string? status, long? productId, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockCountView?> GetAsync(long id, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockSupplyReport> GetSupplyReportAsync(DateOnly from, DateOnly to, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(CancellationToken ct) => throw new NotSupportedException();
    }
}
