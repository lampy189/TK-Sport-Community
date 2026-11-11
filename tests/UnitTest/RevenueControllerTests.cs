using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Revenue;
using be_da.Authorization;
using be_da.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class RevenueControllerTests
{
    private static readonly DateOnly From = new(2026, 11, 1);
    private static readonly DateOnly To = new(2026, 11, 3);
    private const long VenueId = 7;

    [Theory]
    [InlineData(1000, 1000, true)]
    [InlineData(1000, 1365, true)]
    [InlineData(1000, 1366, false)]
    public void ValidRange_EnforcesInclusiveMaximumOf366Days(int fromOffset, int toOffset, bool expected)
    {
        var from = DateOnly.FromDayNumber(fromOffset);
        var to = DateOnly.FromDayNumber(toOffset);

        Assert.Equal(expected, RevenueController.ValidRange(from, to));
    }

    [Fact]
    public void ValidRange_RejectsDefaultOrMaximumDate()
    {
        Assert.False(RevenueController.ValidRange(default, To));
        Assert.False(RevenueController.ValidRange(From, DateOnly.MaxValue));
    }

    [Fact]
    public async Task Daily_WhenDateRangeIsInvalid_ReturnsBadRequestWithoutQueryingStore()
    {
        var store = new Mock<IRevenueStore>();
        var controller = CreateController(store, "Owner");

        var result = await controller.Daily(default, To, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Staff")]
    public async Task Daily_WhenNonOwnerHasNoAssignedVenue_ReturnsForbid(string role)
    {
        var store = new Mock<IRevenueStore>();
        var controller = CreateController(store, role);

        var result = await controller.Daily(From, To, null, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Daily_WhenNonOwnerRequestsAnotherVenue_ReturnsForbid()
    {
        var store = new Mock<IRevenueStore>();
        var controller = CreateController(store, "Manager", VenueId);

        var result = await controller.Daily(From, To, VenueId + 1, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Daily_WhenManagerRequestsAssignedVenue_ReturnsDailyRowsAndCalculatedTotals()
    {
        var entries = new[]
        {
            new RevenueEntry(From, 1000, 500, 100),
            new RevenueEntry(To, 2000, 300, 400)
        };
        var store = new Mock<IRevenueStore>();
        store.Setup(x => x.GetDailyAsync(From, To, VenueId, CancellationToken.None))
            .ReturnsAsync(new RevenueReportData(true, entries, 2));
        var controller = CreateController(store, "Manager", VenueId);

        var result = await controller.Daily(From, To, null, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(response.Value);
        Assert.Equal(VenueId, GetProperty<long?>(response.Value!, "venueId"));
        Assert.Equal("Asia/Ho_Chi_Minh", GetProperty<string>(response.Value!, "timeZone"));
        var days = GetProperty<object[]>(response.Value!, "days");
        Assert.Equal(3, days.Length);
        Assert.Equal(From.AddDays(1), GetProperty<DateOnly>(days[1], "Date"));
        Assert.Equal(0L, GetProperty<long>(days[1], "Social"));
        Assert.Equal(From, GetProperty<DateOnly>(days[0], "Date"));
        Assert.Equal(1400L, GetProperty<long>(days[0], "Net"));
        var totals = GetProperty<object>(response.Value!, "totals");
        Assert.Equal(3000L, GetProperty<long>(totals, "social"));
        Assert.Equal(800L, GetProperty<long>(totals, "goods"));
        Assert.Equal(500L, GetProperty<long>(totals, "refunds"));
        Assert.Equal(3300L, GetProperty<long>(totals, "net"));
        Assert.Equal(2L, GetProperty<long>(response.Value!, "unpostedPayments"));
        store.Verify(x => x.GetDailyAsync(From, To, VenueId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Daily_WhenVenueDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IRevenueStore>();
        store.Setup(x => x.GetDailyAsync(From, To, VenueId, CancellationToken.None))
            .ReturnsAsync(new RevenueReportData(false, Array.Empty<RevenueEntry>(), 0));
        var controller = CreateController(store, "Owner");

        var result = await controller.Daily(From, To, VenueId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Daily_WhenOwnerDoesNotSelectVenue_QueriesAllVenues()
    {
        var store = new Mock<IRevenueStore>();
        store.Setup(x => x.GetDailyAsync(From, To, null, CancellationToken.None))
            .ReturnsAsync(new RevenueReportData(true, Array.Empty<RevenueEntry>(), 0));
        var controller = CreateController(store, "Owner");

        var result = await controller.Daily(From, To, null, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetDailyAsync(From, To, null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Daily_WhenOwnerSelectsVenue_QueriesSelectedVenue()
    {
        var store = new Mock<IRevenueStore>();
        store.Setup(x => x.GetDailyAsync(From, To, VenueId, CancellationToken.None))
            .ReturnsAsync(new RevenueReportData(true, Array.Empty<RevenueEntry>(), 0));
        var controller = CreateController(store, "Owner");

        var result = await controller.Daily(From, To, VenueId, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        store.Verify(x => x.GetDailyAsync(From, To, VenueId, CancellationToken.None), Times.Once);
    }

    private static RevenueController CreateController(
        Mock<IRevenueStore> store,
        string role,
        long? assignedVenueId = null)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Role, role)], "UnitTest"))
        };
        if (assignedVenueId.HasValue)
            context.Items["SportCenterId"] = assignedVenueId.Value;

        return new RevenueController(store.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static T GetProperty<T>(object? value, string name) =>
        (T)value!.GetType().GetProperty(name)!.GetValue(value)!;
}
