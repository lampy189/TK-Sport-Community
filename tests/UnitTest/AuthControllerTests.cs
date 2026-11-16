using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Accounts;
using be_da.Authorization;
using be_da.Controllers;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

//dotnet test tests/TKSC.Api.Tests/TKSC.Api.Tests.csproj --filter AuthControllerTests

public class AuthControllerTests
{
    [Fact]
    public void Csrf_ShouldReturnToken()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(new AntiforgeryTokenSet("request-token", "cookie-token", "X-CSRF-TOKEN", "__RequestVerificationToken"));

        var controller = CreateController(antiforgery.Object, environmentName: "Development");

        var result = controller.Csrf();

        var ok = Assert.IsType<OkObjectResult>(result);
        var token = ok.Value?.GetType().GetProperty("token")?.GetValue(ok.Value)?.ToString();
        Assert.Equal("request-token", token);
    }

    [Fact]
    public async Task Google_WhenCsrfInvalid_ShouldReturnBadRequest()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("invalid"));

        var controller = CreateController(antiforgery.Object, environmentName: "Development");

        var result = await controller.Google();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", badRequest.Value?.GetType().GetProperty("code")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task Google_WhenAlreadyAuthenticated_ShouldReturnConflict()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var controller = CreateController(
            antiforgery.Object,
            environmentName: "Development",
            principal: CreatePrincipal("Player"));

        var result = await controller.Google();

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("sign_out_before_switching_account", conflict.Value?.GetType().GetProperty("code")?.GetValue(conflict.Value));
    }

    [Fact]
    public async Task Google_WhenGoogleConfigMissing_ShouldReturnServiceUnavailable()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = null,
                ["Authentication:Google:ClientSecret"] = null
            })
            .Build();

        var controller = CreateController(antiforgery.Object, configuration, "Development");

        var result = await controller.Google();

        var serviceUnavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, serviceUnavailable.StatusCode);
        Assert.Equal("google_login_not_configured", serviceUnavailable.Value?.GetType().GetProperty("code")?.GetValue(serviceUnavailable.Value));
    }

    [Fact]
    public async Task Google_WhenPortalInvalid_ShouldReturnBadRequest()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var configuration = BuildConfiguration("client-id", "client-secret");
        var controller = CreateController(antiforgery.Object, configuration, "Development");

        var result = await controller.Google("invalid-portal");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_login_portal", badRequest.Value?.GetType().GetProperty("code")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task Google_WhenRequestIsValid_ShouldReturnChallengeResult()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var configuration = BuildConfiguration("client-id", "client-secret");
        var controller = CreateController(antiforgery.Object, configuration, "Development");

        var result = await controller.Google("player");

        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(GoogleDefaults.AuthenticationScheme, challenge.AuthenticationSchemes.Single());
        Assert.Equal("http://localhost:5173/", challenge.Properties?.RedirectUri);
        Assert.Equal("player", challenge.Properties?.Items["login_portal"]);
    }

    [Fact]
    public void Me_ShouldReturnCurrentUserData()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Email, "player@example.test"),
            new Claim(ClaimTypes.Name, "Test Player"),
            new Claim(ClaimTypes.Role, "Player")
        ], "TestAuth"));

        var controller = CreateController(principal: principal);

        var result = controller.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("42", ok.Value?.GetType().GetProperty("id")?.GetValue(ok.Value));
        Assert.Equal("player@example.test", ok.Value?.GetType().GetProperty("email")?.GetValue(ok.Value));
        Assert.Equal("Test Player", ok.Value?.GetType().GetProperty("fullName")?.GetValue(ok.Value));
        Assert.Equal("Player", ok.Value?.GetType().GetProperty("role")?.GetValue(ok.Value));
    }

    [Fact]
    public async Task Logout_WhenCsrfInvalid_ShouldReturnBadRequest()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("invalid"));

        var controller = CreateController(antiforgery.Object, environmentName: "Development");

        var result = await controller.Logout();

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", badRequest.Value?.GetType().GetProperty("code")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task Logout_WhenValid_ShouldSignOutAndReturnNoContent()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(x => x.SignOutAsync(It.IsAny<HttpContext>(), AccountPrincipal.Scheme, It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(antiforgery.Object, authenticationService.Object, environmentName: "Development");

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        authenticationService.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), AccountPrincipal.Scheme, It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task DevLogin_WhenProduction_ShouldReturnNotFound()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var controller = CreateController(antiforgery.Object, environmentName: "Production");
        var store = new Mock<IAccountStore>();

        var result = await controller.DevLogin(new AuthController.DevLoginRequest("player@example.test"), store.Object, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DevLogin_WhenEmailMissing_ShouldReturnBadRequest()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var controller = CreateController(antiforgery.Object, environmentName: "Development");
        var store = new Mock<IAccountStore>();

        var result = await controller.DevLogin(new AuthController.DevLoginRequest("   "), store.Object, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("email_required", badRequest.Value?.GetType().GetProperty("code")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task DevLogin_WhenAccountMissing_ShouldReturnNotFound()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var controller = CreateController(antiforgery.Object, environmentName: "Development");
        var accountStore = new Mock<IAccountStore>();
        accountStore
            .Setup(x => x.FindByEmailAsync("player@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await controller.DevLogin(new AuthController.DevLoginRequest("player@example.test"), accountStore.Object, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("user_not_found", notFound.Value?.GetType().GetProperty("code")?.GetValue(notFound.Value));
    }

    [Fact]
    public async Task DevLogin_WhenAccountCannotSignIn_ShouldReturnForbid()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var account = new Account(1, "player@example.test", "Test Player", "Player", "Inactive", "google-subject", false);
        var accountStore = new Mock<IAccountStore>();
        accountStore
            .Setup(x => x.FindByEmailAsync("player@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var controller = CreateController(antiforgery.Object, environmentName: "Development");

        var result = await controller.DevLogin(new AuthController.DevLoginRequest("player@example.test"), accountStore.Object, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DevLogin_WhenValid_ShouldReturnOkAndSignIn()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var account = new Account(1, "player@example.test", "Test Player", "Player", "Active", "google-subject", false);
        var accountStore = new Mock<IAccountStore>();
        accountStore
            .Setup(x => x.FindByEmailAsync("player@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var authenticationService = new Mock<IAuthenticationService>();
        authenticationService
            .Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), AccountPrincipal.Scheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(antiforgery.Object, authenticationService.Object, environmentName: "Development");

        var result = await controller.DevLogin(new AuthController.DevLoginRequest("player@example.test", "player"), accountStore.Object, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("player@example.test", ok.Value?.GetType().GetProperty("email")?.GetValue(ok.Value));
        Assert.Equal("Player", ok.Value?.GetType().GetProperty("role")?.GetValue(ok.Value));
        authenticationService.Verify(x => x.SignInAsync(It.IsAny<HttpContext>(), AccountPrincipal.Scheme, It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    private static AuthController CreateController(
        IAntiforgery? antiforgery = null,
        IConfiguration? configuration = null,
        string environmentName = "Development",
        ClaimsPrincipal? principal = null,
        IAuthenticationService? authenticationService = null)
    {
        antiforgery ??= new Mock<IAntiforgery>().Object;
        configuration ??= BuildConfiguration("client-id", "client-secret");

        var services = new ServiceCollection();
        if (authenticationService is not null)
        {
            services.AddSingleton(authenticationService);
        }

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        if (principal is not null)
        {
            httpContext.User = principal;
        }

        return new AuthController(configuration, antiforgery, environment.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static AuthController CreateController(
        IAntiforgery antiforgery,
        IAuthenticationService authenticationService,
        string environmentName)
        => CreateController(antiforgery, BuildConfiguration("client-id", "client-secret"), environmentName, null, authenticationService);

    private static IConfiguration BuildConfiguration(string clientId, string clientSecret)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = clientId,
                ["Authentication:Google:ClientSecret"] = clientSecret
            })
            .Build();

    private static ClaimsPrincipal CreatePrincipal(string role)
        => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "99"),
            new Claim(ClaimTypes.Email, "player@example.test"),
            new Claim(ClaimTypes.Name, "Test Player"),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));
}
