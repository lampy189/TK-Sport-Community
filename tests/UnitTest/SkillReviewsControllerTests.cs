using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.Assessments;
using TKSC.Application.Services;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class SkillReviewsControllerTests
{
    private const long ActorId = 42;
    private const long ItemId = 17;
    private const long SessionId = 31;
    private const long AssessmentId = 52;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task Exceptions_ForwardsActorRoleAndOptionalSessionToService()
    {
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.Exceptions(ActorId, "manager", SessionId, TestCancellationToken))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service, role: "Manager")
            .Exceptions(SessionId, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.Exceptions(ActorId, "manager", SessionId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RequestException_WithValidCsrf_ForwardsRequestToService()
    {
        var input = new ReviewInput("Please review my skill eligibility.");
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.RequestException(SessionId, ActorId, input, TestCancellationToken))
            .ReturnsAsync(resultObject);
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(service, "Player", antiforgery, includeCsrfHeader: true)
            .RequestException(SessionId, input, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.RequestException(SessionId, ActorId, input, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task DecideException_ForwardsNormalizedRoleAndDecision()
    {
        var input = new ExceptionDecision("Reviewed evidence.", true);
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.DecideException(ItemId, ActorId, "referee", input, TestCancellationToken))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service, "Referee", includeCsrfHeader: true)
            .DecideException(ItemId, input, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.DecideException(ItemId, ActorId, "referee", input, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Appeals_ForwardsActorAndNormalizedRole()
    {
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.Appeals(ActorId, "player", TestCancellationToken)).ReturnsAsync(resultObject);

        var result = await CreateController(service, role: "PLAYER").Appeals(TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.Appeals(ActorId, "player", TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RequestAppeal_WithValidCsrf_ForwardsRequestToService()
    {
        var input = new ReviewInput("I disagree with the published result.");
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.RequestAppeal(AssessmentId, ActorId, input, TestCancellationToken))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service, "Player", includeCsrfHeader: true)
            .RequestAppeal(AssessmentId, input, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.RequestAppeal(AssessmentId, ActorId, input, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Review_ForwardsRefereeReviewToService()
    {
        var input = new ReviewInput("Observed evidence supports the current result.");
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.ReviewAppeal(ItemId, ActorId, input, TestCancellationToken))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service, "Referee", includeCsrfHeader: true)
            .Review(ItemId, input, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReviewAppeal(ItemId, ActorId, input, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task DecideAppeal_ForwardsManagerDecisionToService()
    {
        var input = new AppealDecision("Evidence reviewed.", true, 80, 29);
        var resultObject = new object();
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.DecideAppeal(ItemId, ActorId, input, TestCancellationToken))
            .ReturnsAsync(resultObject);

        var result = await CreateController(service, "Manager", includeCsrfHeader: true)
            .DecideAppeal(ItemId, input, TestCancellationToken);

        Assert.Same(resultObject, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.DecideAppeal(ItemId, ActorId, input, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Mutation_WhenCsrfHeaderIsMissing_ReturnsBadRequestWithoutValidatingCsrfOrCallingService()
    {
        var service = new Mock<ISkillReviewService>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(service, "Player", antiforgery)
            .RequestException(SessionId, new ReviewInput("Request"), TestCancellationToken);

        Assert.IsType<BadRequestResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Mutation_WhenCsrfValidationFails_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ISkillReviewService>();

        var result = await CreateController(service, "Player", InvalidAntiforgery(), includeCsrfHeader: true)
            .RequestAppeal(AssessmentId, new ReviewInput("Request"), TestCancellationToken);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng thử lại.",
            GetProperty<string>(response.Value, "message"));
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("0")]
    public async Task Endpoint_WhenActorClaimIsMissingInvalidOrNonPositive_ReturnsUnauthorized(string? actorClaim)
    {
        var service = new Mock<ISkillReviewService>();

        var result = await CreateController(service, actorIdClaim: actorClaim)
            .Exceptions(null, TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    public async Task Endpoint_WhenServiceThrowsAssessmentException_UsesItsStatusCode(int statusCode)
    {
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.Exceptions(ActorId, "player", null, TestCancellationToken))
            .ThrowsAsync(new AssessmentException(statusCode, "Review request rejected."));

        var result = await CreateController(service).Exceptions(null, TestCancellationToken);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Equal("Review request rejected.", GetProperty<string>(response.Value, "message"));
    }

    [Fact]
    public async Task Endpoint_WhenServiceThrowsUnexpectedException_PropagatesException()
    {
        var service = new Mock<ISkillReviewService>();
        service.Setup(x => x.Exceptions(ActorId, "player", null, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("review service unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(service).Exceptions(null, TestCancellationToken));

        Assert.Equal("review service unavailable", exception.Message);
    }

    private static SkillReviewsController CreateController(
        Mock<ISkillReviewService> service,
        string role = "Player",
        Mock<IAntiforgery>? antiforgery = null,
        bool includeCsrfHeader = false,
        string? actorIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (includeCsrfHeader)
            context.Request.Headers["X-TKSC-Request"] = "1";
        if (actorIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, actorIdClaim),
                    new Claim(ClaimTypes.Role, role)
                ],
                "UnitTest"));

        return new SkillReviewsController(service.Object, (antiforgery ?? ValidAntiforgery()).Object)
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
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        return antiforgery;
    }

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
