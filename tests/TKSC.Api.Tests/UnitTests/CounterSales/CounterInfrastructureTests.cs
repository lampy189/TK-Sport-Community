using System.Text.Json;
using be_da.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.ExternalServices;
using TKSC.Infrastructure.Persistence.Configurations;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests.CounterSales;

public sealed class CounterInfrastructureTests
{
    [Fact]
    public async Task StoreRepositoryReturnsExistingCartForSameActorKeyAndFingerprint()
    {
        await using var db = CreateContext();
        var product = new Product { Id = 4, Sku = "P4", Name = "Ball", UnitPriceVnd = 2000 };
        var order = new Order { Id = 7, OrderCode = "ORDER-7", StaffId = 22, Status = "Cart", PaymentState = "Unpaid", TotalAmount = 4000 };
        db.Products.Add(product);
        db.Orders.Add(order);
        db.OrderItems.Add(new OrderItem { Id = 1, Order = order, Product = product, Quantity = 2, UnitPriceVnd = 2000, SubtotalVnd = 4000 });
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = 22, EntityType = "GoodsOrder", EntityId = 7, Action = "goods_order_idempotency", OldValue = "counter-key",
            NewValue = JsonSerializer.Serialize(new { fingerprint = "same", orderId = 7, expiresAt = new DateTimeOffset(2026, 11, 26, 3, 15, 0, TimeSpan.Zero) })
        });
        await db.SaveChangesAsync();
        var repository = new StoreRepository(db, new ConfigurationBuilder().Build());

        var result = await repository.FindIdempotentCartAsync(22, "counter-key", "same", default);

        Assert.NotNull(result);
        Assert.Equal(OperationOutcome.Success, result.Outcome);
        var cart = Assert.IsType<StoreCartOrderResult>(result.Value);
        Assert.Equal(7, cart.OrderId);
        Assert.Equal(4000, cart.TotalAmount);
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
        Assert.Equal(new DateTimeOffset(2026, 11, 26, 3, 15, 0, TimeSpan.Zero), cart.ExpiresAt);
        Assert.Null(await repository.FindIdempotentCartAsync(23, "counter-key", "same", default));
    }

    [Fact]
    public async Task StoreRepositoryRejectsKeyReusedForDifferentCart()
    {
        await using var db = CreateContext();
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = 22, EntityType = "GoodsOrder", EntityId = 7, Action = "goods_order_idempotency", OldValue = "counter-key",
            NewValue = JsonSerializer.Serialize(new { fingerprint = "original", orderId = 7 })
        });
        await db.SaveChangesAsync();
        var repository = new StoreRepository(db, new ConfigurationBuilder().Build());

        var result = await repository.FindIdempotentCartAsync(22, "counter-key", "different", default);

        Assert.NotNull(result);
        Assert.Equal(OperationOutcome.Conflict, result.Outcome);
        Assert.Contains("IDEMPOTENCY_KEY_REUSED", JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public async Task StoreRepositoryReportsMissingSavedOrderRatherThanCreatingAnother()
    {
        await using var db = CreateContext();
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = 22, EntityType = "GoodsOrder", EntityId = 7, Action = "goods_order_idempotency", OldValue = "counter-key",
            NewValue = JsonSerializer.Serialize(new { fingerprint = "same", orderId = 7 })
        });
        await db.SaveChangesAsync();
        var repository = new StoreRepository(db, new ConfigurationBuilder().Build());

        var result = await repository.FindIdempotentCartAsync(22, "counter-key", "same", default);
        Assert.NotNull(result);
        Assert.Equal(OperationOutcome.Conflict, result.Outcome);
        Assert.Contains("IDEMPOTENCY_RESULT_MISSING", JsonSerializer.Serialize(result.Value));
    }

    [Theory]
    [InlineData("Payments:PayOS:ClientId")]
    [InlineData("Payments:PayOS:ApiKey")]
    [InlineData("Payments:PayOS:ChecksumKey")]
    public void GatewayRejectsMissingRequiredConfiguration(string missingKey)
    {
        var config = Configuration();
        config[missingKey] = "";
        var gateway = new PayOsGateway(config, new PayOsClientFactory(config));
        Assert.Throws<GatewayException>(() => gateway.ValidateConfiguration());
    }

    [Theory]
    [InlineData("http://public.example.test/return")]
    [InlineData("not-a-url")]
    [InlineData("file:///C:/test")]
    public void GatewayRejectsUnsafeReturnUrl(string returnUrl)
    {
        var config = Configuration();
        config["Payments:PayOS:ReturnUrl"] = returnUrl;
        var gateway = new PayOsGateway(config, new PayOsClientFactory(config));
        Assert.Throws<GatewayException>(() => gateway.ValidateConfiguration());
    }

    [Fact]
    public void GatewayAllowsLoopbackHttpForDevelopmentAndKeepsGoodsReturnSeparate()
    {
        var config = Configuration();
        config["Payments:PayOS:ReturnUrl"] = "http://localhost:5173/return";
        var gateway = new PayOsGateway(config, new PayOsClientFactory(config));
        gateway.ValidateConfiguration();
        var request = gateway.BuildRequest(12, 42, 50000, new DateTimeOffset(2026, 11, 26, 3, 15, 0, TimeSpan.Zero), goods: true);
        Assert.Equal("http://localhost:5173/return?goodsOrderId=42", request.ReturnUrl);
        Assert.DoesNotContain("orderId=", request.ReturnUrl, StringComparison.Ordinal);
    }

    private static Sep490Context CreateContext() => new(new DbContextOptionsBuilder<Sep490Context>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Payments:PayOS:ClientId"] = "test-client",
        ["Payments:PayOS:ApiKey"] = "test-key",
        ["Payments:PayOS:ChecksumKey"] = "test-checksum",
        ["Payments:PayOS:ReturnUrl"] = "https://example.test/return",
        ["Payments:PayOS:CancelUrl"] = "https://example.test/cancel"
    }).Build();
}
