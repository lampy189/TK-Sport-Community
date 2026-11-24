using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TKSC.Application.DTOs;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Services;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class PlayerGoodsPaymentTests
{
    private static (GoodsPaymentService Service, Repo Repo, Gateway Gateway) Setup()
    {
        var repo = new Repo(); var gateway = new Gateway();
        return (new GoodsPaymentService(repo, gateway, TimeProvider.System), repo, gateway);
    }

    [Theory]
    [InlineData("read")]
    [InlineData("checkout")]
    [InlineData("refresh")]
    [InlineData("invoice")]
    public async Task OtherBuyersCannotReadOrMutateOrderOrCallGateway(string action)
    {
        var (service, repo, gateway) = Setup();
        Task Request() => action switch {
            "read" => service.ReadForBuyerAsync(3, 99, default),
            "checkout" => service.CheckoutForBuyerAsync(3, 99, default),
            "refresh" => service.RefreshForBuyerAsync(3, 99, default),
            _ => service.InvoiceForBuyerAsync(3, 99, default)
        };
        Assert.Equal(404, (await Assert.ThrowsAsync<PaymentException>(Request)).StatusCode);
        Assert.Equal(0, gateway.Calls);
        Assert.Empty(repo.Order.Payments); Assert.Empty(repo.Audits);
        Assert.Equal("Cart", repo.Order.Status);
    }

    [Fact]
    public async Task CheckoutRetryUsesSamePaymentAndRecordsBuyer()
    {
        var (service, repo, gateway) = Setup();
        var first = await service.CheckoutForBuyerAsync(3, 7, default);
        var retry = await service.CheckoutForBuyerAsync(3, 7, default);
        var payment = Assert.Single(repo.Order.Payments);
        Assert.Equal(7, payment.PayerId); Assert.Equal(3, payment.GoodsOrderId); Assert.Null(payment.OrderId);
        Assert.Equal(first.ReferenceCode, retry.ReferenceCode);
        Assert.Equal(first.ExpiresAt, retry.ExpiresAt);
        Assert.Equal("https://pay.example/checkout", retry.CheckoutUrl);
        Assert.Equal(50000, retry.TotalAmount); Assert.Equal("Pending", retry.PaymentState);
        Assert.All(gateway.CreatedIds, id => Assert.Equal(payment.Id, id));
    }

    [Fact]
    public async Task VerifiedPaymentCanBeRefreshedAndIssuedInvoiceViewedWithoutReissuing()
    {
        var (service, repo, gateway) = Setup();
        await service.CheckoutForBuyerAsync(3, 7, default);
        gateway.Remote = new(4, 50000, "PAID", "link", [new(4, 50000, "BANK1", "link", "VND", true, "{}")]);
        var paid = await service.RefreshForBuyerAsync(3, 7, default);
        Assert.Equal("Paid", paid.PaymentState); Assert.Null(paid.CheckoutUrl);
        await service.RefreshForBuyerAsync(3, 7, default);
        Assert.Single(repo.Events);
        var issued = await service.InvoiceAsync(3, 22, default);
        var invoice = await service.InvoiceForBuyerAsync(3, 7, default);
        Assert.Equal(JsonSerializer.Serialize(issued.Invoice), JsonSerializer.Serialize(invoice));
        Assert.Equal(22, invoice.IssuedBy);
        Assert.Equal(50000, invoice.TotalAmount);
        Assert.Single(repo.Audits, a => a.Action == "goods_invoice_issued");
        Assert.Equal(JsonSerializer.Serialize(invoice), JsonSerializer.Serialize((await service.ReadForBuyerAsync(3, 7, default)).Invoice));
    }

    [Fact]
    public async Task BrowserReturnOrRemotePaidWithoutBankEventCannotConfirmPayment()
    {
        var (service, repo, gateway) = Setup();
        await service.CheckoutForBuyerAsync(3, 7, default);
        gateway.Remote = new(4, 50000, "PAID", "link");
        Assert.Equal("Pending", (await service.RefreshForBuyerAsync(3, 7, default)).PaymentState);
        Assert.Equal(409, (await Assert.ThrowsAsync<PaymentException>(() => service.InvoiceForBuyerAsync(3, 7, default))).StatusCode);
        Assert.Empty(repo.Events);
    }

    [Theory]
    [InlineData("CANCELLED", "Cancelled")]
    [InlineData("EXPIRED", "Expired")]
    [InlineData("FAILED", "Failed")]
    public async Task ClosedGatewayLinkReturnsResultWithoutPayableUrl(string remote, string expected)
    {
        var (service, repo, gateway) = Setup();
        await service.CheckoutForBuyerAsync(3, 7, default);
        gateway.Remote = new(4, 0, remote, "link");
        var result = await service.RefreshForBuyerAsync(3, 7, default);
        Assert.Equal(expected, result.PaymentState); Assert.Null(result.CheckoutUrl);
        Assert.Equal("failed", Assert.Single(repo.Order.Payments).Status);
        Assert.Equal(expected, (await service.RefreshForBuyerAsync(3, 7, default)).PaymentState);
    }

    [Fact]
    public async Task OwnershipIsRecheckedAfterExternalCheckoutCall()
    {
        var (service, repo, gateway) = Setup();
        gateway.OnCreate = () => repo.Order.BuyerId = 99;
        Assert.Equal(404, (await Assert.ThrowsAsync<PaymentException>(() => service.CheckoutForBuyerAsync(3, 7, default))).StatusCode);
        Assert.Null(Assert.Single(repo.Order.Payments).CheckoutUrl);
    }

    [Theory]
    [InlineData("payment-instructions")]
    [InlineData("refresh")]
    public async Task PlayerMutationsRequireCsrf(string action)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", "Player");
        client.DefaultRequestHeaders.Add("X-TKSC-Request", "1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync($"/api/v1/commerce/my-goods-orders/3/{action}", null)).StatusCode);
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Referee")]
    [InlineData("Manager")]
    public async Task OtherRolesCannotUseBuyerEndpoints(string role)
    {
        await using var app = new SkillAuthorizationApp();
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("Test-Role", role);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/commerce/my-goods-orders/3")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/commerce/my-goods-orders/3/invoice")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/commerce/my-goods-orders/3/payment-instructions", null)).StatusCode);
    }

    [Fact]
    public async Task GuestCounterOrderCannotBeClaimedByPlayer()
    {
        var (service, repo, gateway) = Setup();
        repo.Order.BuyerId = null;
        Assert.Equal(404, (await Assert.ThrowsAsync<PaymentException>(() => service.CheckoutForBuyerAsync(3, 7, default))).StatusCode);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task CheckoutTimeoutRetryKeepsSavedPaymentIdentity()
    {
        var (service, repo, gateway) = Setup();
        gateway.OnCreate = () => throw new TimeoutException();
        await Assert.ThrowsAsync<TimeoutException>(() => service.CheckoutForBuyerAsync(3, 7, default));
        var payment = Assert.Single(repo.Order.Payments);
        gateway.OnCreate = null;
        await service.CheckoutForBuyerAsync(3, 7, default);
        Assert.Same(payment, Assert.Single(repo.Order.Payments));
        Assert.All(gateway.CreatedIds, id => Assert.Equal(payment.Id, id));
    }

    [Fact]
    public async Task WrongAmountReturnsReviewWithoutInvoice()
    {
        var (service, repo, gateway) = Setup();
        await service.CheckoutForBuyerAsync(3, 7, default);
        gateway.Remote = new(4, 49000, "PAID", "link", [new(4, 49000, "SHORT", "link", "VND", true, "{}")]);
        var result = await service.RefreshForBuyerAsync(3, 7, default);
        Assert.Equal("NeedsReview", result.PaymentState); Assert.Null(result.CheckoutUrl); Assert.Null(result.Invoice);
        Assert.Equal("review_required", Assert.Single(repo.Events).MatchStatus);
    }

    [Fact]
    public async Task CashInvoiceHandoverAndReceiptAreIdempotent()
    {
        var (service,repo,_)=Setup();
        await service.CashAsync(3,22,50000,default);
        await service.CashAsync(3,22,50000,default);
        Assert.Single(repo.Order.Payments);
        var first=await service.InvoiceAsync(3,22,default);
        var again=await service.InvoiceAsync(3,23,default);
        Assert.Equal(JsonSerializer.Serialize(first.Invoice),JsonSerializer.Serialize(again.Invoice));
        await service.DeliverAsync(3,22,default); await service.DeliverAsync(3,22,default);
        await service.ReceiveAsync(3,22,default); await service.ReceiveAsync(3,22,default);
        Assert.Equal(1,repo.Consumed);
        Assert.Equal("Completed",repo.Order.Status);
        Assert.Single(repo.Audits,a=>a.Action=="goods_invoice_issued");
    }
    [Fact]
    public async Task ExpiredStockHoldNeverConfirmsTransferOrAllowsDelivery()
    {
        var (service,repo,gateway)=Setup();
        await service.CheckoutForBuyerAsync(3,7,default);repo.HoldValid=false;
        gateway.Remote=new(4,50000,"PAID","link",[new(4,50000,"LATE","link","VND",true,"{}")]);
        Assert.Equal("NeedsReview",(await service.RefreshForBuyerAsync(3,7,default)).PaymentState);
        await Assert.ThrowsAsync<PaymentException>(()=>service.InvoiceAsync(3,22,default));
        await Assert.ThrowsAsync<PaymentException>(()=>service.DeliverAsync(3,22,default));
        Assert.Equal(0,repo.Consumed);
    }
    [Fact]
    public async Task StaffCanDeliverPaidOnlineOrderWithoutIssuingInvoiceSeparately()
    {
        var (service, repo, gateway) = Setup();
        await service.CheckoutForBuyerAsync(3, 7, default);
        gateway.Remote = new(4, 50000, "PAID", "link", [new(4, 50000, "PAID-ONLINE", "link", "VND", true, "{}")]);
        var paid = await service.RefreshForBuyerAsync(3, 7, default);
        Assert.Equal("Paid", paid.PaymentState);
        Assert.Null(paid.Invoice);

        var delivered = await service.DeliverAsync(3, 22, default);
        await service.DeliverAsync(3, 22, default);
        await service.ReceiveAsBuyerAsync(3, 7, default);

        Assert.Equal("Delivered", delivered.HandoverState);
        Assert.NotNull(delivered.Invoice);
        Assert.Equal(1, repo.Consumed);
        Assert.Single(repo.Audits, a => a.Action == "goods_invoice_issued");
        Assert.Single(repo.Audits, a => a.Action == "goods_delivered");
    }

    [Fact]
    public async Task DeliveryCanInvoiceLegacyPaidCashOrderWithCommittedStock()
    {
        var (service, repo, _) = Setup();
        await service.CashAsync(3, 22, 50000, default);
        repo.Audits.RemoveAll(a => a.Action == "goods_paid_lines");

        await Assert.ThrowsAsync<PaymentException>(() => service.InvoiceAsync(3, 22, default));
        var delivered = await service.DeliverAsync(3, 22, default);

        Assert.Equal("Delivered", delivered.HandoverState);
        Assert.NotNull(delivered.Invoice);
        Assert.Equal(1, repo.Consumed);
    }

    [Fact]
    public async Task CancelReleasesOnlyOnceAndLateTransferRequiresReview()
    {
        var (service,repo,gateway)=Setup();
        await service.CheckoutForBuyerAsync(3,7,default);
        await service.CancelAsync(3,7,7,default);await service.CancelAsync(3,7,7,default);
        Assert.Equal(1,repo.Released);
        gateway.Remote=new(4,50000,"PAID","link",[new(4,50000,"CANCELLED-LATE","link","VND",true,"{}")]);
        await service.RecheckAsync(4,default);
        Assert.Equal("NeedsReview",repo.Order.PaymentState);
        await Assert.ThrowsAsync<PaymentException>(()=>service.ApproveReviewAsync(4,1,22,"Đối soát có chứng từ ngân hàng",default));
        Assert.Equal(0,repo.Consumed);
    }
    [Fact]
    public async Task OutOfOrderEventRequiresExplicitApprovalAndRetryCannotDoubleRecordMoney()
    {
        var (service,repo,gateway)=Setup();
        await service.CheckoutForBuyerAsync(3,7,default);
        var bank=new VerifiedBankEvent(4,50000,"ORDERING","link","VND",false,"{}");
        gateway.Remote=new(4,50000,"PENDING","link",[bank]);
        await service.RecheckAsync(4,default);
        gateway.Remote=new(4,50000,"PAID","link",[bank with {Success=true}]);
        await service.RecheckAsync(4,default);
        Assert.Equal("NeedsReview",repo.Order.PaymentState);
        Assert.Single(repo.Events);
        await service.ApproveReviewAsync(4,1,22,"Ngân hàng xác nhận đủ tiền",default);
        await service.ApproveReviewAsync(4,1,22,"Ngân hàng xác nhận đủ tiền",default);
        Assert.Equal("Paid",repo.Order.PaymentState);Assert.Single(repo.Order.Payments);
        Assert.Single(repo.Audits,a=>a.Action=="goods_review_approved");
    }
    [Theory]
    [InlineData("Player")][InlineData("Referee")][InlineData("SystemAdmin")]
    public async Task RecoveryEndpointsDenyUnauthorizedRoles(string role)
    {
        await using var app=new SkillAuthorizationApp();
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost")});
        client.DefaultRequestHeaders.Add("Test-Role",role);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/commerce/payment-recovery")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/v1/commerce/payment-recovery/4/approve",new {evidence="Không được cấp quyền",bankTransactionId=1})).StatusCode);
    }
    [Theory]
    [InlineData("recheck")][InlineData("approve")][InlineData("close-unpaid")][InlineData("note")]
    public async Task RecoveryRequiresCsrf(string action)
    {
        await using var app=new SkillAuthorizationApp();
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost")});
        client.DefaultRequestHeaders.Add("Test-Role","Staff");client.DefaultRequestHeaders.Add("X-TKSC-Request","1");
        Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/v1/commerce/payment-recovery/4/"+action,new {evidence="Đối soát ngân hàng hợp lệ",bankTransactionId=1})).StatusCode);
    }

    private sealed class Gateway : IPaymentGateway
    {
        public int Calls; public List<long> CreatedIds = []; public Action? OnCreate;
        public GatewayStatus Remote = new(4, 0, "PENDING", "link");
        public void ValidateConfiguration() => Calls++;
        public Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiry) => throw new NotSupportedException();
        public Task<GatewayCheckout> CreateGoodsAsync(long paymentId, long orderId, int amount, DateTimeOffset expiry)
        { Calls++; CreatedIds.Add(paymentId); OnCreate?.Invoke(); return Task.FromResult(new GatewayCheckout(paymentId, amount, "link", "https://pay.example/checkout", "PENDING")); }
        public Task<GatewayStatus> GetAsync(long paymentId) { Calls++; return Task.FromResult(Remote); }
        public Task<VerifiedBankEvent> VerifyAsync(string payload) => throw new NotSupportedException();
    }
    private sealed class Repo : IGoodsPaymentRepository
    {
        public Task<IReadOnlyList<TicketHistoryEntry>> HistoryAsync(long id,CancellationToken ct) => Task.FromResult<IReadOnlyList<TicketHistoryEntry>>([]);
        public Task<Payment?> RecoveryPaymentAsync(long id,CancellationToken ct) => Task.FromResult(Order.Payments.SingleOrDefault(p=>p.Id==id));
        public Task<BankTransaction?> RecoveryBankAsync(long id,long bank,CancellationToken ct) => Task.FromResult(Events.SingleOrDefault(b=>b.MatchedPaymentId==id && b.Id==bank));
        public Task ReserveAsync(Order order, DateTimeOffset expiry, bool reacquire, CancellationToken ct) => Task.CompletedTask;
        public Task<DateTimeOffset?> HoldExpiryAsync(long id, CancellationToken ct) => Task.FromResult<DateTimeOffset?>(DateTimeOffset.UtcNow.AddMinutes(15));
        public bool HoldValid=true; public int Consumed; public int Released;
        public Task<bool> CommitHoldAsync(Order order, CancellationToken ct) => Task.FromResult(HoldValid);
        public Task ReleaseHoldAsync(long id, CancellationToken ct) { Released++; return Task.CompletedTask; }
        public Task ConsumeHoldAsync(Order order, long actor, CancellationToken ct) { Consumed++; return Task.CompletedTask; }
        public Order Order = new() { Id = 3, BuyerId = 7, OrderCode = "GOODS-3", Status = "Cart", PaymentState = "Unpaid", TotalAmount = 50000,
            OrderItems = [new() { Id = 1, ProductId = 2, Product = new() { Name = "Cầu" }, Quantity = 2, UnitPriceVnd = 25000, SubtotalVnd = 50000 }] };
        public List<AuditLog> Audits = []; public List<BankTransaction> Events = [];
        public Task<T> TransactionAsync<T>(Func<Task<T>> action, string? eventKey, CancellationToken ct) => action();
        public Task<Order?> LoadOrdersAsync(long id, CancellationToken ct) => Task.FromResult<Order?>(id == Order.Id ? Order : null);
        public Task<int> SaveAsync(CancellationToken ct) { foreach(var p in Order.Payments) if(p.Id == 0) p.Id = 4; return Task.FromResult(1); }
        public void ClearTracking() { }
        public Task<string?> AuditValueAuditLogsAsync(long id, string action, CancellationToken ct) => Task.FromResult(Audits.LastOrDefault(a => a.EntityId == id && a.Action == action)?.NewValue);
        public void AddAuditLogs(AuditLog value) => Audits.Add(value);
        public Task<bool> ApplyBankTransactionsExistsAsync(CancellationToken ct, string? externalId) => Task.FromResult(Events.Any(e => e.ExternalId == externalId));
        public Task<Payment> ApplyPaymentsAsync(VerifiedBankEvent data, CancellationToken ct) => Task.FromResult(Order.Payments.Single(p => p.Id == data.OrderCode));
        public void AddBankTransactions(BankTransaction value) { value.Id=Events.Count+1; Events.Add(value); }
        public Task<bool> TryWebhookPaymentsExistsAsync(CancellationToken ct, VerifiedBankEvent data) => Task.FromResult(Order.Payments.Any(p => p.Id == data.OrderCode));
        public Task<List<GoodsOrderListItem>> ListOrdersListAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<List<Order>> ListForBuyerOrdersListAsync(long buyerId, CancellationToken ct) => Task.FromResult(Order.BuyerId == buyerId ? new List<Order> { Order } : []);
        public Task<bool> ConfirmReceiptStockMovementsExistsAsync(CancellationToken ct, long id) => Task.FromResult(Consumed>0);
        public Task<long?> ConfirmReceiptStockItemsAsync(CancellationToken ct, GoodsStockLine line) => throw new NotSupportedException();
        public Task<int> ConfirmReceiptDatabaseUpdateAsync(CancellationToken ct, GoodsStockLine line, long stockItemId, long orderId) => throw new NotSupportedException();
        public void AddStockMovements(StockMovement value) => throw new NotSupportedException();
    }
}

