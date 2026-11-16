using be_da.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using Xunit;

public sealed class StoreServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-14T10:00:00Z");

    [Fact]
    public async Task CreateCartRequiresAnIdempotencyKey()
    {
        var repository = new Repository();
        var service = new StoreService(repository, new Clock());

        var result = await service.CreateCartAsync(new([new CreateCartLine(1, 1)]), 7, false, "Staff", false, null, null, CancellationToken.None);

        Assert.Equal(OperationOutcome.Invalid, result.Outcome);
        Assert.False(repository.Saved);
    }

    [Fact]
    public async Task CreateCartAggregatesRepeatedProductsAndPassesHoldExpiry()
    {
        var repository = new Repository();
        var service = new StoreService(repository, new Clock());
        var key = "counter-sale-attempt-0001";

        var result = await service.CreateCartAsync(new([new CreateCartLine(1, 2), new CreateCartLine(1, 3)]), 7, false, "Staff", false, null, key, CancellationToken.None);

        Assert.Equal(OperationOutcome.Created, result.Outcome);
        var line = Assert.Single(repository.Lines);
        Assert.Equal(5, line.Quantity);
        Assert.Equal(key, repository.IdempotencyKey);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("1:5"))), repository.Fingerprint);
        Assert.Equal(Now.AddMinutes(15), repository.ExpiresAt);
    }

    [Fact]
    public async Task CreateCartRetryReturnsStoredOrderBeforeReadingProducts()
    {
        var repository = new Repository
        {
            PreviousResult = OperationResult.Success(new StoreCartOrderResult(9, "ORD-9", 20000, "Cart", "Unpaid", "Pending", [], Now.AddMinutes(15)))
        };
        var service = new StoreService(repository, new Clock());

        var result = await service.CreateCartAsync(new([new CreateCartLine(1, 1)]), 7, false, "Staff", false, null, "counter-sale-attempt-0009", CancellationToken.None);

        Assert.Same(repository.PreviousResult, result);
        Assert.False(repository.Saved);
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Repository : IStoreRepository
    {
        public bool Saved { get; private set; }
        public IReadOnlyList<StoreCartItemResult> Lines { get; private set; } = [];
        public string? IdempotencyKey { get; private set; }
        public string? Fingerprint { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }
        public OperationResult? PreviousResult { get; set; }

        public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoreProductItem>>([]);
        public Task<long[]?> CenterProductIdsAsync(bool scoped, long? venueId, CancellationToken ct) => Task.FromResult<long[]?>(null);
        public Task<OperationResult?> FindIdempotentCartAsync(long actorId, string idempotencyKey, string fingerprint, CancellationToken ct) => Task.FromResult(PreviousResult);
        public Task<List<Product>> FindProductsAsync(List<long> productIds)
        {
            if (PreviousResult is not null) throw new InvalidOperationException("Product lookup must not run for an idempotent retry.");
            return Task.FromResult(productIds.Contains(1)
                ? new List<Product> { new() { Id = 1, Sku = "BALL-01", Name = "Bóng", UnitPriceVnd = 20000, IsActive = true } }
                : []);
        }

        public Task<OperationResult> SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, long actorId, bool scoped, long? venueId, string idempotencyKey, string fingerprint, DateTimeOffset expiresAt, CancellationToken ct)
        {
            Saved = true;
            Lines = orderItems;
            IdempotencyKey = idempotencyKey;
            Fingerprint = fingerprint;
            ExpiresAt = expiresAt;
            return Task.FromResult(OperationResult.Created(new StoreCartOrderResult(1, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, orderItems, expiresAt)));
        }
    }
}