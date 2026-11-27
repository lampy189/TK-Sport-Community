using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace TKSC.Api.Tests.InventoryWorkflows;

public sealed class BatchReceiptWorkflowTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("REF WITH SPACE")]
    public async Task InvalidReferenceIsRejectedBeforeStore(string? sourceRef)
    {
        var store = new Store();
        var result = await Service(store).PostBatchReceiptAsync(new(sourceRef, [new("WATER", null, 2)]), 22, default);
        Assert.False(result.Success);
        Assert.Equal("VALIDATION_FAILED", result.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task EmptyAndTooManyLinesAreRejectedBeforeStore()
    {
        var store = new Store();
        var service = Service(store);
        Assert.Equal("VALIDATION_FAILED", (await service.PostBatchReceiptAsync(new("REF-1", []), 22, default)).Code);
        Assert.Equal("VALIDATION_FAILED", (await service.PostBatchReceiptAsync(new("REF-1", Enumerable.Repeat(new BatchReceiptLine("WATER", null, 1), 501).ToArray()), 22, default)).Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ValidBatchForwardsAllLinesActorTokenAndStoreResult()
    {
        var store = new Store();
        var request = new BatchReceiptRequest("REF-1", [new("WATER", "Pack", 2), new("BALL", null, 3)]);
        using var cts = new CancellationTokenSource();
        var result = await Service(store).PostBatchReceiptAsync(request, 22, cts.Token);
        Assert.Same(store.Result, result);
        Assert.Same(request, store.Request);
        Assert.Equal(22, store.Actor);
        Assert.Equal(cts.Token, store.Token);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task StoreLineErrorsArePreservedWithoutClaimingSqlWasRun()
    {
        var error = new BatchLineError(2, "Unknown product");
        var store = new Store { Result = BatchReceiptResult.Fail("INVALID_LINES", "invalid", [error]) };
        var result = await Service(store).PostBatchReceiptAsync(new("REF-1", [new("WATER", null, 1), new("BAD", null, 1)]), 22, default);
        Assert.Same(store.Result, result);
        Assert.Same(error, Assert.Single(result.LineErrors));
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task ReceiptControllerRejectsInvalidCsrfBeforeStore()
    {
        var store = new Store();
        var response = await Controller(store, false).PostBatchReceipt(new("REF-1", [new("WATER", null, 1)]), default);
        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ReceiptControllerRequiresNumericActorBeforeStore()
    {
        var store = new Store();
        var response = await Controller(store, true, null).PostBatchReceipt(new("REF-1", [new("WATER", null, 1)]), default);
        Assert.IsType<UnauthorizedResult>(response);
        Assert.Equal(0, store.Calls);
    }

    [Theory]
    [InlineData("INVALID_LINES", 400)]
    [InlineData("QUANTITY_TOO_LARGE", 400)]
    [InlineData("SOURCE_REF_USED", 409)]
    public async Task ReceiptControllerMapsStoreFailure(string code, int status)
    {
        var store = new Store { Result = BatchReceiptResult.Fail(code, "failure") };
        var response = Assert.IsAssignableFrom<ObjectResult>(await Controller(store, true).PostBatchReceipt(new("REF-1", [new("WATER", null, 1)]), default));
        Assert.Equal(status, response.StatusCode);
        Assert.Same(store.Result, response.Value);
        Assert.Equal(22, store.Actor);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReceiptControllerReturnsSuccessIncludingAlreadyPosted(bool alreadyPosted)
    {
        var store = new Store { Result = BatchReceiptResult.Ok([new(1, "Water", 12, "Pack", 2)], alreadyPosted) };
        var response = Assert.IsType<OkObjectResult>(await Controller(store, true).PostBatchReceipt(new("REF-1", [new("WATER", "Pack", 2)]), default));
        Assert.Same(store.Result, response.Value);
        Assert.Equal(alreadyPosted, Assert.IsType<BatchReceiptResult>(response.Value).AlreadyPosted);
        Assert.Equal(1, store.Calls);
    }

    private static StockService Service(Store store) => new(store);
    private static StockController Controller(Store store, bool csrf, string? actor = "22") => new(Service(store), new InventoryTestContext.Antiforgery(csrf))
    {
        ControllerContext = InventoryTestContext.ForActor(actor)
    };

    private sealed class Store : IStockStore
    {
        public int Calls;
        public long Actor;
        public CancellationToken Token;
        public BatchReceiptRequest? Request;
        public BatchReceiptResult Result = BatchReceiptResult.Fail("TEST_RESULT", "test");
        public Task<BatchReceiptResult> PostBatchReceiptAsync(BatchReceiptRequest request, long staffUserId, CancellationToken ct)
        {
            Calls++; Request = request; Actor = staffUserId; Token = ct;
            return Task.FromResult(Result);
        }
        public Task<IReadOnlyList<StockProduct>> GetProductsAsync(StockProductFilter filter, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockProduct?> GetProductAsync(long productId, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken ct) => throw new NotSupportedException();
        public Task<StockCodeLookup?> LookupCodeAsync(string code, CancellationToken ct) => throw new NotSupportedException();
    }
}
