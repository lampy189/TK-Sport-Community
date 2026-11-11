using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class GoodsPaymentsControllerTests
{
    private const long ActorId = 42;
    private const long OrderId = 17;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task List_ReturnsServiceResult()
    {
        var orders = new[] { new { Id = OrderId, Status = "Invoiced" } };
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ListAsync(TestCancellationToken)).ReturnsAsync((object)orders);
        var controller = CreateController(service);

        var result = await controller.List(TestCancellationToken);

        Assert.Same(orders, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ListAsync(TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Read_ReturnsServiceResultForOrder()
    {
        var order = CreateOrderView();
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ReadAsync(OrderId, TestCancellationToken)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.Read(OrderId, TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReadAsync(OrderId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Instructions_WhenRequestIsValid_ReturnsServiceResult()
    {
        var order = CreateOrderView();
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.InstructionsAsync(OrderId, ActorId, TestCancellationToken)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.Instructions(OrderId, TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.InstructionsAsync(OrderId, ActorId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ConfirmTransfer_WhenRequestIsValid_ReturnsServiceResult()
    {
        var order = CreateOrderView();
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.ConfirmTransferAsync(OrderId, TestCancellationToken)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.ConfirmTransfer(OrderId, TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ConfirmTransferAsync(OrderId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ConfirmCash_WhenRequestIsValid_PassesActorAndAmountToService()
    {
        const int receivedAmount = 50000;
        var order = CreateOrderView();
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.CashAsync(OrderId, ActorId, receivedAmount, TestCancellationToken)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.ConfirmCash(OrderId, new GoodsPaymentsController.CashRequest(receivedAmount), TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.CashAsync(OrderId, ActorId, receivedAmount, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Invoice_WhenRequestIsValid_PassesActorToService()
    {
        var order = CreateOrderView();
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.InvoiceAsync(OrderId, ActorId, TestCancellationToken)).ReturnsAsync(order);
        var controller = CreateController(service);

        var result = await controller.Invoice(OrderId, TestCancellationToken);

        Assert.Same(order, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.InvoiceAsync(OrderId, ActorId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData("instructions")]
    [InlineData("transfer")]
    [InlineData("cash")]
    [InlineData("invoice")]
    public async Task Mutation_WhenActorIdIsInvalid_ReturnsUnauthorizedWithoutCallingDependencies(string action)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, userIdClaim: "invalid");

        var result = await InvokeMutation(controller, action);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("instructions")]
    [InlineData("transfer")]
    [InlineData("cash")]
    [InlineData("invoice")]
    public async Task Mutation_WhenRequestHeaderIsInvalid_ReturnsBadRequest(string action)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, requestHeader: "0");

        var result = await InvokeMutation(controller, action);

        Assert.IsType<BadRequestResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("instructions")]
    [InlineData("transfer")]
    [InlineData("cash")]
    [InlineData("invoice")]
    public async Task Mutation_WhenCsrfIsInvalid_ReturnsBadRequest(string action)
    {
        var service = new Mock<IGoodsPaymentsService>();
        var antiforgery = InvalidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await InvokeMutation(controller, action);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Instructions_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<IGoodsPaymentsService>();
        service.Setup(x => x.InstructionsAsync(OrderId, ActorId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("payment service failed"));
        var controller = CreateController(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.Instructions(OrderId, TestCancellationToken));

        Assert.Equal("payment service failed", exception.Message);
    }

    private static Task<IActionResult> InvokeMutation(GoodsPaymentsController controller, string action) =>
        action switch
        {
            "instructions" => controller.Instructions(OrderId, TestCancellationToken),
            "transfer" => controller.ConfirmTransfer(OrderId, TestCancellationToken),
            "cash" => controller.ConfirmCash(OrderId, new GoodsPaymentsController.CashRequest(50000), TestCancellationToken),
            "invoice" => controller.Invoice(OrderId, TestCancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown mutation action.")
        };

    private static GoodsPaymentsController CreateController(
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

        return new GoodsPaymentsController(service.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

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

    private static GoodsOrderView CreateOrderView() =>
        new(OrderId, "GO-17", "Test Customer", 50000, "Invoiced", "Paid", "Pending",
            Array.Empty<GoodsLine>(), null, null, null, null, null);

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
