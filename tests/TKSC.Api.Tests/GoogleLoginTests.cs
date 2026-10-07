using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using be_da.Application.Abstractions;
using be_da.Application.Accounts;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public class GoogleLoginTests
{
    [Fact]
    public async Task SwaggerLoginShowsErrorsAndReturnsNavigationOnlyAfterValidCsrf()
    {
        await using var app = new LoginApp { EnvironmentName = "Development" };
        using var client = app.Client();
        client.DefaultRequestHeaders.Add("X-TKSC-Swagger", "1");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/google", null)).StatusCode);
        var challenge = await PostWithCsrf(client, "/api/auth/google");
        Assert.Equal(HttpStatusCode.OK, challenge.StatusCode);
        var body = await challenge.Content.ReadFromJsonAsync<JsonElement>();
        var authorization = new Uri(body.GetProperty("authorizationUrl").GetString()!);
        Assert.Equal("accounts.google.com", authorization.Host);
        var query = QueryHelpers.ParseQuery(authorization.Query);
        Assert.True(query.ContainsKey("code_challenge"));
        var callback = await client.GetAsync("/signin-google?code=test-code&state=" + Uri.EscapeDataString(query["state"].ToString()));
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.Equal("/swagger/index.html?googleLogin=complete", callback.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await PostWithCsrf(client, "/api/auth/google")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostWithCsrf(client, "/api/auth/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task SwaggerHeaderDoesNotChangeProductionRedirect()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        client.DefaultRequestHeaders.Add("X-TKSC-Swagger", "1");
        Assert.Equal(HttpStatusCode.Redirect, (await PostWithCsrf(client, "/api/auth/google")).StatusCode);
    }

    [Fact]
    public async Task ManualSwaggerFormRequiresTheTokenFromTheSameBrowser()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var invalid = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = "incorrect-token" });
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/google", invalid)).StatusCode);
        using var valid = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["__RequestVerificationToken"] = csrf.GetProperty("token").GetString()! });
        var response = await client.PostAsync("/api/auth/google", valid);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("accounts.google.com", response.Headers.Location!.Host);
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task FirstGoogleLoginCreatesOnePlayerAndSubsequentLoginReusesIt()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        var response = await Login(client);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-TKSC.Session="));
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=lax", cookie);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Player", me.GetProperty("role").GetString());
        Assert.Equal(1, app.Store.Created);
        await PostWithCsrf(client, "/api/auth/logout");
        await Login(client);
        Assert.Equal(1, app.Store.Created);
    }

    [Fact]
    public async Task UnverifiedEmailNeverCreatesAccountOrCookie()
    {
        await using var app = new LoginApp { Verified = false };
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(client)).StatusCode);
        Assert.Equal(0, app.Store.Created);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task MatchingEmailDoesNotLinkOrTakeOverExistingAccount()
    {
        await using var app = new LoginApp();
        app.Store.Account = Existing(role: "Owner", subject: "different-google-id");
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(client)).StatusCode);
        Assert.Equal("different-google-id", app.Store.Account.GoogleSubject);
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task ExistingRoleIsPreservedAndRoleChangesAreReadFromDatabase()
    {
        await using var app = new LoginApp();
        app.Store.Account = Existing(role: "Manager");
        using var client = app.Client();
        await Login(client);
        Assert.Equal("Manager", (await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("role").GetString());
        app.Store.Account = app.Store.Account with { Role = "Player" };
        Assert.Equal("Player", (await client.GetFromJsonAsync<JsonElement>("/api/auth/me")).GetProperty("role").GetString());
    }

    [Theory]
    [InlineData("Inactive", false)]
    [InlineData("Active", true)]
    public async Task DisabledOrDeletedAccountsAreRejectedAtLogin(string status, bool deleted)
    {
        await using var app = new LoginApp();
        app.Store.Account = Existing() with { Status = status, IsDeleted = deleted };
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await Login(client)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task DisablingAccountRevokesExistingSessionOnNextRequest()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        await Login(client);
        app.Store.Account = app.Store.Account! with { Status = "Inactive" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task MissingOrForgedOAuthStateIsRejectedBeforeStorage()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/signin-google?code=fake&state=fake")).StatusCode);
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task LoginAndLogoutRequireCsrfAndAccountSwitchRequiresLogout()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/google", null)).StatusCode);
        await Login(client);
        Assert.Equal(HttpStatusCode.Conflict, (await PostWithCsrf(client, "/api/auth/google")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PostWithCsrf(client, "/api/auth/logout")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task MissingGoogleConfigurationReturnsSetupErrorWithoutDatabaseWrite()
    {
        await using var app = new LoginApp { Configured = false };
        using var client = app.Client();
        var response = await PostWithCsrf(client, "/api/auth/google");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task CallbackCannotBeReplayedAndReturnUrlCannotEscapeApi()
    {
        await using var app = new LoginApp();
        using var client = app.Client();
        var challenge = await PostWithCsrf(client, "/api/auth/google?returnUrl=https://evil.example");
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        var callback = "/signin-google?code=test-code&state=" + Uri.EscapeDataString(state);
        var response = await client.GetAsync(callback);
        Assert.Equal("/api/auth/me", response.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(callback)).StatusCode);
        Assert.Equal(1, app.Store.Created);
    }

    private static Account Existing(string role = "Player", string subject = "google-test-subject") =>
        new(1, "player@example.test", "Test Player", role, "Active", subject, false);

    private static async Task<HttpResponseMessage> Login(HttpClient client)
    {
        var challenge = await PostWithCsrf(client, "/api/auth/google");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        Assert.Equal("accounts.google.com", challenge.Headers.Location!.Host);
        var query = QueryHelpers.ParseQuery(challenge.Headers.Location.Query);
        Assert.True(query.ContainsKey("code_challenge"));
        return await client.GetAsync("/signin-google?code=test-code&state=" + Uri.EscapeDataString(query["state"].ToString()));
    }

    private static async Task<HttpResponseMessage> PostWithCsrf(HttpClient client, string path)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
}

internal sealed class LoginApp : WebApplicationFactory<Program>
{
    public string EnvironmentName { get; init; } = "Testing";
    public FakeAccounts Store { get; } = new();
    public bool Verified { get; init; } = true;
    public bool Configured { get; init; } = true;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = Configured ? "test-client" : null,
            ["Authentication:Google:ClientSecret"] = Configured ? "test-secret-not-real" : null
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountStore>();
            services.AddSingleton<IAccountStore>(Store);
            services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                options => options.Backchannel = new HttpClient(new GoogleStub(() => Verified)));
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
}

internal sealed class FakeAccounts : IAccountStore
{
    public Account? Account { get; set; }
    public int Created { get; private set; }
    public Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Account?.Id == id ? Account : null);
    public Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName, CancellationToken cancellationToken)
    {
        if (Account is not null && Account.GoogleSubject != subject)
            throw new SignInRejectedException("account_link_required");
        if (Account is null)
        {
            Account = new(1, email, fullName, "Player", "Active", subject, false);
            Created++;
        }
        return Task.FromResult(Account);
    }
}

internal sealed class GoogleStub(Func<bool> verified) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        object body = request.Method == HttpMethod.Post
            ? new { access_token = "fake-backchannel-token", token_type = "Bearer", expires_in = 3600 }
            : new { id = "google-test-subject", email = "player@example.test", name = "Test Player", verified_email = verified(), role = "Owner" };
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
    }
}
