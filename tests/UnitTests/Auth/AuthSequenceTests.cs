using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.Auth;

public sealed class AuthSequenceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task UTC028_RejectsMissingCsrf()
    {
        output.WriteLine("SD08 AuthController.Google | Expected: HTTP 400 invalid_csrf_token; no Google challenge or account");
        await using var app = new LoginApp();
        using var client = app.Client();
        var response = await client.PostAsync("/api/auth/google", null);
        await CheckError(response, HttpStatusCode.BadRequest, "invalid_csrf_token");
        Assert.Null(response.Headers.Location);
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task UTC029_StartsGoogleChallenge()
    {
        output.WriteLine("SD08 AuthController.Google | Expected: HTTP 302 to accounts.google.com; account selection prompt; no account yet");
        await using var app = new LoginApp();
        using var client = app.Client();
        var response = await Challenge(client, "player", 1);
        var redirect = response.Headers.Location!;
        Assert.Equal("accounts.google.com", redirect.Host);
        var query = QueryHelpers.ParseQuery(redirect.Query);
        Assert.Equal("select_account", query["prompt"].ToString());
        output.WriteLine($"Actual redirect host: {redirect.Host}; prompt: {query["prompt"]}");
        Assert.Equal(0, app.Store.Created);
    }

    [Fact]
    public async Task UTC030_RejectsInvalidPortal()
    {
        output.WriteLine("SD08 AuthController.Google | Expected: HTTP 400 invalid_login_portal");
        await using var app = new LoginApp();
        using var client = app.Client();
        await CheckError(await Post(client, "/api/auth/google?portal=guest"), HttpStatusCode.BadRequest, "invalid_login_portal");
    }

    [Fact]
    public async Task UTC031_RejectsWhileAlreadySignedIn()
    {
        output.WriteLine("SD08 AuthController.Google | Expected: HTTP 409 sign_out_before_switching_account");
        await using var app = new LoginApp();
        using var client = app.Client();
        await SignIn(client);
        await CheckError(await Post(client, "/api/auth/google"), HttpStatusCode.Conflict, "sign_out_before_switching_account");
    }

    [Fact]
    public async Task UTC032_CallbackIssuesCookieAndReturnsIdentity()
    {
        output.WriteLine("SD08 AuthController.Me | Expected: Session cookie Secure/HttpOnly/SameSite=Lax; Player and selected center 1; one account created");
        await using var app = new LoginApp { CentersEnabled = true };
        using var client = app.Client();
        var response = await SignIn(client, 1);
        CheckSessionCookie(response);
        var me = await Me(client);
        Assert.Equal("Player", me.GetProperty("role").GetString());
        Assert.Equal("1", me.GetProperty("selectedVenueId").GetString());
        Assert.Equal(1, app.Store.Created);
    }

    [Fact]
    public async Task UTC033_InitialUnavailableCenterDoesNotIssueSession()
    {
        output.WriteLine("SD08 AuthController.Me | Expected: Callback redirects to login error; no session cookie; /me returns HTTP 401");
        await using var app = new LoginApp { CentersEnabled = true };
        using var client = app.Client();
        var response = await Callback(client, await Challenge(client, "player", 999));
        Assert.Equal("/?loginError=1", response.Headers.Location?.ToString());
        Assert.False(HasSessionCookie(response));
        CheckStatus("GET /api/auth/me", await client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UTC034_SwitchesCenterAndReissuesSession()
    {
        output.WriteLine("SD08 AuthController.SelectCenter | Expected: HTTP 200; new cookie; selected center 2; same identity and no personnel assignment");
        await using var app = new LoginApp { CentersEnabled = true };
        using var client = app.Client();
        await SignIn(client, 1);
        var before = await Me(client);
        var response = await Post(client, "/api/auth/center", 2);
        CheckStatus("POST /api/auth/center", response, HttpStatusCode.OK);
        CheckSessionCookie(response);
        var after = await Me(client);
        Assert.Equal(before.GetProperty("id").GetString(), after.GetProperty("id").GetString());
        Assert.Equal("2", after.GetProperty("selectedVenueId").GetString());
        Assert.Null(app.Store.Account!.VenueId);
    }

    [Fact]
    public async Task UTC035_DeniesUnavailableCenterAndKeepsPreviousSelection()
    {
        output.WriteLine("SD08 AuthController.SelectCenter | Expected: HTTP 403 center_unavailable; /me stays at center 1");
        await using var app = new LoginApp { CentersEnabled = true };
        using var client = app.Client();
        await SignIn(client, 1);
        await CheckError(await Post(client, "/api/auth/center", 999), HttpStatusCode.Forbidden, "center_unavailable");
        Assert.Equal("1", (await Me(client)).GetProperty("selectedVenueId").GetString());
    }

    [Fact]
    public async Task UTC036_RequiresCsrfAndClearsSession()
    {
        output.WriteLine("SD08 AuthController.Logout | Expected: HTTP 400 without token, then 204 and expired cookie; /me returns 401");
        await using var app = new LoginApp();
        using var client = app.Client();
        await SignIn(client);
        await CheckError(await client.PostAsync("/api/auth/logout", null), HttpStatusCode.BadRequest, "invalid_csrf_token");
        var response = await Post(client, "/api/auth/logout");
        CheckStatus("POST /api/auth/logout", response, HttpStatusCode.NoContent);
        Assert.True(HasSessionCookie(response));
        Assert.Contains("expires=", response.Headers.GetValues("Set-Cookie").First(x => x.StartsWith("__Host-TKSC.Session=")), StringComparison.OrdinalIgnoreCase);
        CheckStatus("GET /api/auth/me", await client.GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UTC037_RejectsCallbackWithoutOAuthState()
    {
        output.WriteLine("SD08 AuthController.Google | Expected: HTTP 400; no account created or session cookie");
        await using var app = new LoginApp();
        using var client = app.Client();
        var response = await client.GetAsync("/signin-google");
        CheckStatus("GET /signin-google without state", response, HttpStatusCode.BadRequest);
        Assert.Equal(0, app.Store.Created);
        Assert.False(HasSessionCookie(response));
    }

    [Fact]
    public async Task UTC038_CenterSwitchRequiresCsrf()
    {
        output.WriteLine("SD08 AuthController.SelectCenter | Expected: HTTP 400 invalid_csrf_token; selection stays at center 1");
        await using var app = new LoginApp { CentersEnabled = true };
        using var client = app.Client();
        await SignIn(client, 1);
        await CheckError(await client.PostAsJsonAsync("/api/auth/center", new { venueId = 2 }), HttpStatusCode.BadRequest, "invalid_csrf_token");
        Assert.Equal("1", (await Me(client)).GetProperty("selectedVenueId").GetString());
    }

    private async Task<HttpResponseMessage> Challenge(HttpClient client, string? portal = null, long? venue = null)
    {
        var args = new Dictionary<string, string?>();
        if (portal is not null) args["portal"] = portal;
        if (venue is not null) args["venueId"] = venue.Value.ToString();
        var path = QueryHelpers.AddQueryString("/api/auth/google", args);
        var response = await Post(client, path);
        CheckStatus($"POST {path}", response, HttpStatusCode.Redirect);
        return response;
    }

    private async Task<HttpResponseMessage> Callback(HttpClient client, HttpResponseMessage challenge)
    {
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        var response = await client.GetAsync("/signin-google?code=test-code&state=" + Uri.EscapeDataString(state));
        CheckStatus("GET /signin-google with stub code and state", response, HttpStatusCode.Redirect);
        output.WriteLine($"Actual callback redirect: {response.Headers.Location}");
        return response;
    }

    private async Task<HttpResponseMessage> SignIn(HttpClient client, long? venue = null) =>
        await Callback(client, await Challenge(client, venue is null ? null : "player", venue));

    private async Task<HttpResponseMessage> Post(HttpClient client, string path, long? venue = null)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        var token = csrf.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        output.WriteLine($"Input: POST {path}; CSRF present; venueId={venue?.ToString() ?? "N/A"}");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token);
        if (venue.HasValue) request.Content = JsonContent.Create(new { venueId = venue.Value });
        return await client.SendAsync(request);
    }

    private void CheckStatus(string endpoint, HttpResponseMessage response, HttpStatusCode expected)
    {
        output.WriteLine($"Input: {endpoint}");
        output.WriteLine($"Actual: HTTP {(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(expected, response.StatusCode);
    }

    private async Task CheckError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        CheckStatus(response.RequestMessage!.RequestUri!.PathAndQuery, response, status);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        output.WriteLine($"Actual error code: {body.GetProperty("code").GetString()}");
        Assert.Equal(code, body.GetProperty("code").GetString());
    }

    private async Task<JsonElement> Me(HttpClient client)
    {
        var response = await client.GetAsync("/api/auth/me");
        CheckStatus("GET /api/auth/me", response, HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        output.WriteLine($"Actual identity: {JsonSerializer.Serialize(new
        {
            id = data.GetProperty("id"), role = data.GetProperty("role"),
            selectedVenueId = data.GetProperty("selectedVenueId")
        })}");
        return data;
    }

    private static bool HasSessionCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
        && values.Any(x => x.StartsWith("__Host-TKSC.Session=", StringComparison.Ordinal));

    private void CheckSessionCookie(HttpResponseMessage response)
    {
        Assert.True(HasSessionCookie(response));
        var header = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("__Host-TKSC.Session="));
        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
        output.WriteLine("Actual session cookie: Secure, HttpOnly, SameSite=Lax");
    }
}
