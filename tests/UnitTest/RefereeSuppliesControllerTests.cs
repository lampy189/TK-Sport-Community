using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Stock;
using be_da.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class RefereeSuppliesControllerTests
{
    private const long RefereeId = 42;
    private const long SessionId = 17;
    private const long ProductId = 8;
    private static readonly DateOnly TodayInVietnam = new(2026, 11, 11);
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task GetSessions_WhenDateIsOmitted_UsesVietnamTodayAndCurrentReferee()
    {
        IReadOnlyList<SupplySession> sessions = [CreateSession()];
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetSessionsAsync(TodayInVietnam, RefereeId, TestCancellationToken))
            .ReturnsAsync(sessions);
        var controller = CreateController(store);

        var result = await controller.GetSessions(null, TestCancellationToken);

        Assert.Same(sessions, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSessionsAsync(TodayInVietnam, RefereeId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(-366)]
    [InlineData(366)]
    public async Task GetSessions_AcceptsDateWithin366Days(int dayOffset)
    {
        var requestedDate = TodayInVietnam.AddDays(dayOffset);
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetSessionsAsync(requestedDate, RefereeId, TestCancellationToken))
            .ReturnsAsync(Array.Empty<SupplySession>());

        var result = await CreateController(store).GetSessions(requestedDate, TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetSessionsAsync(requestedDate, RefereeId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(-367)]
    [InlineData(367)]
    public async Task GetSessions_RejectsDateMoreThan366DaysAway(int dayOffset)
    {
        var store = new Mock<ISessionSupplyStore>();
        var requestedDate = TodayInVietnam.AddDays(dayOffset);

        var result = await CreateController(store).GetSessions(requestedDate, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("VALIDATION_FAILED", GetProperty<string>(badRequest.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task GetSessions_WhenRefereeIdIsMissingOrInvalid_ReturnsUnauthorized(string? refereeIdClaim)
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store, refereeIdClaim: refereeIdClaim)
            .GetSessions(null, TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetails_WhenRefereeIsAssigned_ReturnsSessionDetails()
    {
        var details = CreateDetails();
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IsRefereeAssignedAsync(SessionId, RefereeId, TestCancellationToken)).ReturnsAsync(true);
        store.Setup(x => x.GetDetailsAsync(SessionId, TestCancellationToken)).ReturnsAsync(details);

        var result = await CreateController(store).GetDetails(SessionId, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.IsRefereeAssignedAsync(SessionId, RefereeId, TestCancellationToken), Times.Once);
        store.Verify(x => x.GetDetailsAsync(SessionId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDetails_WhenRefereeIsNotAssigned_ReturnsNotFoundWithoutLoadingDetails()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IsRefereeAssignedAsync(SessionId, RefereeId, TestCancellationToken)).ReturnsAsync(false);

        var result = await CreateController(store).GetDetails(SessionId, TestCancellationToken);

        Assert.Equal("SESSION_NOT_FOUND",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
        store.Verify(x => x.GetDetailsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDetails_WhenAssignedSessionHasNoDetails_ReturnsNotFound()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IsRefereeAssignedAsync(SessionId, RefereeId, TestCancellationToken)).ReturnsAsync(true);
        store.Setup(x => x.GetDetailsAsync(SessionId, TestCancellationToken))
            .ReturnsAsync((SessionSupplyDetails?)null);

        var result = await CreateController(store).GetDetails(SessionId, TestCancellationToken);

        Assert.Equal("SESSION_NOT_FOUND",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task GetDetails_WhenRefereeIdIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store, refereeIdClaim: "invalid")
            .GetDetails(SessionId, TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReportUse_WhenRequestIsValid_ReturnsUpdatedSessionDetails()
    {
        var request = new ReportSupplyUseRequest(3);
        var details = CreateDetails();
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReportUseAsync(SessionId, ProductId, request, RefereeId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Ok(details));
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery)
            .ReportUse(SessionId, ProductId, request, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.ReportUseAsync(SessionId, ProductId, request, RefereeId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReportUse_WhenCsrfIsInvalid_ReturnsBadRequestWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();
        var request = new ReportSupplyUseRequest(3);

        var result = await CreateController(store, InvalidAntiforgery())
            .ReportUse(SessionId, ProductId, request, TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReportUse_WhenRefereeIdIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, "invalid")
            .ReportUse(SessionId, ProductId, new ReportSupplyUseRequest(3), TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100001)]
    public async Task ReportUse_WhenUsedQuantityIsOutOfRange_ReturnsBadRequest(int usedQuantity)
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store)
            .ReportUse(SessionId, ProductId, new ReportSupplyUseRequest(usedQuantity), TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("VALIDATION_FAILED", GetProperty<string>(badRequest.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("NOT_ASSIGNED")]
    [InlineData("ALLOCATION_NOT_FOUND")]
    public async Task ReportUse_WhenAssignmentOrAllocationIsMissing_ReturnsNotFound(string errorCode)
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReportUseAsync(SessionId, ProductId, It.IsAny<ReportSupplyUseRequest>(),
                RefereeId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Fail(errorCode, "The allocation is unavailable."));

        var result = await CreateController(store)
            .ReportUse(SessionId, ProductId, new ReportSupplyUseRequest(3), TestCancellationToken);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(errorCode, GetProperty<string>(notFound.Value, "code"));
    }

    [Fact]
    public async Task ReportUse_WhenUseExceedsIssuedQuantity_ReturnsConflict()
    {
        const string errorCode = "USE_EXCEEDS_ISSUED";
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReportUseAsync(SessionId, ProductId, It.IsAny<ReportSupplyUseRequest>(),
                RefereeId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Fail(errorCode, "Reported use exceeds the issued amount."));

        var result = await CreateController(store)
            .ReportUse(SessionId, ProductId, new ReportSupplyUseRequest(3), TestCancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(errorCode, GetProperty<string>(conflict.Value, "code"));
    }

    [Fact]
    public async Task ReportUse_WhenStoreThrows_PropagatesException()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReportUseAsync(SessionId, ProductId, It.IsAny<ReportSupplyUseRequest>(),
                RefereeId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("supply usage unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).ReportUse(
                SessionId, ProductId, new ReportSupplyUseRequest(3), TestCancellationToken));

        Assert.Equal("supply usage unavailable", exception.Message);
    }

    private static RefereeSuppliesController CreateController(
        Mock<ISessionSupplyStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? refereeIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (refereeIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, refereeIdClaim)], "UnitTest"));

        return new RefereeSuppliesController(store.Object, (antiforgery ?? ValidAntiforgery()).Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.Zero)))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static SupplySession CreateSession() =>
        new(SessionId, TodayInVietnam, "18:00-20:00", "Badminton", "Court 1", "published", 1, 0);

    private static SessionSupplyDetails CreateDetails() =>
        new(CreateSession(), [new SupplyRecipient(91, "Assigned Referee", "Referee")],
            [new SupplyAllocation(ProductId, "SHUTTLE", "Shuttlecock", 6, 0, 91,
                "Assigned Referee", false, 3)]);

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
