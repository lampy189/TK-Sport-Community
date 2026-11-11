using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Sessions;
using be_da.Controllers;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class SessionControllerTests
{
    private const long UserId = 42;
    private const long SessionId = 17;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task GetTimeSlots_ReturnsConfiguredTimeSlots()
    {
        IReadOnlyList<TimeSlotSummary> timeSlots = [new(1, new TimeSpan(18, 0, 0), new TimeSpan(20, 0, 0), "18:00-20:00")];
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetTimeSlotsAsync(TestCancellationToken)).ReturnsAsync(timeSlots);

        var result = await CreateController(store).GetTimeSlots(TestCancellationToken);

        Assert.Same(timeSlots, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetTimeSlotsAsync(TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetVenues_ReturnsAvailableVenues()
    {
        IReadOnlyList<VenueSummary> venues = [new(3, "TK Sport", "District 1")];
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetVenuesAsync(TestCancellationToken)).ReturnsAsync(venues);

        var result = await CreateController(store).GetVenues(TestCancellationToken);

        Assert.Same(venues, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetVenuesAsync(TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessions_PassesAllFiltersAndCurrentUserToStore()
    {
        var filter = new SessionFilter(5, "badminton", new DateOnly(2026, 11, 12), 2, "published",
            new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 14), 3, "TK Sport", "Social");
        IReadOnlyList<SessionSummary> sessions = [CreateSession()];
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetSessionsAsync(filter, UserId, TestCancellationToken)).ReturnsAsync(sessions);

        var result = await CreateController(store).GetSessions(
            filter.SportId, filter.Sport, filter.Date, filter.TimeSlotId, filter.Status,
            filter.FromDate, filter.ToDate, filter.VenueId, filter.Venue, filter.SessionType,
            TestCancellationToken);

        Assert.Same(sessions, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSessionsAsync(filter, UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessions_ForAnonymousViewerPassesNullUserId()
    {
        var filter = new SessionFilter();
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetSessionsAsync(filter, null, TestCancellationToken))
            .ReturnsAsync(Array.Empty<SessionSummary>());

        var result = await CreateController(store, userIdClaim: null)
            .GetSessions(null, null, null, null, null, null, null, null, null, null, TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetSessionsAsync(filter, null, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessions_WhenFromDateIsAfterToDate_ReturnsBadRequestWithoutCallingStore()
    {
        var store = new Mock<ISessionStore>();

        var result = await CreateController(store).GetSessions(
            null, null, null, null, null, new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 12),
            null, null, null, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("INVALID_DATE_RANGE", GetProperty<string>(badRequest.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSessionById_PassesCurrentUserAndReturnsSessionDetails()
    {
        var details = CreateSessionDetails();
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetSessionByIdAsync(SessionId, UserId, TestCancellationToken)).ReturnsAsync(details);

        var result = await CreateController(store).GetSessionById(SessionId, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSessionByIdAsync(SessionId, UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessionById_ForAnonymousViewerPassesNullUserId()
    {
        var details = CreateSessionDetails();
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetSessionByIdAsync(SessionId, null, TestCancellationToken)).ReturnsAsync(details);

        var result = await CreateController(store, userIdClaim: null)
            .GetSessionById(SessionId, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSessionByIdAsync(SessionId, null, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessionById_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.GetSessionByIdAsync(SessionId, UserId, TestCancellationToken))
            .ReturnsAsync((SessionDetails?)null);

        var result = await CreateController(store).GetSessionById(SessionId, TestCancellationToken);

        Assert.Equal("SESSION_NOT_FOUND",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task RegisterSession_WhenCsrfIsInvalid_ReturnsBadRequestWithoutRegistering()
    {
        var store = new Mock<ISessionStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .RegisterSession(SessionId, TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterSession_WhenUserIdClaimIsInvalid_ReturnsUnauthorizedWithoutRegistering()
    {
        var store = new Mock<ISessionStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, userIdClaim: "invalid")
            .RegisterSession(SessionId, TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterSession_WhenSuccessful_ReturnsPendingOrderAndTicketDetails()
    {
        var expiresAt = new DateTimeOffset(2026, 11, 12, 12, 15, 0, TimeSpan.Zero);
        var registration = new RegisterSessionResult(true, null, null, OrderId: 90, OrderCode: "SO-90",
            TicketId: 91, TicketCode: "TK-91", AmountVnd: 50000, ExpiresAt: expiresAt);
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.RegisterSessionAsync(UserId, SessionId, TestCancellationToken)).ReturnsAsync(registration);

        var result = await CreateController(store).RegisterSession(SessionId, TestCancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(GetProperty<bool>(ok.Value, "success"));
        Assert.Equal(90L, GetProperty<long?>(ok.Value, "orderId"));
        Assert.Equal("SO-90", GetProperty<string>(ok.Value, "orderCode"));
        Assert.Equal(91L, GetProperty<long?>(ok.Value, "ticketId"));
        Assert.Equal("TK-91", GetProperty<string>(ok.Value, "ticketCode"));
        Assert.Equal(SessionId, GetProperty<long>(ok.Value, "sessionId"));
        Assert.Equal(50000, GetProperty<int?>(ok.Value, "amountVnd"));
        Assert.Equal("Pending", GetProperty<string>(ok.Value, "status"));
        Assert.Equal(expiresAt, GetProperty<DateTimeOffset?>(ok.Value, "expiresAt"));
        store.Verify(x => x.RegisterSessionAsync(UserId, SessionId, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData("SKILL_INELIGIBLE", 400)]
    [InlineData("SESSION_FULL", 400)]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("ALREADY_REGISTERED", 409)]
    [InlineData("OTHER_ERROR", 400)]
    public async Task RegisterSession_MapsStoreErrorsToHttpStatus(string errorCode, int expectedStatus)
    {
        var registration = new RegisterSessionResult(false, errorCode, "Registration rejected", "outside_band");
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.RegisterSessionAsync(UserId, SessionId, TestCancellationToken)).ReturnsAsync(registration);

        var result = await CreateController(store).RegisterSession(SessionId, TestCancellationToken);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
        if (errorCode == "SKILL_INELIGIBLE")
            Assert.Equal("outside_band", GetProperty<string>(response.Value, "reason"));
        store.Verify(x => x.RegisterSessionAsync(UserId, SessionId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RegisterSession_WhenStoreThrows_PropagatesException()
    {
        var store = new Mock<ISessionStore>();
        store.Setup(x => x.RegisterSessionAsync(UserId, SessionId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("session registration unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).RegisterSession(SessionId, TestCancellationToken));

        Assert.Equal("session registration unavailable", exception.Message);
    }

    private static SessionController CreateController(
        Mock<ISessionStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));

        return new SessionController(store.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static SessionSummary CreateSession() =>
        new(SessionId, 5, "badminton", "Badminton", 3, "Court 1", "TK Sport", 2,
            new TimeSpan(18, 0, 0), new TimeSpan(20, 0, 0), "18:00-20:00",
            new DateOnly(2026, 11, 12), "B3", 8, 50000, "published", 1, 4, VenueId: 3);

    private static SessionDetails CreateSessionDetails() =>
        new(CreateSession(), [new SessionParticipant(91, 20, "Player One", null, "B3", "confirmed")]);

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

    private const long OrderId = 90;
}
