using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class TicketCheckInControllerTests
{
    private const long ActorId = 42;
    private const string TicketCode = "TKSC-SESSION-123";
    private static readonly DateOnly SessionDate = new(2026, 11, 11);

    [Theory]
    [InlineData("Referee", false)]
    [InlineData("Manager", true)]
    [InlineData("manager", true)]
    public async Task Inspect_ForwardsTicketActorAndManagerPermission(string role, bool expectedManager)
    {
        var resultData = CreateResult();
        var service = new Mock<ITicketCheckInService>();
        service.Setup(x => x.ExecuteAsync(TicketCode, ActorId, expectedManager, false)).ReturnsAsync(resultData);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, role: role, includeRequestHeader: true);

        var result = await controller.Inspect(new TicketCheckInController.CheckInRequest(TicketCode));

        Assert.Same(resultData, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.ExecuteAsync(TicketCode, ActorId, expectedManager, false), Times.Once);
    }

    [Theory]
    [InlineData("Referee", false)]
    [InlineData("Manager", true)]
    [InlineData("manager", true)]
    public async Task Confirm_ForwardsConfirmationFlagAndManagerPermission(string role, bool expectedManager)
    {
        var resultData = CreateResult(checkedIn: true);
        var service = new Mock<ITicketCheckInService>();
        service.Setup(x => x.ExecuteAsync(TicketCode, ActorId, expectedManager, true)).ReturnsAsync(resultData);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, role: role, includeRequestHeader: true);

        var result = await controller.Confirm(new TicketCheckInController.CheckInRequest(TicketCode));

        Assert.Same(resultData, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.ExecuteAsync(TicketCode, ActorId, expectedManager, true), Times.Once);
    }

    [Fact]
    public async Task Inspect_ForwardsTicketCodeWithoutTransformingIt()
    {
        const string ticketCode = "  tksc-mixed-Case  ";
        var service = new Mock<ITicketCheckInService>();
        service.Setup(x => x.ExecuteAsync(ticketCode, ActorId, false, false)).ReturnsAsync(CreateResult(ticketCode));

        var result = await CreateController(service, role: "Referee", includeRequestHeader: true)
            .Inspect(new TicketCheckInController.CheckInRequest(ticketCode));

        Assert.IsType<OkObjectResult>(result);
        service.Verify(x => x.ExecuteAsync(ticketCode, ActorId, false, false), Times.Once);
    }

    [Fact]
    public async Task Inspect_ForwardsNullTicketCodeToServiceValidation()
    {
        var service = new Mock<ITicketCheckInService>();
        service.Setup(x => x.ExecuteAsync(null, ActorId, false, false))
            .ReturnsAsync(CreateResult(ticketCode: string.Empty));

        var result = await CreateController(service, role: "Referee", includeRequestHeader: true)
            .Inspect(new TicketCheckInController.CheckInRequest(null));

        Assert.IsType<OkObjectResult>(result);
        service.Verify(x => x.ExecuteAsync(null, ActorId, false, false), Times.Once);
    }

    [Fact]
    public async Task Mutation_WhenAntiforgeryValidationFails_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ITicketCheckInService>();
        var controller = CreateController(service, InvalidAntiforgery(), includeRequestHeader: true);

        var result = await controller.Confirm(new TicketCheckInController.CheckInRequest(TicketCode));

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(response.Value, "code"));
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng thử lại.", GetProperty<string>(response.Value, "message"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Mutation_WhenRequestHeaderIsMissing_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ITicketCheckInService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await controller.Inspect(new TicketCheckInController.CheckInRequest(TicketCode));

        Assert.IsType<BadRequestResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Mutation_WhenRequestHeaderHasUnexpectedValue_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ITicketCheckInService>();
        var controller = CreateController(service, requestHeaderValue: "true");

        var result = await controller.Confirm(new TicketCheckInController.CheckInRequest(TicketCode));

        Assert.IsType<BadRequestResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public async Task Mutation_WhenActorClaimIsMissingOrInvalid_ReturnsUnauthorizedWithoutCallingService(
        string? actorClaim)
    {
        var service = new Mock<ITicketCheckInService>();
        var controller = CreateController(service, actorIdClaim: actorClaim, includeRequestHeader: true);

        var result = await controller.Confirm(new TicketCheckInController.CheckInRequest(TicketCode));

        Assert.IsType<UnauthorizedResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Inspect_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<ITicketCheckInService>();
        service.Setup(x => x.ExecuteAsync(TicketCode, ActorId, false, false))
            .ThrowsAsync(new InvalidOperationException("ticket inspection unavailable"));
        var controller = CreateController(service, role: "Referee", includeRequestHeader: true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.Inspect(new TicketCheckInController.CheckInRequest(TicketCode)));

        Assert.Equal("ticket inspection unavailable", exception.Message);
    }

    private static TicketCheckInController CreateController(
        Mock<ITicketCheckInService> service,
        Mock<IAntiforgery>? antiforgery = null,
        string role = "Referee",
        string? actorIdClaim = "42",
        bool includeRequestHeader = false,
        string? requestHeaderValue = null)
    {
        var context = new DefaultHttpContext();
        if (includeRequestHeader)
            context.Request.Headers["X-TKSC-Request"] = "1";
        else if (requestHeaderValue is not null)
            context.Request.Headers["X-TKSC-Request"] = requestHeaderValue;
        if (actorIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, actorIdClaim),
                    new Claim(ClaimTypes.Role, role)
                ],
                "UnitTest"));

        return new TicketCheckInController(service.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static CheckInResult CreateResult(string ticketCode = TicketCode, bool checkedIn = false) =>
        new(17, ticketCode, "Test Player", "Badminton", "Court 1", SessionDate,
            new TimeOnly(18, 0), new TimeOnly(20, 0), checkedIn, checkedIn ? ActorId : null);

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
