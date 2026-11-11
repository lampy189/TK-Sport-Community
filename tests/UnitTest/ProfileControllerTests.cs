using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Profiles;
using be_da.Controllers;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class ProfileControllerTests
{
    private const long UserId = 42;
    private const long PlayerId = 17;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task GetMyProfile_ReturnsProfileForCurrentUser()
    {
        var profile = CreateProfile();
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetProfileAsync(UserId, TestCancellationToken)).ReturnsAsync(profile);

        var result = await CreateController(store).GetMyProfile(TestCancellationToken);

        Assert.Same(profile, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetProfileAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetMyProfile_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetProfileAsync(UserId, TestCancellationToken))
            .ReturnsAsync((UserProfileDetails?)null);

        var result = await CreateController(store).GetMyProfile(TestCancellationToken);

        Assert.Equal("profile_not_found",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task GetMyProfile_WhenUserIdClaimIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<IProfileStore>();

        var result = await CreateController(store, userIdClaim: "not-a-number")
            .GetMyProfile(TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("42", 42L)]
    [InlineData(null, null)]
    public async Task GetPublicPlayerProfile_PassesPlayerAndOptionalViewerToStore(
        string? userIdClaim,
        long? viewerUserId)
    {
        var profile = CreatePublicProfile();
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetPublicPlayerProfileAsync(PlayerId, viewerUserId, TestCancellationToken))
            .ReturnsAsync(profile);

        var result = await CreateController(store, userIdClaim: userIdClaim)
            .GetPublicPlayerProfile(PlayerId, TestCancellationToken);

        Assert.Same(profile, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetPublicPlayerProfileAsync(PlayerId, viewerUserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetPublicPlayerProfile_WhenPlayerDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetPublicPlayerProfileAsync(PlayerId, UserId, TestCancellationToken))
            .ReturnsAsync((PublicPlayerProfileDetails?)null);

        var result = await CreateController(store).GetPublicPlayerProfile(PlayerId, TestCancellationToken);

        Assert.Equal("player_not_found",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task GetMySportProfiles_ReturnsSportProfilesForCurrentUser()
    {
        IReadOnlyList<SportProfileItem> profiles = [CreateSportProfile()];
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetSportProfilesAsync(UserId, TestCancellationToken)).ReturnsAsync(profiles);

        var result = await CreateController(store).GetMySportProfiles(TestCancellationToken);

        Assert.Same(profiles, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSportProfilesAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetMySportProfiles_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.GetSportProfilesAsync(UserId, TestCancellationToken))
            .ReturnsAsync((IReadOnlyList<SportProfileItem>?)null);

        var result = await CreateController(store).GetMySportProfiles(TestCancellationToken);

        Assert.Equal("profile_not_found",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task UpdateMyProfile_WhenRequestIsValid_TrimsNameAndReturnsUpdatedProfile()
    {
        var request = new UpdateProfileRequest("  Updated Player  ", new DateOnly(1998, 10, 15),
            "Male", "https://example.test/avatar.jpg", "Player bio");
        var updated = CreateProfile("Updated Player");
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.UpdateProfileAsync(UserId, It.IsAny<UpdateProfileRequest>(), TestCancellationToken))
            .ReturnsAsync(updated);
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery)
            .UpdateMyProfile(request, TestCancellationToken);

        Assert.Same(updated, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.UpdateProfileAsync(UserId,
            It.Is<UpdateProfileRequest>(x => x.FullName == "Updated Player"), TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenCsrfIsInvalid_ReturnsBadRequestWithoutUpdating()
    {
        var store = new Mock<IProfileStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .UpdateMyProfile(ValidProfileRequest(), TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateMyProfile_WhenUserIdClaimIsInvalid_ReturnsUnauthorizedWithoutUpdating()
    {
        var store = new Mock<IProfileStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, "invalid")
            .UpdateMyProfile(ValidProfileRequest(), TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("blank_name")]
    [InlineData("short_name")]
    [InlineData("long_name")]
    [InlineData("future_birth_date")]
    [InlineData("old_birth_date")]
    [InlineData("invalid_gender")]
    [InlineData("long_bio")]
    [InlineData("invalid_avatar_url")]
    public async Task UpdateMyProfile_WhenInputIsInvalid_ReturnsBadRequestWithoutUpdating(string invalidInput)
    {
        var store = new Mock<IProfileStore>();
        var result = await CreateController(store)
            .UpdateMyProfile(InvalidProfileRequest(invalidInput), TestCancellationToken);

        var response = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(ExpectedProfileError(invalidInput), GetProperty<string>(response.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateMyProfile_WhenProfileDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.UpdateProfileAsync(UserId, It.IsAny<UpdateProfileRequest>(), TestCancellationToken))
            .ReturnsAsync((UserProfileDetails?)null);

        var result = await CreateController(store)
            .UpdateMyProfile(ValidProfileRequest(), TestCancellationToken);

        Assert.Equal("profile_not_found",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task UpdateSportProfile_WhenRequestIsValid_ReturnsUpdatedSportProfile()
    {
        var request = new UpdateSportProfileRequest(72, null, "Two years of recreational play");
        var profile = CreateSportProfile();
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.UpdateSportProfileAsync(UserId, "pickleball", request, TestCancellationToken))
            .ReturnsAsync(new SportProfileResult(true, null, null, profile));

        var result = await CreateController(store)
            .UpdateSportProfile("pickleball", request, TestCancellationToken);

        Assert.Same(profile, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.UpdateSportProfileAsync(UserId, "pickleball", request, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)100)]
    public async Task UpdateSportProfile_AcceptsScoreAtEitherBoundary(short score)
    {
        var store = new Mock<IProfileStore>();
        var request = new UpdateSportProfileRequest(score, null, null);
        store.Setup(x => x.UpdateSportProfileAsync(UserId, "badminton", request, TestCancellationToken))
            .ReturnsAsync(new SportProfileResult(true, null, null, CreateSportProfile()));

        var result = await CreateController(store)
            .UpdateSportProfile("badminton", request, TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.UpdateSportProfileAsync(UserId, "badminton", request, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((short)-1)]
    [InlineData((short)101)]
    public async Task UpdateSportProfile_WhenScoreIsOutOfRange_ReturnsBadRequest(short? score)
    {
        var store = new Mock<IProfileStore>();
        var request = new UpdateSportProfileRequest(score, null, null);

        var result = await CreateController(store)
            .UpdateSportProfile("badminton", request, TestCancellationToken);

        Assert.Equal("invalid_score",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateSportProfile_WhenCsrfIsInvalid_ReturnsBadRequestWithoutUpdating()
    {
        var store = new Mock<IProfileStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .UpdateSportProfile("badminton", new UpdateSportProfileRequest(50, null, null), TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("sport_not_found", 404)]
    [InlineData("confirmed_rating_cannot_be_overwritten", 409)]
    [InlineData("invalid_profile", 400)]
    public async Task UpdateSportProfile_MapsStoreErrorsToHttpStatus(string errorCode, int expectedStatus)
    {
        var store = new Mock<IProfileStore>();
        store.Setup(x => x.UpdateSportProfileAsync(UserId, "badminton",
                It.IsAny<UpdateSportProfileRequest>(), TestCancellationToken))
            .ReturnsAsync(new SportProfileResult(false, errorCode, "Update rejected", null));

        var result = await CreateController(store)
            .UpdateSportProfile("badminton", new UpdateSportProfileRequest(50, null, null), TestCancellationToken);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
    }

    private static ProfileController CreateController(
        Mock<IProfileStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));

        return new ProfileController(store.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static UpdateProfileRequest ValidProfileRequest() =>
        new("Valid Player", new DateOnly(1998, 10, 15), "Female",
            "https://example.test/avatar.jpg", "Short bio");

    private static UpdateProfileRequest InvalidProfileRequest(string invalidInput) =>
        invalidInput switch
        {
            "blank_name" => ValidProfileRequest() with { FullName = "   " },
            "short_name" => ValidProfileRequest() with { FullName = "A" },
            "long_name" => ValidProfileRequest() with { FullName = new string('N', 101) },
            "future_birth_date" => ValidProfileRequest() with { DateOfBirth = DateOnly.MaxValue },
            "old_birth_date" => ValidProfileRequest() with { DateOfBirth = new DateOnly(1919, 12, 31) },
            "invalid_gender" => ValidProfileRequest() with { Gender = "Unknown" },
            "long_bio" => ValidProfileRequest() with { Bio = new string('B', 501) },
            "invalid_avatar_url" => ValidProfileRequest() with { AvatarUrl = "ftp://example.test/avatar.png" },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidInput), invalidInput, "Unknown invalid input.")
        };

    private static string ExpectedProfileError(string invalidInput) =>
        invalidInput switch
        {
            "blank_name" or "short_name" or "long_name" => "invalid_full_name",
            "future_birth_date" or "old_birth_date" => "invalid_date_of_birth",
            "invalid_gender" => "invalid_gender",
            "long_bio" => "invalid_bio",
            "invalid_avatar_url" => "invalid_avatar_url",
            _ => throw new ArgumentOutOfRangeException(nameof(invalidInput), invalidInput, "Unknown invalid input.")
        };

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

    private static UserProfileDetails CreateProfile(string fullName = "Test Player") =>
        new(UserId, "player@example.test", fullName, "Player", "active", new DateOnly(1998, 10, 15),
            "Male", "https://example.test/avatar.jpg", [CreateSportProfile()], PlayerId, "Player bio");

    private static PublicPlayerProfileDetails CreatePublicProfile() =>
        new(PlayerId, UserId, "Test Player", null, "Male", new DateOnly(1998, 10, 15),
            "Player bio", false, "None", null, [CreateSportProfile()], Array.Empty<PublicRatingHistoryItem>());

    private static SportProfileItem CreateSportProfile() =>
        new("badminton", "Badminton", 72, "B4", "Provisional", 5);

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
