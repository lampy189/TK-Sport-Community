using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.Assessments;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class AssessmentsControllerTests
{
    private const long ActorId = 42;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task Sessions_ReturnsServiceResultForCurrentReferee()
    {
        var sessions = new[] { new { Id = 7L } };
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.SessionsAsync(ActorId, TestCancellationToken)).ReturnsAsync((object)sessions);
        var controller = CreateController(service);

        var result = await controller.Sessions(TestCancellationToken);

        Assert.Same(sessions, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.SessionsAsync(ActorId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Rubric_ReturnsServiceResultForSport()
    {
        var rubric = new RubricView(3, 5, "BM-CLB-V1", "Badminton", true, Array.Empty<CriterionView>());
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.GetRubricAsync(5, TestCancellationToken)).ReturnsAsync(rubric);
        var controller = CreateController(service);

        var result = await controller.Rubric(5, TestCancellationToken);

        Assert.Same(rubric, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.GetRubricAsync(5, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Attendees_ReturnsServiceResultForAssignedSession()
    {
        IReadOnlyList<AttendeeView> attendees = [new(9, "Test Player", 12, "B3", "Rated", 15)];
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.GetAttendeesAsync(7, ActorId, TestCancellationToken)).ReturnsAsync(attendees);
        var controller = CreateController(service);

        var result = await controller.Attendees(7, TestCancellationToken);

        Assert.Same(attendees, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.GetAttendeesAsync(7, ActorId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Open_ReturnsAssessmentWhenRequestAndCsrfAreValid()
    {
        var assessment = CreateAssessment();
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.OpenAsync(7, 9, ActorId, TestCancellationToken)).ReturnsAsync(assessment);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery);

        var result = await controller.Open(7, 9, TestCancellationToken);

        Assert.Same(assessment, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        service.Verify(x => x.OpenAsync(7, 9, ActorId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Read_ReturnsAssessmentForCurrentReferee()
    {
        var assessment = CreateAssessment();
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.ReadAsync(15, ActorId, TestCancellationToken)).ReturnsAsync(assessment);
        var controller = CreateController(service);

        var result = await controller.Read(15, TestCancellationToken);

        Assert.Same(assessment, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.ReadAsync(15, ActorId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Save_ReturnsAssessmentWhenRequestAndCsrfAreValid()
    {
        var request = new SaveAssessmentRequest(4, [new MarkInput(2, 3, "Observed")]);
        var assessment = CreateAssessment();
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.SaveAsync(15, ActorId, request, TestCancellationToken)).ReturnsAsync(assessment);
        var controller = CreateController(service, ValidAntiforgery());

        var result = await controller.Save(15, request, TestCancellationToken);

        Assert.Same(assessment, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.SaveAsync(15, ActorId, request, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Publish_ReturnsAssessmentWhenRequestAndCsrfAreValid()
    {
        var request = new PublishAssessmentRequest(4, 55, "B3");
        var assessment = CreateAssessment();
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.PublishAsync(15, ActorId, request, TestCancellationToken)).ReturnsAsync(assessment);
        var controller = CreateController(service, ValidAntiforgery());

        var result = await controller.Publish(15, request, TestCancellationToken);

        Assert.Same(assessment, Assert.IsType<OkObjectResult>(result).Value);
        service.Verify(x => x.PublishAsync(15, ActorId, request, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task AnyActionWithoutValidActorId_ReturnsUnauthorizedWithoutCallingService()
    {
        var service = new Mock<ISkillAssessmentService>();
        var controller = CreateController(service, actorId: null);

        var result = await controller.Sessions(TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MutationWithoutRequestHeader_ReturnsBadRequestWithoutValidatingCsrf()
    {
        var service = new Mock<ISkillAssessmentService>();
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(service, antiforgery, requestHeader: null);

        var result = await controller.Open(7, 9, TestCancellationToken);

        Assert.IsType<BadRequestResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Never);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MutationWithInvalidCsrf_ReturnsBadRequestWithoutCallingService()
    {
        var service = new Mock<ISkillAssessmentService>();
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        var controller = CreateController(service, antiforgery);

        var result = await controller.Open(7, 9, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng thử lại.", GetProperty<string>(badRequest.Value, "message"));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ServiceAssessmentException_IsMappedToItsStatusAndMessage()
    {
        var service = new Mock<ISkillAssessmentService>();
        service.Setup(x => x.ReadAsync(15, ActorId, TestCancellationToken))
            .ThrowsAsync(new AssessmentException(StatusCodes.Status404NotFound, "Assessment not found"));
        var controller = CreateController(service);

        var result = await controller.Read(15, TestCancellationToken);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, error.StatusCode);
        Assert.Equal("Assessment not found", GetProperty<string>(error.Value, "message"));
    }

    private static AssessmentsController CreateController(
        Mock<ISkillAssessmentService> service,
        Mock<IAntiforgery>? antiforgery = null,
        long? actorId = ActorId,
        string? requestHeader = "1")
    {
        var context = new DefaultHttpContext();
        context.User = actorId is null
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId.Value.ToString())], "UnitTest"));
        if (requestHeader is not null)
            context.Request.Headers["X-TKSC-Request"] = requestHeader;

        return new AssessmentsController(service.Object, (antiforgery ?? ValidAntiforgery()).Object)
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

    private static AssessmentView CreateAssessment() =>
        new(15, 7, 9, "draft", 4,
            new RubricView(3, 5, "BM-CLB-V1", "Badminton", true, Array.Empty<CriterionView>()),
            Array.Empty<MarkInput>(), null, null, null, null);

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
