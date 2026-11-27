using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Services;
using TKSC.Presentation.Controllers;
using Xunit;

namespace TKSC.Api.Tests.CounterSales;

public sealed class CounterControllerTests
{
    [Fact]
    public async Task StoreControllerRejectsInvalidCsrfWithoutCreatingCart()
    {
        var service = new StoreStub();
        var controller = new StoreController(service, new AntiforgeryStub(false)) { ControllerContext = Context() };
        var result = await controller.CreateCart(new([new(1, 2)]), "counter-sale-key-123456");
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task StoreControllerPassesActorLinesAndIdempotencyKeyToService()
    {
        var service = new StoreStub();
        var controller = new StoreController(service, new AntiforgeryStub(true)) { ControllerContext = Context() };
        var request = new CreateCartRequest([new(1, 2)]);
        var result = await controller.CreateCart(request, "counter-sale-key-123456");
        Assert.IsType<ObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Same(request, service.Request);
        Assert.Equal(22, service.Actor);
        Assert.Equal("counter-sale-key-123456", service.Key);
        Assert.False(service.IsPlayer);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CashEndpointRejectsMissingHeaderOrInvalidCsrfBeforePayment(bool header, bool csrf)
    {
        var repo = new PlayerGoodsPaymentTests.Repo();
        var service = new GoodsPaymentService(repo, new PlayerGoodsPaymentTests.Gateway(), TimeProvider.System);
        var controller = new GoodsPaymentsController(service, new AntiforgeryStub(csrf)) { ControllerContext = Context(header) };
        var result = await controller.ConfirmCash(3, new CashRequest(50000), default);
        if (header) Assert.IsType<BadRequestObjectResult>(result);
        else Assert.IsType<BadRequestResult>(result);
        Assert.Empty(repo.Order.Payments);
    }

    [Fact]
    public async Task CashEndpointPassesAuthenticatedStaffIdAndAmountToService()
    {
        var repo = new PlayerGoodsPaymentTests.Repo();
        var service = new GoodsPaymentService(repo, new PlayerGoodsPaymentTests.Gateway(), TimeProvider.System);
        var controller = new GoodsPaymentsController(service, new AntiforgeryStub(true)) { ControllerContext = Context() };
        var response = Assert.IsType<OkObjectResult>(await controller.ConfirmCash(3, new CashRequest(50000), default));
        Assert.Equal("Paid", Assert.IsType<GoodsOrderView>(response.Value).PaymentState);
        Assert.Equal(22, Assert.Single(repo.Order.Payments).ConfirmedBy);
    }

    [Fact]
    public async Task HandoverEndpointRejectsInvalidCsrfBeforeConsumingStock()
    {
        var repo = new PlayerGoodsPaymentTests.Repo();
        var service = new GoodsPaymentService(repo, new PlayerGoodsPaymentTests.Gateway(), TimeProvider.System);
        var controller = new GoodsHandoverController(service, new AntiforgeryStub(false)) { ControllerContext = Context() };
        Assert.IsType<BadRequestObjectResult>(await controller.Deliver(3, default));
        Assert.Equal(0, repo.Consumed);
    }

    [Fact]
    public async Task RecoveryEndpointPassesActionAndAuthenticatedActorToService()
    {
        var service = new RecoveryStub();
        var controller = new PaymentRecoveryController(service, new AntiforgeryStub(true)) { ControllerContext = Context() };
        var input = new PaymentRecoveryInput("Bank evidence verified", 15);
        Assert.IsType<OkObjectResult>(await controller.Act(4, "approve", input, default));
        Assert.Equal(4, service.PaymentId);
        Assert.Equal(22, service.Actor);
        Assert.Equal("approve", service.Action);
        Assert.Same(input, service.Input);
    }

    private static ControllerContext Context(bool includeHeader = true)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "22"), new Claim(ClaimTypes.Role, "Staff"), new Claim(ClaimTypes.Name, "Staff")], "test"))
        };
        if (includeHeader) http.Request.Headers["X-TKSC-Request"] = "1";
        return new ControllerContext { HttpContext = http };
    }

    private sealed class AntiforgeryStub(bool valid) : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public AntiforgeryTokenSet GetTokens(HttpContext context) => new("request", "cookie", "form", "header");
        public Task<bool> IsRequestValidAsync(HttpContext context) => Task.FromResult(valid);
        public Task ValidateRequestAsync(HttpContext context) => valid ? Task.CompletedTask : Task.FromException(new AntiforgeryValidationException("Invalid test token."));
        public void SetCookieTokenAndHeader(HttpContext context) { }
    }

    private sealed class StoreStub : IStoreService
    {
        public int Calls;
        public CreateCartRequest? Request;
        public long Actor;
        public string? Key;
        public bool IsPlayer;
        public Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct) => Task.FromResult<IReadOnlyList<StoreProductItem>>([]);
        public Task<OperationResult> CreateCartAsync(CreateCartRequest request, long userId, bool isPlayer, string? customerName, bool scoped, long? venueId, string? idempotencyKey, CancellationToken ct)
        {
            Calls++;
            Request = request;
            Actor = userId;
            Key = idempotencyKey;
            IsPlayer = isPlayer;
            return Task.FromResult(OperationResult.Created(new { id = 3 }));
        }
    }

    private sealed class RecoveryStub : IPaymentRecoveryService
    {
        public long PaymentId;
        public long Actor;
        public string? Action;
        public PaymentRecoveryInput? Input;
        public Task<IReadOnlyList<PaymentRecoveryItem>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<PaymentRecoveryItem>>([]);
        public Task<object> ActAsync(long id, long actor, string action, PaymentRecoveryInput input, CancellationToken ct)
        {
            PaymentId = id;
            Actor = actor;
            Action = action;
            Input = input;
            return Task.FromResult<object>(new { status = "review_required" });
        }
    }
}
