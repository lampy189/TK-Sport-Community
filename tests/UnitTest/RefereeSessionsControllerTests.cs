using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.DTOs;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class RefereeSessionsControllerTests
{
    private const long ActorId = 42;

    [Fact]
    public async Task GetAssignedSessions_ReturnsSessionsForCurrentActor()
    {
        IReadOnlyList<AssignedSessionSummary> sessions = [CreateSession()];
        var service = new Mock<IRefereeSessionsService>();
        service.Setup(x => x.GetAssignedSessionsAsync(ActorId)).ReturnsAsync(sessions);
        var controller = CreateController(service);

        var result = await controller.GetAssignedSessions();

        Assert.Same(sessions, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.GetAssignedSessionsAsync(ActorId), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public async Task GetAssignedSessions_WhenActorIdIsMissingOrInvalid_ReturnsUnauthorized(string? actorIdClaim)
    {
        var service = new Mock<IRefereeSessionsService>();
        var controller = CreateController(service, actorIdClaim);

        var result = await controller.GetAssignedSessions();

        Assert.IsType<UnauthorizedResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAssignedSessions_WhenServiceThrows_PropagatesException()
    {
        var service = new Mock<IRefereeSessionsService>();
        service.Setup(x => x.GetAssignedSessionsAsync(ActorId))
            .ThrowsAsync(new InvalidOperationException("assigned sessions unavailable"));
        var controller = CreateController(service);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetAssignedSessions());

        Assert.Equal("assigned sessions unavailable", exception.Message);
    }

    private static RefereeSessionsController CreateController(
        Mock<IRefereeSessionsService> service,
        string? actorIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (actorIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorIdClaim)], "UnitTest"));

        return new RefereeSessionsController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static AssignedSessionSummary CreateSession() =>
        new(7, "Badminton", "Court 1", "TK Sport", new DateOnly(2026, 11, 11),
            new TimeOnly(18, 0), new TimeOnly(20, 0), "published", "B3",
            8, 4, true, "not_ready", "The session has not started.");
}
