using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class PlayerGoodsOrdersControllerTests
{
    private const long PlayerId = 42;
    private const long OrderId = 17;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task List_ReturnsOrdersForCurrentPlayer()
    {
        IReadOnlyList<BuyerGoodsOrder> orders = [CreateOrder()];
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ListForBuyerAsync(PlayerId, TestCancellationToken)).ReturnsAsync(orders);
        var controller = CreateController(service);

        var result = await controller.List(TestCancellationToken);

        Assert.Same(orders, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ListForBuyerAsync(PlayerId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task List_WhenPlayerIdIsMissingOrInvalid_ReturnsUnauthorized(string? userIdClaim)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var controller = CreateController(service, userIdClaim: userIdClaim);

        var result = await controller.List(TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Receive_WhenRequestIsValid_ReturnsReceivedOrder()
    {
        var order = CreateOrder("Completed", "Received");
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ReceiveAsBuyerAsync(OrderId, PlayerId, TestCancellationToken)).ReturnsAsync(order);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await controller.Receive(OrderId, TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.ReceiveAsBuyerAsync(OrderId, PlayerId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Receive_WhenPlayerIdIsMissingOrInvalid_ReturnsUnauthorizedWithoutCallingDependencies(string? userIdClaim)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, userIdClaim);

        var result = await controller.Receive(OrderId, TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    public async Task Receive_WhenApplicationRequestHeaderIsInvalid_ReturnsBadRequestWithoutCallingDependencies(string? requestHeader)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, requestHeader: requestHeader);

        var result = await controller.Receive(OrderId, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_request", GetProperty<string>(badRequest.Value, "code"));
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Receive_WhenCsrfIsInvalid_ReturnsBadRequestWithoutReceivingOrder()
    {
        var service = new Mock<IGoodsPaymentsService>();
        var controller = CreateController(service, InvalidAntiforgery());

        var result = await controller.Receive(OrderId, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng thử lại.",
            GetProperty<string>(badRequest.Value, "message"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Receive_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ReceiveAsBuyerAsync(OrderId, PlayerId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("goods receipt failed"));
        var controller = CreateController(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.Receive(OrderId, TestCancellationToken));

        Assert.Equal("goods receipt failed", exception.Message);
    }

    [Fact]
    public async Task List_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ListForBuyerAsync(PlayerId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("goods orders unavailable"));
        var controller = CreateController(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.List(TestCancellationToken));

        Assert.Equal("goods orders unavailable", exception.Message);
    }

    private static PlayerGoodsOrdersController CreateController(
        Mock<IGoodsPaymentsService> service,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = "42",
        string? requestHeader = "1")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));
        if (requestHeader is not null)
            context.Request.Headers["X-TKSC-Request"] = requestHeader;

        return new PlayerGoodsOrdersController(service.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static BuyerGoodsOrder CreateOrder(string status = "Invoiced", string? handoverState = "Delivered") =>
        new(OrderId, "GO-17", 50000, status, "Paid", handoverState, Array.Empty<GoodsLine>());

    private static Mock<IAntiforgery> ValidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        return antiforgery;
    }

    private static Mock<IAntiforgery> InvalidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        return antiforgery;
    }

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
