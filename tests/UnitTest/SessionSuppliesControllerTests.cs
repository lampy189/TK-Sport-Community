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

public sealed class SessionSuppliesControllerTests
{
    private const long StaffId = 42;
    private const long SessionId = 17;
    private const long ProductId = 8;
    private const long RecipientId = 91;
    private static readonly DateOnly TodayInVietnam = new(2026, 11, 11);
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task GetSessions_WhenDateIsOmitted_UsesVietnamToday()
    {
        IReadOnlyList<SupplySession> sessions = [CreateSession()];
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetSessionsAsync(TodayInVietnam, null, TestCancellationToken))
            .ReturnsAsync(sessions);

        var result = await CreateController(store).GetSessions(null, TestCancellationToken);

        Assert.Same(sessions, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetSessionsAsync(TodayInVietnam, null, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetSessions_WhenDateIsProvided_UsesThatDate()
    {
        var requestedDate = TodayInVietnam.AddDays(-12);
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetSessionsAsync(requestedDate, null, TestCancellationToken))
            .ReturnsAsync(Array.Empty<SupplySession>());

        var result = await CreateController(store).GetSessions(requestedDate, TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetSessionsAsync(requestedDate, null, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(-366)]
    [InlineData(366)]
    public async Task GetSessions_AcceptsDateAt366DayBoundary(int dayOffset)
    {
        var requestedDate = TodayInVietnam.AddDays(dayOffset);
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetSessionsAsync(requestedDate, null, TestCancellationToken))
            .ReturnsAsync(Array.Empty<SupplySession>());

        var result = await CreateController(store).GetSessions(requestedDate, TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetSessionsAsync(requestedDate, null, TestCancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(-367)]
    [InlineData(367)]
    public async Task GetSessions_RejectsDateMoreThan366DaysAway(int dayOffset)
    {
        var store = new Mock<ISessionSupplyStore>();
        var requestedDate = TodayInVietnam.AddDays(dayOffset);

        var result = await CreateController(store).GetSessions(requestedDate, TestCancellationToken);

        Assert.Equal("VALIDATION_FAILED",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDetails_WhenSessionExists_ReturnsDetails()
    {
        var details = CreateDetails();
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetDetailsAsync(SessionId, TestCancellationToken)).ReturnsAsync(details);

        var result = await CreateController(store).GetDetails(SessionId, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetDetailsAsync(SessionId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetDetails_WhenSessionDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.GetDetailsAsync(SessionId, TestCancellationToken))
            .ReturnsAsync((SessionSupplyDetails?)null);

        var result = await CreateController(store).GetDetails(SessionId, TestCancellationToken);

        Assert.Equal("SESSION_NOT_FOUND",
            GetProperty<string>(Assert.IsType<NotFoundObjectResult>(result).Value, "code"));
    }

    [Fact]
    public async Task Issue_WhenRequestIsValid_ReturnsUpdatedDetailsAndStaffId()
    {
        var request = new IssueSupplyRequest(ProductId, 12, RecipientId);
        var details = CreateDetails();
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IssueAsync(SessionId, request, StaffId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Ok(details));
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery)
            .Issue(SessionId, request, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.IssueAsync(SessionId, request, StaffId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Issue_WhenCsrfIsInvalid_ReturnsBadRequestWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .Issue(SessionId, ValidIssueRequest(), TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Issue_WhenStaffIdClaimIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, "invalid")
            .Issue(SessionId, ValidIssueRequest(), TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0, 12, 91)]
    [InlineData(8, 12, 0)]
    [InlineData(8, 0, 91)]
    [InlineData(8, 100001, 91)]
    public async Task Issue_WhenProductRecipientOrQuantityIsInvalid_ReturnsBadRequest(
        long productId, int quantity, long recipientId)
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store)
            .Issue(SessionId, new IssueSupplyRequest(productId, quantity, recipientId), TestCancellationToken);

        Assert.Equal("VALIDATION_FAILED",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Issue_WhenUnitNameIsTooLong_ReturnsBadRequest()
    {
        var store = new Mock<ISessionSupplyStore>();
        var request = ValidIssueRequest() with { UnitName = new string('U', 51) };

        var result = await CreateController(store).Issue(SessionId, request, TestCancellationToken);

        Assert.Equal("VALIDATION_FAILED",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("PRODUCT_NOT_FOUND", 404)]
    [InlineData("ALLOCATION_NOT_FOUND", 404)]
    [InlineData("UNIT_NOT_FOUND", 400)]
    [InlineData("QUANTITY_TOO_LARGE", 400)]
    [InlineData("INSUFFICIENT_STOCK", 409)]
    public async Task Issue_MapsStoreErrorsToHttpStatus(string errorCode, int expectedStatus)
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IssueAsync(SessionId, It.IsAny<IssueSupplyRequest>(),
                StaffId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Fail(errorCode, "Issue rejected."));

        var result = await CreateController(store)
            .Issue(SessionId, ValidIssueRequest(), TestCancellationToken);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
    }

    [Fact]
    public async Task Reconcile_WhenRequestIsValid_ReturnsUpdatedDetailsAndStaffId()
    {
        var request = new ReturnSupplyRequest(4);
        var details = CreateDetails();
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReconcileAsync(SessionId, ProductId, request, StaffId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Ok(details));
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery)
            .Reconcile(SessionId, ProductId, request, TestCancellationToken);

        Assert.Same(details, Assert.IsType<OkObjectResult>(result).Value);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.ReconcileAsync(SessionId, ProductId, request, StaffId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Reconcile_WhenCsrfIsInvalid_ReturnsBadRequestWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store, InvalidAntiforgery())
            .Reconcile(SessionId, ProductId, new ReturnSupplyRequest(4), TestCancellationToken);

        Assert.Equal("invalid_csrf_token",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reconcile_WhenStaffIdClaimIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<ISessionSupplyStore>();
        var antiforgery = ValidAntiforgery();

        var result = await CreateController(store, antiforgery, "invalid")
            .Reconcile(SessionId, ProductId, new ReturnSupplyRequest(4), TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100001)]
    public async Task Reconcile_WhenReturnedQuantityIsOutOfRange_ReturnsBadRequest(int returnedQuantity)
    {
        var store = new Mock<ISessionSupplyStore>();

        var result = await CreateController(store)
            .Reconcile(SessionId, ProductId, new ReturnSupplyRequest(returnedQuantity), TestCancellationToken);

        Assert.Equal("VALIDATION_FAILED",
            GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("SESSION_NOT_FOUND", 404)]
    [InlineData("PRODUCT_NOT_FOUND", 404)]
    [InlineData("ALLOCATION_NOT_FOUND", 404)]
    [InlineData("UNIT_NOT_FOUND", 400)]
    [InlineData("QUANTITY_TOO_LARGE", 400)]
    [InlineData("RETURN_EXCEEDS_ISSUED", 409)]
    public async Task Reconcile_MapsStoreErrorsToHttpStatus(string errorCode, int expectedStatus)
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReconcileAsync(SessionId, ProductId, It.IsAny<ReturnSupplyRequest>(),
                StaffId, TestCancellationToken))
            .ReturnsAsync(SupplyResult.Fail(errorCode, "Reconciliation rejected."));

        var result = await CreateController(store)
            .Reconcile(SessionId, ProductId, new ReturnSupplyRequest(4), TestCancellationToken);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
    }

    [Fact]
    public async Task Issue_WhenStoreThrows_PropagatesException()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.IssueAsync(SessionId, It.IsAny<IssueSupplyRequest>(),
                StaffId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("supply issue unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).Issue(SessionId, ValidIssueRequest(), TestCancellationToken));

        Assert.Equal("supply issue unavailable", exception.Message);
    }

    [Fact]
    public async Task Reconcile_WhenStoreThrows_PropagatesException()
    {
        var store = new Mock<ISessionSupplyStore>();
        store.Setup(x => x.ReconcileAsync(SessionId, ProductId, It.IsAny<ReturnSupplyRequest>(),
                StaffId, TestCancellationToken))
            .ThrowsAsync(new InvalidOperationException("supply reconciliation unavailable"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateController(store).Reconcile(
                SessionId, ProductId, new ReturnSupplyRequest(4), TestCancellationToken));

        Assert.Equal("supply reconciliation unavailable", exception.Message);
    }

    private static SessionSuppliesController CreateController(
        Mock<ISessionSupplyStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? staffIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (staffIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, staffIdClaim)], "UnitTest"));

        return new SessionSuppliesController(store.Object, (antiforgery ?? ValidAntiforgery()).Object,
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 18, 0, 0, TimeSpan.Zero)))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static IssueSupplyRequest ValidIssueRequest() => new(ProductId, 12, RecipientId);

    private static SupplySession CreateSession() =>
        new(SessionId, TodayInVietnam, "18:00-20:00", "Badminton", "Court 1", "published", 1, 0);

    private static SessionSupplyDetails CreateDetails() =>
        new(CreateSession(), [new SupplyRecipient(RecipientId, "Staff Member", "Staff")],
            [new SupplyAllocation(ProductId, "SHUTTLE", "Shuttlecock", 12, 4, RecipientId,
                "Staff Member", true, 8)]);

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
