using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Services;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests.CounterSales;

public sealed class CounterPaymentTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(49999)]
    [InlineData(50001)]
    public async Task CashRejectsAmountOtherThanExactTotal(int received)
    {
        var (service, repo, _) = Setup();
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.CashAsync(3, 22, received, default));
        Assert.Equal(400, error.StatusCode);
        Assert.Empty(repo.Order.Payments);
        Assert.Equal("Unpaid", repo.Order.PaymentState);
        Assert.Empty(repo.Audits);
    }

    [Fact]
    public async Task ExpiredHoldPreventsCashFromBeingRecorded()
    {
        var (service, repo, _) = Setup();
        repo.HoldValid = false;
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.CashAsync(3, 22, 50000, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Empty(repo.Order.Payments);
        Assert.Equal("Unpaid", repo.Order.PaymentState);
    }

    [Fact]
    public async Task PendingTransferCannotBeReplacedWithCash()
    {
        var (service, repo, _) = Setup();
        await service.InstructionsAsync(3, 22, default);
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.CashAsync(3, 22, 50000, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal("payos", Assert.Single(repo.Order.Payments).Method);
        Assert.Equal("Pending", repo.Order.PaymentState);
    }

    [Fact]
    public async Task CheckoutPersistsPaymentAndSnapshotBeforeCallingGateway()
    {
        var (service, repo, gateway) = Setup();
        gateway.OnCreate = (paymentId, orderId, amount, expiry) =>
        {
            Assert.Equal(4, paymentId);
            Assert.Equal(3, orderId);
            Assert.Equal(50000, amount);
            Assert.True(expiry > DateTimeOffset.UtcNow);
            Assert.Equal("pending", Assert.Single(repo.Order.Payments).Status);
            Assert.Equal("PendingPayment", repo.Order.Status);
            Assert.Contains(repo.Audits, a => a.Action == "goods_checkout");
        };
        var result = await service.InstructionsAsync(3, 22, default);
        Assert.Equal("https://pay.example/checkout", result.CheckoutUrl);
    }

    [Theory]
    [InlineData("wrong-id")]
    [InlineData("wrong-amount")]
    [InlineData("empty-link")]
    [InlineData("insecure-url")]
    public async Task CheckoutRejectsUnexpectedGatewayResponse(string scenario)
    {
        var (service, repo, gateway) = Setup();
        gateway.Checkout = scenario switch
        {
            "wrong-id" => new(5, 50000, "link", "https://pay.example/checkout", "PENDING"),
            "wrong-amount" => new(4, 49999, "link", "https://pay.example/checkout", "PENDING"),
            "empty-link" => new(4, 50000, "", "https://pay.example/checkout", "PENDING"),
            _ => new(4, 50000, "link", "http://pay.example/checkout", "PENDING")
        };

        await Assert.ThrowsAsync<GatewayException>(() => service.InstructionsAsync(3, 22, default));
        Assert.Null(Assert.Single(repo.Order.Payments).CheckoutUrl);
        Assert.Equal("Pending", repo.Order.PaymentState);
    }

    [Fact]
    public async Task UnknownWebhookDoesNotChangeTheGoodsOrder()
    {
        var (service, repo, gateway) = Setup();
        gateway.Event = new(99, 50000, "bank-1", "link", "VND", true, "{}");
        Assert.False(await service.TryWebhookAsync("signed-event", default));
        Assert.Empty(repo.Order.Payments);
        Assert.Empty(repo.Events);
        Assert.Equal("Unpaid", repo.Order.PaymentState);
        Assert.Equal(0, gateway.GetCalls);
    }

    [Fact]
    public async Task ReplayedVerifiedWebhookDoesNotRecordMoneyTwice()
    {
        var (service, repo, gateway) = Setup();
        await service.InstructionsAsync(3, 22, default);
        var bank = new VerifiedBankEvent(4, 50000, "bank-1", "link", "VND", true, "{}");
        gateway.Event = bank;
        gateway.Remote = new(4, 50000, "PAID", "link", [bank]);

        Assert.True(await service.TryWebhookAsync("signed-event", default));
        Assert.True(await service.TryWebhookAsync("signed-event", default));
        Assert.Single(repo.Events);
        Assert.Equal("matched", repo.Events[0].MatchStatus);
        Assert.Equal("Paid", repo.Order.PaymentState);
        Assert.Single(repo.Audits, a => a.Action == "goods_transfer_confirmed" && a.EntityType == "GoodsOrder");
    }

    [Fact]
    public async Task InvoiceRejectsChangedPaidLines()
    {
        var (service, repo, _) = Setup();
        await service.CashAsync(3, 22, 50000, default);
        var item = Assert.Single(repo.Order.OrderItems);
        item.Product.Name = "changed";

        var error = await Assert.ThrowsAsync<PaymentException>(() => service.InvoiceAsync(3, 22, default));
        Assert.Equal(409, error.StatusCode);
        Assert.DoesNotContain(repo.Audits, a => a.Action == "goods_invoice_issued");
        Assert.Equal("Paid", repo.Order.Status);
    }

    [Fact]
    public async Task HandoverRejectsChangedInvoicedLinesWithoutConsumingStock()
    {
        var (service, repo, _) = Setup();
        await service.CashAsync(3, 22, 50000, default);
        await service.InvoiceAsync(3, 22, default);
        Assert.Single(repo.Order.OrderItems).Product.Name = "changed";

        var error = await Assert.ThrowsAsync<PaymentException>(() => service.DeliverAsync(3, 22, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(0, repo.Consumed);
        Assert.NotEqual("Delivered", repo.Order.HandoverState);
    }

    [Fact]
    public async Task ReceiptBeforeDeliveryIsRejectedWithoutStockMovement()
    {
        var (service, repo, _) = Setup();
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.ReceiveAsync(3, 22, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(0, repo.Consumed);
        Assert.NotEqual("Completed", repo.Order.Status);
    }

    [Theory]
    [InlineData("Paid")]
    [InlineData("NeedsReview")]
    public async Task CancelCannotReleasePaidOrReviewedOrders(string state)
    {
        var (service, repo, _) = Setup();
        repo.Order.PaymentState = state;
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.CancelAsync(3, 22, null, default));
        Assert.Equal(409, error.StatusCode);
        Assert.Equal(0, repo.Released);
        Assert.NotEqual("Cancelled", repo.Order.Status);
    }

    [Fact]
    public async Task MissingOrderDoesNotContactGateway()
    {
        var (service, _, gateway) = Setup();
        var error = await Assert.ThrowsAsync<PaymentException>(() => service.InstructionsAsync(999, 22, default));
        Assert.Equal(404, error.StatusCode);
        Assert.Equal(0, gateway.CreateCalls);
    }

    private static (GoodsPaymentService Service, PlayerGoodsPaymentTests.Repo Repo, Gateway Gateway) Setup()
    {
        var repo = new PlayerGoodsPaymentTests.Repo();
        repo.Order.BuyerId = null;
        var gateway = new Gateway();
        return (new GoodsPaymentService(repo, gateway, TimeProvider.System), repo, gateway);
    }

    private sealed class Gateway : IPaymentGateway
    {
        public Action<long, long, int, DateTimeOffset>? OnCreate;
        public GatewayCheckout? Checkout;
        public VerifiedBankEvent? Event;
        public GatewayStatus Remote = new(4, 0, "PENDING", "link");
        public int CreateCalls;
        public int GetCalls;
        public void ValidateConfiguration() { }
        public Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt) => throw new NotSupportedException();
        public Task<GatewayCheckout> CreateGoodsAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt)
        {
            CreateCalls++;
            OnCreate?.Invoke(paymentId, orderId, amount, expiresAt);
            return Task.FromResult(Checkout ?? new GatewayCheckout(paymentId, amount, "link", "https://pay.example/checkout", "PENDING"));
        }
        public Task<GatewayStatus> GetAsync(long paymentId) { GetCalls++; return Task.FromResult(Remote); }
        public Task<VerifiedBankEvent> VerifyAsync(string payload) => Task.FromResult(Event ?? throw new InvalidOperationException("Set a test event."));
    }
}
