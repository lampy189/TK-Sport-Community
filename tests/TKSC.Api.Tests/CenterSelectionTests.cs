using be_da.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using be_da.Application.Interfaces.Repositories;
using be_da.Presentation.Authorization;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class CenterSelectionTests
{
    [Fact]
    public async Task PlayerSelectionSurvivesCookieRefreshAndCanChangeWithoutChangingAssignment()
    {
        await using var app = new CenterApp(); using var client = app.Client();
        (await Post(client, "/api/auth/dev-login", new { email = "player@center.test", portal = "player", venueId = 1 })).EnsureSuccessStatusCode();
        var first = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("1", first.GetProperty("selectedVenueId").GetString());
        Assert.Equal("", first.GetProperty("venueId").GetString());
        (await Post(client, "/api/auth/center", new { venueId = 2 })).EnsureSuccessStatusCode();
        var second = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("2", second.GetProperty("selectedVenueId").GetString());
        Assert.Equal(first.GetProperty("id").GetString(), second.GetProperty("id").GetString());
        Assert.Null(app.Accounts.AccountsById[1].VenueId);
    }

    [Fact]
    public async Task SelectedCenterFiltersSessionsAndRejectsForgedCenterDetailAndBooking()
    {
        await using var app = new CenterApp(); using var client = app.Client();
        (await Post(client, "/api/auth/dev-login", new { email = "player@center.test", venueId = 1 })).EnsureSuccessStatusCode();
        var list = await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions");
        Assert.Equal(1, Assert.Single(list!).GetProperty("venueId").GetInt64());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/sessions?venueId=2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/sessions/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Post(client, "/api/sessions/2/register", new { })).StatusCode);
    }

    [Theory]
    [InlineData("staff@center.test")]
    [InlineData("manager@center.test")]
    [InlineData("referee@center.test")]
    public async Task PersonnelCannotSelectOrSwitchToUnassignedCenter(string email)
    {
        await using var app = new CenterApp(); using var client = app.Client();
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/auth/dev-login", new { email, portal = "staff", venueId = 2 })).StatusCode);
        (await Post(client, "/api/auth/dev-login", new { email, portal = "staff", venueId = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/auth/center", new { venueId = 2 })).StatusCode);
        var choices = await client.GetFromJsonAsync<JsonElement>("/api/auth/centers");
        Assert.Single(choices.GetProperty("centers").EnumerateArray());
    }

    [Fact]
    public async Task PlayerMustChooseActiveCenterAndSelectionNeedsCsrf()
    {
        await using var app = new CenterApp(); using var client = app.Client();
        (await Post(client, "/api/auth/dev-login", new { email = "player@center.test" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync("/api/sessions")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/center", new { venueId = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(client, "/api/auth/center", new { venueId = 999 })).StatusCode);
        (await Post(client, "/api/auth/center", new { venueId = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/sessions")).StatusCode);
    }

    [Fact]
    public async Task OwnerCanViewAllCentersOrSelectOne()
    {
        await using var app = new CenterApp(); using var client = app.Client();
        (await Post(client, "/api/auth/dev-login", new { email = "owner@center.test", portal = "staff" })).EnsureSuccessStatusCode();
        Assert.Equal(2, (await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions"))!.Count);
        (await Post(client, "/api/auth/center", new { venueId = 2 })).EnsureSuccessStatusCode();
        Assert.Single((await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions"))!);
        (await Post(client, "/api/auth/center", new { venueId = (long?)null })).EnsureSuccessStatusCode();
        Assert.Equal(2, (await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions"))!.Count);
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string path, object payload)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(payload) };
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }

    private sealed class CenterApp : WebApplicationFactory<Program>
    {
        public FakeSessionAccounts Accounts { get; } = new();
        private readonly FakeSessionStore sessions = new();
        public CenterApp()
        {
            string[] roles = ["Player", "Staff", "Manager", "Referee", "Owner"];
            for (var i = 0; i < roles.Length; i++)
            {
                var role = roles[i];
                var account = new Account(i + 1, role.ToLowerInvariant() + "@center.test", role, role, "Active", "subject-" + i, false,
                    CenterSelection.Personnel(role) ? 1 : null);
                Accounts.Accounts[account.Email] = account; Accounts.AccountsById[account.Id] = account;
            }
            for (var id = 1; id <= 2; id++)
                sessions.Sessions.Add(new SessionSummary(id, 1, "BADMINTON", "Badminton", id, "Court", "Center", 1,
                    new(8,0,0), new(10,0,0), "08:00-10:00", new(2026,11,20), null, 8, 80000, "Published", 1, 0, VenueId: id));
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string,string?> {
                ["SportCenters:Enabled"] = "true", ["SportCenters:RequireAssignment"] = "true" }));
            builder.ConfigureTestServices(s => {
                s.RemoveAll<IAccountStore>(); s.AddSingleton<IAccountStore>(Accounts);
                s.RemoveAll<ISessionStore>(); s.AddSingleton<ISessionStore>(sessions);
            });
        }
        public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions {
            BaseAddress = new Uri("https://localhost"), HandleCookies = true, AllowAutoRedirect = false });
    }
}
