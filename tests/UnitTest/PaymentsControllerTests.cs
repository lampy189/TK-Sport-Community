using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class PaymentsControllerTests
{
    private const long UserId = 42;
    private const long OrderId = 17;
    private const long TicketId = 31;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task Display_ReturnsOrderForCurrentUser()
    {
        var order = CreateOrderSummary();
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.DisplayAsync(OrderId, UserId)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.Display(OrderId);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.DisplayAsync(OrderId, UserId), Times.Once);
    }

    [Fact]
    public async Task Process_WhenRequestIsValid_ReturnsCheckoutResult()
    {
        var checkout = new CheckoutResult(OrderId, "PAY-17", "Pending", "https://checkout.example.test");
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.ProcessAsync(OrderId, UserId)).ReturnsAsync(checkout);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await controller.Process(OrderId);

        Assert.Same(checkout, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.ProcessAsync(OrderId, UserId), Times.Once);
    }

    [Fact]
    public async Task Refresh_WhenRequestIsValid_ReturnsUpdatedOrder()
    {
        var order = CreateOrderSummary();
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.RefreshAsync(OrderId, UserId)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.Refresh(OrderId);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RefreshAsync(OrderId, UserId), Times.Once);
    }

    [Fact]
    public async Task Flag_WhenRequestIsValid_SendsReasonAndReturnsReviewStatus()
    {
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.FlagAsync(OrderId, UserId, "Transfer not visible")).ReturnsAsync(true);
        var controller = CreateController(service);

        var result = await controller.Flag(OrderId, new PaymentsController.ReviewRequest("Transfer not visible"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("review_required", GetProperty<string>(ok.Value, "status"));
        service.Verify(x => x.FlagAsync(OrderId, UserId, "Transfer not visible"), Times.Once);
    }

    [Fact]
    public async Task Tickets_ReturnsTicketsForCurrentUser()
    {
        IReadOnlyList<TicketSummary> tickets = [CreateTicketSummary()];
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.TicketsAsync(UserId)).ReturnsAsync(tickets);
        var controller = CreateController(service);

        var result = await controller.Tickets();

        Assert.Same(tickets, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.TicketsAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task CancelTicket_WhenRequestIsValid_ReturnsCancellationResult()
    {
        var cancellation = new TicketCancelResult(TicketId, "cancelled", 12000);
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.CancelTicketAsync(TicketId, UserId)).ReturnsAsync(cancellation);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await controller.CancelTicket(TicketId);

        Assert.Same(cancellation, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.CancelTicketAsync(TicketId, UserId), Times.Once);
    }

    [Fact]
    public async Task Reviews_ReturnsReviewList()
    {
        IReadOnlyList<ReviewSummary> reviews = [new(5, OrderId, "PAY-17", 50000, "NeedsReview", [])];
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.ReviewsAsync()).ReturnsAsync(reviews);
        var controller = CreateController(service);

        var result = await controller.Reviews();

        Assert.Same(reviews, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReviewsAsync(), Times.Once);
    }

    [Fact]
    public async Task Webhook_WhenGoodsServiceHandlesPayload_DoesNotCallSocialPaymentService()
    {
        const string payload = """{"code":"00","orderCode":17}""";
        using var document = JsonDocument.Parse(payload);
        var service = new Mock<IPaymentService>();
        var goods = new Mock<IGoodsPaymentsService>();
        goods.Setup(x => x.TryWebhookAsync(payload, TestCancellationToken)).ReturnsAsync(true);
        var controller = CreateController(service, goodsPayments: goods);

        var result = await controller.Webhook(document.RootElement);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("00", GetProperty<string>(ok.Value, "code"));
        goods.Verify(x => x.TryWebhookAsync(payload, TestCancellationToken), Times.Once);
        service.Verify(x => x.HandleWebhookAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_WhenGoodsServiceDoesNotHandlePayload_DelegatesToSocialPaymentService()
    {
        const string payload = """{"code":"00","orderCode":17}""";
        using var document = JsonDocument.Parse(payload);
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.HandleWebhookAsync(payload)).Returns(Task.CompletedTask);
        var goods = new Mock<IGoodsPaymentsService>();
        goods.Setup(x => x.TryWebhookAsync(payload, TestCancellationToken)).ReturnsAsync(false);
        var controller = CreateController(service, goodsPayments: goods);

        var result = await controller.Webhook(document.RootElement);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("00", GetProperty<string>(ok.Value, "code"));
        service.Verify(x => x.HandleWebhookAsync(payload), Times.Once);
    }

    [Fact]
    public async Task Process_WhenCsrfIsInvalid_ReturnsBadRequestWithoutProcessing()
    {
        var service = new Mock<IPaymentService>();
        var controller = CreateController(service, InvalidAntiforgery());

        var result = await controller.Process(OrderId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CancelTicket_WhenCsrfIsInvalid_ReturnsBadRequestWithoutCancelling()
    {
        var service = new Mock<IPaymentService>();
        var controller = CreateController(service, InvalidAntiforgery());

        var result = await controller.CancelTicket(TicketId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Mutation_WhenApplicationRequestHeaderIsMissing_ThrowsPaymentException()
    {
        var service = new Mock<IPaymentService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, requestHeader: null);

        var exception = await Assert.ThrowsAsync<PaymentException>(() => controller.Process(OrderId));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("Missing application request header.", exception.Message);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Process_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<IPaymentService>();
        service.Setup(x => x.ProcessAsync(OrderId, UserId)).ThrowsAsync(new InvalidOperationException("payment failed"));
        var controller = CreateController(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Process(OrderId));

        Assert.Equal("payment failed", exception.Message);
    }

    private static PaymentsController CreateController(
        Mock<IPaymentService> service,
        Mock<IAntiforgery>? antiforgery = null,
        Mock<IGoodsPaymentsService>? goodsPayments = null,
        string? userIdClaim = "42",
        string? requestHeader = "1")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));
        if (requestHeader is not null)
            context.Request.Headers["X-TKSC-Request"] = requestHeader;

        return new PaymentsController(
            service.Object,
            (antiforgery ?? ValidAntiforgery()).Object,
            (goodsPayments ?? new Mock<IGoodsPaymentsService>()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static OrderSummary CreateOrderSummary() =>
        new(OrderId, "ORDER-17", 50000, "PendingPayment", null, Array.Empty<PaymentSummary>(), Array.Empty<TicketSummary>());

    private static TicketSummary CreateTicketSummary() =>
        new(TicketId, OrderId, 8, 20, 50000, "confirmed", "TK-31", true,
            new DateOnly(2026, 11, 12), new TimeOnly(18, 0), new TimeOnly(20, 0),
            "paid", null, "paid", "Badminton", "Court 1", "Test Venue", "Test Player",
            false, null, null);

    private static Mock<IAntiforgery> ValidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        return antiforgery;
    }

    private static Mock<IAntiforgery> InvalidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        return antiforgery;
    }

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
