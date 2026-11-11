using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Sessions;
using be_da.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TKSC.Application.DTOs;
using TKSC.Controllers;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class LateEntryControllerTests
{
    private const long UserId = 42;
    private const long VenueId = 7;
    private const long SessionId = 19;
    private const long Revision = 60000;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task List_ReturnsOffersForRequestedFilters()
    {
        IReadOnlyList<LateEntryOffer> offers = [CreateOffer()];
        var lateEntryStore = new Mock<ILateEntryStore>();
        lateEntryStore.Setup(x => x.List(VenueId, SessionId, false, TestCancellationToken)).ReturnsAsync(offers);
        var controller = CreateController(lateEntryStore);

        var result = await controller.List(VenueId, SessionId, TestCancellationToken);

        Assert.Same(offers, Assert.IsType<OkObjectResult>(result).Value);
        lateEntryStore.Verify(x => x.List(VenueId, SessionId, false, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task List_AllowsMissingOptionalFilters()
    {
        IReadOnlyList<LateEntryOffer> offers = Array.Empty<LateEntryOffer>();
        var lateEntryStore = new Mock<ILateEntryStore>();
        lateEntryStore.Setup(x => x.List(null, null, false, TestCancellationToken)).ReturnsAsync(offers);
        var controller = CreateController(lateEntryStore);

        var result = await controller.List(null, null, TestCancellationToken);

        Assert.Same(offers, Assert.IsType<OkObjectResult>(result).Value);
        lateEntryStore.Verify(x => x.List(null, null, false, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Manage_UsesAssignedVenueAndIgnoresCallerSuppliedVenue()
    {
        IReadOnlyList<LateEntryOffer> offers = [CreateOffer()];
        var lateEntryStore = new Mock<ILateEntryStore>();
        lateEntryStore.Setup(x => x.List(VenueId, null, true, TestCancellationToken)).ReturnsAsync(offers);
        var controller = CreateController(lateEntryStore, assignedVenueId: VenueId);
        controller.Request.QueryString = new QueryString("?venueId=999");

        var result = await controller.Manage(TestCancellationToken);

        Assert.Same(offers, Assert.IsType<OkObjectResult>(result).Value);
        lateEntryStore.Verify(x => x.List(VenueId, null, true, TestCancellationToken), Times.Once);
        lateEntryStore.Verify(x => x.List(999, null, true, TestCancellationToken), Times.Never);
    }

    [Fact]
    public async Task Manage_WhenNoVenueIsAssigned_ThrowsPaymentException()
    {
        var lateEntryStore = new Mock<ILateEntryStore>();
        var controller = CreateController(lateEntryStore);

        var exception = await Assert.ThrowsAsync<PaymentException>(() => controller.Manage(TestCancellationToken));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        lateEntryStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Hold_WhenCsrfIsValid_RegistersUsingAuthenticatedUserAndRevision()
    {
        var holdResult = new RegisterSessionResult(true, null, "OK", OrderId: 123, AmountVnd: 60000);
        var sessions = new Mock<ISessionStore>();
        sessions.Setup(x => x.RegisterLateEntryAsync(UserId, SessionId, Revision, TestCancellationToken))
            .ReturnsAsync(holdResult);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(sessions: sessions, antiforgery: antiforgery, userIdClaim: UserId.ToString());

        var result = await controller.Hold(SessionId, new HoldLateEntryRequest(Revision), TestCancellationToken);

        Assert.Same(holdResult, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        sessions.Verify(x => x.RegisterLateEntryAsync(UserId, SessionId, Revision, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Hold_WhenRegistrationFails_ReturnsConflictWithStoreError()
    {
        var sessions = new Mock<ISessionStore>();
        sessions.Setup(x => x.RegisterLateEntryAsync(UserId, SessionId, Revision, TestCancellationToken))
            .ReturnsAsync(new RegisterSessionResult(false, "session_full", "No places remain."));
        var controller = CreateController(sessions: sessions, userIdClaim: UserId.ToString());

        var result = await controller.Hold(SessionId, new HoldLateEntryRequest(Revision), TestCancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("session_full", GetProperty<string>(conflict.Value, "code"));
        Assert.Equal("No places remain.", GetProperty<string>(conflict.Value, "message"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-number")]
    public async Task Hold_WhenAuthenticatedUserIdIsInvalid_ThrowsUnauthorizedPaymentException(string? userIdClaim)
    {
        var sessions = new Mock<ISessionStore>();
        var controller = CreateController(sessions: sessions, userIdClaim: userIdClaim);

        var exception = await Assert.ThrowsAsync<PaymentException>(
            () => controller.Hold(SessionId, new HoldLateEntryRequest(Revision), TestCancellationToken));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        sessions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Hold_WhenCsrfValidationFails_ThrowsBadRequestPaymentExceptionWithoutRegistering()
    {
        var sessions = new Mock<ISessionStore>();
        var controller = CreateController(
            sessions: sessions,
            antiforgery: InvalidAntiforgery(),
            userIdClaim: UserId.ToString());

        var exception = await Assert.ThrowsAsync<PaymentException>(
            () => controller.Hold(SessionId, new HoldLateEntryRequest(Revision), TestCancellationToken));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("Phiên bảo mật đã thay đổi. Vui lòng tải lại.", exception.Message);
        sessions.VerifyNoOtherCalls();
    }

    private static LateEntryController CreateController(
        Mock<ILateEntryStore>? lateEntryStore = null,
        Mock<ISessionStore>? sessions = null,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = null,
        long? assignedVenueId = null)
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));
        if (assignedVenueId.HasValue)
            context.Items["SportCenterId"] = assignedVenueId.Value;

        return new LateEntryController(
            (lateEntryStore ?? new Mock<ILateEntryStore>()).Object,
            (sessions ?? new Mock<ISessionStore>()).Object,
            (antiforgery ?? ValidAntiforgery()).Object)
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

    private static LateEntryOffer CreateOffer() =>
        new(SessionId, VenueId, "Test Venue", "Court 1", "Badminton", "B3",
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2),
            8, 3, 3, 0, 5, 5, 60000, true, Revision);

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
