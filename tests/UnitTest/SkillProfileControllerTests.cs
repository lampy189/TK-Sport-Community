using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Skills;
using be_da.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class SkillProfileControllerTests
{
    private const long UserId = 42;
    private const long SportId = 5;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task GetRubrics_WhenSportIsNotSpecified_ReturnsActiveRubricsForAllSports()
    {
        IReadOnlyList<SportRubricDto> rubrics = [CreateRubric()];
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.GetActiveRubricsAsync(null, TestCancellationToken)).ReturnsAsync(rubrics);

        var result = await CreateController(store).GetRubrics(null, TestCancellationToken);

        Assert.Same(rubrics, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetActiveRubricsAsync(null, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetRubrics_WhenSportIsSpecified_PassesSportIdToStore()
    {
        IReadOnlyList<SportRubricDto> rubrics = [CreateRubric()];
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.GetActiveRubricsAsync(SportId, TestCancellationToken)).ReturnsAsync(rubrics);

        var result = await CreateController(store).GetRubrics(SportId, TestCancellationToken);

        Assert.Same(rubrics, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetActiveRubricsAsync(SportId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetRubrics_WhenStoreThrows_PropagatesException()
    {
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.GetActiveRubricsAsync(null, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("rubrics unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).GetRubrics(null, TestCancellationToken));

        Assert.Equal("rubrics unavailable", exception.Message);
    }

    [Fact]
    public async Task GetMySkillProfiles_WhenOverviewExists_ReturnsOverviewForCurrentUser()
    {
        var overview = CreateOverview();
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.GetPlayerSkillOverviewAsync(UserId, TestCancellationToken)).ReturnsAsync(overview);

        var result = await CreateController(store).GetMySkillProfiles(TestCancellationToken);

        Assert.Same(overview, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetPlayerSkillOverviewAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetMySkillProfiles_WhenOverviewDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.GetPlayerSkillOverviewAsync(UserId, TestCancellationToken))
            .ReturnsAsync((PlayerSkillOverviewDto?)null);

        var result = await CreateController(store).GetMySkillProfiles(TestCancellationToken);

        var response = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("PLAYER_NOT_FOUND", GetProperty<string>(response.Value, "code"));
        store.Verify(x => x.GetPlayerSkillOverviewAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-user-id")]
    public async Task GetMySkillProfiles_WhenUserIdClaimIsMissingOrInvalid_ReturnsUnauthorized(
        string? userIdClaim)
    {
        var store = new Mock<ISkillProfileStore>();

        var result = await CreateController(store, userIdClaim: userIdClaim)
            .GetMySkillProfiles(TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SubmitMySurvey_WhenRequestIsValid_ReturnsSubmittedResult()
    {
        var request = ValidSurveyRequest();
        var submitted = new SubmitSkillSurveyResult(true, null, null, CreateProfile());
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.SubmitSelfAssessmentAsync(UserId, request, TestCancellationToken))
            .ReturnsAsync(submitted);
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery)
            .SubmitMySurvey(request, TestCancellationToken);

        Assert.Same(submitted, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.SubmitSelfAssessmentAsync(UserId, request, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task SubmitMySurvey_WhenCsrfIsInvalid_ReturnsBadRequestWithoutCallingStore()
    {
        var store = new Mock<ISkillProfileStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .SubmitMySurvey(ValidSurveyRequest(), TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SubmitMySurvey_WhenUserIdClaimIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<ISkillProfileStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, "invalid")
            .SubmitMySurvey(ValidSurveyRequest(), TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("ALREADY_RATED")]
    [InlineData("ALREADY_SURVEYED")]
    public async Task SubmitMySurvey_WhenSurveyCannotReplaceExistingResult_ReturnsConflict(string errorCode)
    {
        var request = ValidSurveyRequest();
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.SubmitSelfAssessmentAsync(UserId, request, TestCancellationToken))
            .ReturnsAsync(new SubmitSkillSurveyResult(false, errorCode, "An existing result cannot be replaced."));

        var result = await CreateController(store)
            .SubmitMySurvey(request, TestCancellationToken);

        var response = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
        Assert.Equal("An existing result cannot be replaced.", GetProperty<string>(response.Value, "message"));
    }

    [Fact]
    public async Task SubmitMySurvey_WhenStoreRejectsRequest_ReturnsBadRequest()
    {
        var request = ValidSurveyRequest();
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.SubmitSelfAssessmentAsync(UserId, request, TestCancellationToken))
            .ReturnsAsync(new SubmitSkillSurveyResult(false, "INVALID_ANSWERS", "Answer all required dimensions."));

        var result = await CreateController(store)
            .SubmitMySurvey(request, TestCancellationToken);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("INVALID_ANSWERS", GetProperty<string>(response.Value, "code"));
        Assert.Equal("Answer all required dimensions.", GetProperty<string>(response.Value, "message"));
    }

    [Fact]
    public async Task SubmitMySurvey_WhenStoreThrows_PropagatesException()
    {
        var request = ValidSurveyRequest();
        var store = new Mock<ISkillProfileStore>();
        store.Setup(x => x.SubmitSelfAssessmentAsync(UserId, request, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("survey submission unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).SubmitMySurvey(request, TestCancellationToken));

        Assert.Equal("survey submission unavailable", exception.Message);
    }

    private static SkillProfileController CreateController(
        Mock<ISkillProfileStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));

        return new SkillProfileController(store.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static SubmitSkillSurveyRequest ValidSurveyRequest() =>
        new(SportId, [new SurveyDimensionAnswerInput(1, 3), new SurveyDimensionAnswerInput(2, 4)]);

    private static SportRubricDto CreateRubric() =>
        new(1, SportId, "BM", "Badminton", "BM-CLB-V1", "Badminton Club Rubric",
            [new RubricDimensionDto(1, "FOOTWORK", "Footwork", 20)]);

    private static PlayerSkillOverviewDto CreateOverview() =>
        new(17, UserId, "Test Player", null, [CreateProfile()]);

    private static PlayerSportSkillProfileDto CreateProfile() =>
        new(10, SportId, "BM", "Badminton", 72, "B4", "Provisional", CreateRubric(),
            null, Array.Empty<RatingHistoryItemDto>());

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
