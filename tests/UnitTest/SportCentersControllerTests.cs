using System.Reflection;
using be_da.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class SportCentersControllerTests
{
    [Fact]
    public void Controller_RequiresAuthentication()
    {
        Assert.Contains(
            typeof(SportCentersController).GetCustomAttributes<AuthorizeAttribute>(),
            attribute => attribute.Roles is null);
    }

    [Theory]
    [InlineData("Owner", true)]
    [InlineData("Manager", true)]
    [InlineData("Staff", true)]
    [InlineData("Referee", true)]
    [InlineData("Player", false)]
    [InlineData("SystemAdmin", false)]
    public void List_DeclaresOnlyVenuePersonnelRoles(string role, bool expected)
    {
        var authorize = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.List))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();
        var allowedRoles = authorize.Roles!.Split(',');

        Assert.Equal(expected, allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Personnel_RequiresOwnerRole()
    {
        var authorize = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.Personnel))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Owner,owner", authorize.Roles);
    }

    [Fact]
    public void Assign_RequiresOwnerRole()
    {
        var authorize = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.Assign))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Owner,owner", authorize.Roles);
    }

    [Fact]
    public void Actions_DeclareExpectedRoutesAndHttpMethods()
    {
        var controllerRoute = typeof(SportCentersController)
            .GetCustomAttribute<RouteAttribute>();
        var listRoute = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.List))!
            .GetCustomAttribute<HttpGetAttribute>();
        var personnelRoute = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.Personnel))!
            .GetCustomAttribute<HttpGetAttribute>();
        var assignRoute = typeof(SportCentersController)
            .GetMethod(nameof(SportCentersController.Assign))!
            .GetCustomAttribute<HttpPutAttribute>();

        Assert.Equal("api/v1/sport-centers", controllerRoute?.Template);
        Assert.NotNull(listRoute);
        Assert.Null(listRoute.Template);
        Assert.Equal("personnel", personnelRoute?.Template);
        Assert.Equal("personnel/{id:long}", assignRoute?.Template);
    }

    [Fact]
    public async Task Assign_WhenCsrfValidationFails_ReturnsBadRequestWithoutOpeningDatabaseConnection()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(service => service.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        var controller = new SportCentersController(
            new ConfigurationBuilder().Build(),
            antiforgery.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.Assign(
            42,
            new SportCentersController.AssignRequest(7),
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", badRequest.Value!.GetType()
            .GetProperty("code")!.GetValue(badRequest.Value));
        antiforgery.Verify(service => service.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
    }
}
