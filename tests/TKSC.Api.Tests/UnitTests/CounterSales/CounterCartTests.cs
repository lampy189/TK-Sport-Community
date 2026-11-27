using be_da.Application.DTOs;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests.CounterSales;

public sealed class CounterCartTests
{
    private const string Key = "counter-sale-key-123456";
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 3, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("counter sale with spaces")]
    [InlineData("counter-sale-invalid!")]
    public async Task InvalidIdempotencyKeyIsRejectedBeforeRepositoryAccess(string? key)
    {
        var repo = new CartRepository();
        var result = await Service(repo).CreateCartAsync(new([new(1, 1)]), 22, false, null, false, null, key, default);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Equal(0, repo.LookupCalls);
        Assert.Null(repo.SavedOrder);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task InvalidProductOrQuantityIsRejected(long productId, short quantity)
    {
        var repo = new CartRepository();
        var result = await Service(repo).CreateCartAsync(new([new(productId, quantity)]), 22, false, null, false, null, Key, default);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Equal(0, repo.LookupCalls);
        Assert.Null(repo.SavedOrder);
    }

    [Fact]
    public async Task EmptyCartAndQuantityOverflowAreRejected()
    {
        var repo = new CartRepository();
        var service = Service(repo);
        Assert.Equal(OperationOutcome.Invalid, (await service.CreateCartAsync(new(null), 22, false, null, false, null, Key, default)).Outcome);
        Assert.Equal(OperationOutcome.Invalid, (await service.CreateCartAsync(new([new(1, short.MaxValue), new(1, 1)]), 22, false, null, false, null, Key, default)).Outcome);
        Assert.Equal(0, repo.LookupCalls);
    }

    [Fact]
    public async Task CartAggregatesLinesAndSavesFixedPriceAndExpiry()
    {
        var repo = new CartRepository();
        var result = await Service(repo).CreateCartAsync(new([new(1, 2), new(1, 3), new(2, 1)]), 22, false, "Counter customer", true, 5, Key, default);

        Assert.Equal(OperationOutcome.Created, result.Outcome);
        var order = Assert.IsType<Order>(repo.SavedOrder);
        Assert.Equal(22, order.StaffId);
        Assert.Null(order.BuyerId);
        Assert.Equal("Cart", order.Status);
        Assert.Equal("Unpaid", order.PaymentState);
        Assert.Equal(12000, order.TotalAmount);
        Assert.Equal(5, repo.Lines.Single(x => x.ProductId == 1).Quantity);
        Assert.Equal(10000, repo.Lines.Single(x => x.ProductId == 1).SubtotalVnd);
        Assert.Equal(Now.AddMinutes(15), repo.ExpiresAt);
    }

    [Fact]
    public async Task SameCartLinesHaveSameFingerprintRegardlessOfOrderOrGrouping()
    {
        var repo = new CartRepository();
        var service = Service(repo);
        await service.CreateCartAsync(new([new(2, 1), new(1, 2), new(1, 3)]), 22, false, null, true, 5, Key, default);
        var firstFingerprint = repo.Fingerprint;
        await service.CreateCartAsync(new([new(1, 5), new(2, 1)]), 22, false, null, true, 5, Key, default);
        Assert.Equal(firstFingerprint, repo.Fingerprint);
    }

    [Fact]
    public async Task PreviousResultReturnsWithoutLookingUpProductsOrSavingAnotherOrder()
    {
        var repo = new CartRepository { Previous = OperationResult.Success("existing order") };
        var result = await Service(repo).CreateCartAsync(new([new(1, 1)]), 22, false, null, true, 5, Key, default);

        Assert.Same(repo.Previous, result);
        Assert.Equal(0, repo.ProductCalls);
        Assert.Null(repo.SavedOrder);
    }

    [Fact]
    public async Task MissingCenterOrProductOutsideCenterIsRejected()
    {
        var repo = new CartRepository { Allowed = [1] };
        var service = Service(repo);
        Assert.Equal(OperationOutcome.Invalid, (await service.CreateCartAsync(new([new(1, 1)]), 22, false, null, true, null, Key, default)).Outcome);
        Assert.Equal(OperationOutcome.Missing, (await service.CreateCartAsync(new([new(2, 1)]), 22, false, null, true, 5, Key, default)).Outcome);
        Assert.Equal(0, repo.ProductCalls);
        Assert.Null(repo.SavedOrder);
    }

    [Fact]
    public async Task MissingProductOrInsufficientStockDoesNotSaveOrder()
    {
        var repo = new CartRepository { Allowed = [1, 99] };
        var service = Service(repo);
        Assert.Equal(OperationOutcome.Missing, (await service.CreateCartAsync(new([new(99, 1)]), 22, false, null, true, 5, Key, default)).Outcome);
        Assert.Equal(OperationOutcome.Conflict, (await service.CreateCartAsync(new([new(1, 11)]), 22, false, null, true, 5, Key, default)).Outcome);
        Assert.Null(repo.SavedOrder);
    }

    [Fact]
    public async Task CartRejectsTotalBeyondInt32WithoutSaving()
    {
        var repo = new CartRepository();
        repo.Products[0].UnitPriceVnd = int.MaxValue;
        var result = await Service(repo).CreateCartAsync(new([new(1, 2)]), 22, false, null, true, 5, Key, default);
        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.Null(repo.SavedOrder);
    }

    private static StoreService Service(CartRepository repo) => new(repo, new FixedClock());

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class CartRepository : IStoreRepository
    {
        public long[]? Allowed = [1, 2];
        public List<Product> Products =
        [
            new() { Id = 1, Sku = "P1", Name = "P1", UnitPriceVnd = 2000, StockItem = new() { QuantityOnHand = 10 } },
            new() { Id = 2, Sku = "P2", Name = "P2", UnitPriceVnd = 2000, StockItem = new() { QuantityOnHand = 10 } }
        ];
        public OperationResult? Previous;
        public int LookupCalls;
        public int ProductCalls;
        public Order? SavedOrder;
        public List<StoreCartItemResult> Lines = [];
        public DateTimeOffset ExpiresAt;
        public string? Fingerprint;

        public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoreProductItem>>([]);
        public Task<long[]?> CenterProductIdsAsync(bool scoped, long? venueId, CancellationToken ct) => Task.FromResult(Allowed);
        public Task<OperationResult?> FindIdempotentCartAsync(long actorId, string key, string fingerprint, CancellationToken ct)
        {
            LookupCalls++;
            Fingerprint = fingerprint;
            return Task.FromResult(Previous);
        }
        public Task<List<Product>> FindProductsAsync(List<long> ids)
        {
            ProductCalls++;
            return Task.FromResult(Products.Where(p => ids.Contains(p.Id)).ToList());
        }
        public Task<OperationResult> SaveCartAsync(Order order, List<StoreCartItemResult> lines, List<Product> products, long actorId, bool scoped, long? venueId, string key, string fingerprint, DateTimeOffset expiresAt, CancellationToken ct)
        {
            SavedOrder = order;
            Lines = lines;
            ExpiresAt = expiresAt;
            return Task.FromResult(OperationResult.Created(order));
        }
    }
}
