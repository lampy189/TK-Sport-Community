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
using be_da.Application.Profiles;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public class ProfileTests
{
    [Fact]
    public async Task GetProfile_WithoutAuthentication_ReturnsUnauthorized()
    {
        await using var app = new ProfileTestApp();
        using var client = app.CreateAnonymousClient();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WhenAuthenticated_ReturnsProfileDetails()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/profile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDetails>();
        Assert.NotNull(profile);
        Assert.Equal(1, profile.Id);
        Assert.Equal(1, profile.PlayerId);
        Assert.Equal("player@example.test", profile.Email);
        Assert.Equal("Test Player", profile.FullName);
        Assert.Equal("Player", profile.Role);
        Assert.Equal("Active", profile.Status);
        Assert.Equal(2, profile.SportProfiles.Count);
        Assert.Equal("badminton", profile.SportProfiles[0].SportCode);
        Assert.Equal((short?)55, profile.SportProfiles[0].CurrentScore);
        Assert.Equal("pickleball", profile.SportProfiles[1].SportCode);
    }

    [Fact]
    public async Task GetPublicPlayerProfile_WhenAuthenticated_ReturnsBothSportsAndFriendshipStatus()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/profile/players/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var publicProfile = await response.Content.ReadFromJsonAsync<PublicPlayerProfileDetails>();
        Assert.NotNull(publicProfile);
        Assert.Equal(2, publicProfile.PlayerId);
        Assert.Equal("Player Two", publicProfile.FullName);
        Assert.Equal("Yêu cầu lông & Pickleball cuối tuần", publicProfile.Bio);
        Assert.Equal("None", publicProfile.FriendshipStatus);
        Assert.False(publicProfile.IsMe);
        Assert.Equal(2, publicProfile.SportProfiles.Count);
        Assert.Contains(publicProfile.SportProfiles, s => s.SportCode == "badminton" && s.CurrentBand == "B3");
        Assert.Contains(publicProfile.SportProfiles, s => s.SportCode == "pickleball" && s.CurrentBand == "B2");
        Assert.NotEmpty(publicProfile.RecentRatingHistory);
    }

    [Fact]
    public async Task GetPublicPlayerProfile_WhenPlayerNotFound_ReturnsNotFound()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/profile/players/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("player_not_found", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetSportProfiles_WhenAuthenticated_ReturnsDistinctSports()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/profile/sports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sports = await response.Content.ReadFromJsonAsync<List<SportProfileItem>>();
        Assert.NotNull(sports);
        Assert.Equal(2, sports.Count);
        Assert.Equal("badminton", sports[0].SportCode);
        Assert.Equal("pickleball", sports[1].SportCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutCsrf_ReturnsBadRequest()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateProfileRequest("New Name", new DateOnly(1995, 5, 20), "Male", "https://example.com/avatar.jpg", "Bio");
        var response = await client.PutAsJsonAsync("/api/profile/me", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_csrf_token", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithBlankName_ReturnsBadRequest()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateProfileRequest("   ", null, null, null, null);
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/me", body);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(raw.Contains("invalid_full_name") || raw.Contains("Full name"));
    }

    [Fact]
    public async Task UpdateProfile_WithFutureBirthDate_ReturnsBadRequest()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var future = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var body = new UpdateProfileRequest("Valid Name", future, null, null, null);
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/me", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_date_of_birth", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidAvatarUrl_ReturnsBadRequest()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateProfileRequest("Valid Name", null, null, "ftp://invalid-scheme/avatar.png", null);
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/me", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_avatar_url", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithValidDataAndBio_UpdatesProfileAndReturnsOk()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var newDob = new DateOnly(1998, 10, 15);
        var body = new UpdateProfileRequest(
            "Nguyen Van A",
            newDob,
            "Male",
            "https://res.cloudinary.com/tksc/image/upload/v1/avatar.jpg",
            "Thích đánh đôi cầu lông tối thứ 3 & 5"
        );
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/me", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDetails>();
        Assert.NotNull(profile);
        Assert.Equal("Nguyen Van A", profile.FullName);
        Assert.Equal(newDob, profile.DateOfBirth);
        Assert.Equal("Male", profile.Gender);
        Assert.Equal("https://res.cloudinary.com/tksc/image/upload/v1/avatar.jpg", profile.AvatarUrl);
        Assert.Equal("Thích đánh đôi cầu lông tối thứ 3 & 5", profile.Bio);
        Assert.Equal("Player", profile.Role);
        Assert.NotEmpty(profile.SportProfiles);
    }

    [Fact]
    public async Task UpdateSportProfile_WithValidScore_UpdatesTargetSportSeparately()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateSportProfileRequest(72, null, "Played recreationally for 2 years.");
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/sports/pickleball", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<SportProfileItem>();
        Assert.NotNull(updated);
        Assert.Equal("pickleball", updated.SportCode);
        Assert.Equal((short?)72, updated.CurrentScore);
        Assert.Equal("B4", updated.CurrentBand);
        Assert.Equal("Provisional", updated.RatingStatus);

        var allSportsResponse = await client.GetAsync("/api/profile/sports");
        var allSports = await allSportsResponse.Content.ReadFromJsonAsync<List<SportProfileItem>>();
        Assert.NotNull(allSports);
        var badminton = allSports.Single(s => s.SportCode == "badminton");
        Assert.Equal((short?)55, badminton.CurrentScore);
        Assert.Equal("B3", badminton.CurrentBand);
    }

    [Fact]
    public async Task UpdateSportProfile_WhenConfirmedRatingExists_RejectsOverwrite()
    {
        await using var app = new ProfileTestApp();
        app.ProfileStore.ConfirmSportRating("badminton");
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateSportProfileRequest(90, null, "Trying to boost confirmed score");
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/sports/badminton", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("confirmed_rating_cannot_be_overwritten", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task UpdateSportProfile_WithUnknownSport_ReturnsNotFound()
    {
        await using var app = new ProfileTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new UpdateSportProfileRequest(50, null, null);
        var response = await SendWithCsrf(client, HttpMethod.Put, "/api/profile/sports/tennis", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("sport_not_found", json.GetProperty("code").GetString());
    }

    private static async Task<HttpResponseMessage> SendWithCsrf(HttpClient client, HttpMethod method, string path, object body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
}

internal sealed class ProfileTestApp : WebApplicationFactory<Program>
{
    public FakeProfileStore ProfileStore { get; } = new();
    public FakeAccounts AccountStore { get; } = new();

    public ProfileTestApp()
    {
        AccountStore.Account = new Account(1, "player@example.test", "Test Player", "Player", "Active", "google-test-subject", false);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "test-client",
            ["Authentication:Google:ClientSecret"] = "test-secret-not-real"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountStore>();
            services.AddSingleton<IAccountStore>(AccountStore);
            services.RemoveAll<IProfileStore>();
            services.AddSingleton<IProfileStore>(ProfileStore);
            services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
        });
    }

    public HttpClient CreateAnonymousClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = CreateAnonymousClient();
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/google");
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        var challenge = await client.SendAsync(request);
        var query = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
        var callback = "/signin-google?code=test-code&state=" + Uri.EscapeDataString(query["state"].ToString());
        var response = await client.GetAsync(callback);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }
}

internal sealed class FakeProfileStore : IProfileStore
{
    private readonly List<SportProfileItem> _sports = new()
    {
        new("badminton", "Badminton", 55, "B3", "Provisional", 1),
        new("pickleball", "Pickleball", null, "Unrated", "Unrated", 2)
    };

    private UserProfileDetails _profile;

    public FakeProfileStore()
    {
        _profile = new(
            1,
            "player@example.test",
            "Test Player",
            "Player",
            "Active",
            new DateOnly(2000, 1, 1),
            "Other",
            "https://example.com/initial.jpg",
            _sports,
            1,
            "Player bio"
        );
    }

    public void ConfirmSportRating(string sportCode)
    {
        var index = _sports.FindIndex(s => s.SportCode == sportCode);
        if (index >= 0)
        {
            var old = _sports[index];
            _sports[index] = old with { RatingStatus = "Confirmed" };
        }
    }

    public Task<UserProfileDetails?> GetProfileAsync(long userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<UserProfileDetails?>(_profile.Id == userId ? _profile with { SportProfiles = _sports.ToList() } : null);
    }

    public Task<PublicPlayerProfileDetails?> GetPublicPlayerProfileAsync(long playerId, long? viewerUserId, CancellationToken cancellationToken)
    {
        if (playerId == 2)
        {
            var sports = new List<SportProfileItem>
            {
                new("badminton", "Badminton", 60, "B3", "Confirmed", 1),
                new("pickleball", "Pickleball", 38, "B2", "Provisional", 2)
            };
            var history = new List<PublicRatingHistoryItem>
            {
                new(501, "badminton", "Badminton", 45, 60, "B2", "B3", "OnCourt", new DateOnly(2026, 3, 25), "Cụm sân Cầu Giấy")
            };
            return Task.FromResult<PublicPlayerProfileDetails?>(new PublicPlayerProfileDetails(
                2,
                2,
                "Player Two",
                "https://example.com/p2.jpg",
                "Male",
                new DateOnly(1997, 6, 15),
                "Yêu cầu lông & Pickleball cuối tuần",
                false,
                "None",
                null,
                sports,
                history
            ));
        }

        return Task.FromResult<PublicPlayerProfileDetails?>(null);
    }

    public Task<IReadOnlyList<SportProfileItem>?> GetSportProfilesAsync(long userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<SportProfileItem>?>(_profile.Id == userId ? _sports.ToList() : null);
    }

    public Task<UserProfileDetails?> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (_profile.Id != userId) return Task.FromResult<UserProfileDetails?>(null);

        _profile = _profile with
        {
            FullName = request.FullName!.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            Bio = request.Bio is null ? _profile.Bio : (string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim()),
            SportProfiles = _sports.ToList()
        };

        return Task.FromResult<UserProfileDetails?>(_profile);
    }

    public Task<SportProfileResult> UpdateSportProfileAsync(long userId, string sportCode, UpdateSportProfileRequest request, CancellationToken cancellationToken)
    {
        var index = _sports.FindIndex(s => s.SportCode.Equals(sportCode, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return Task.FromResult(new SportProfileResult(false, "sport_not_found", $"Sport '{sportCode}' not found.", null));
        }

        var existing = _sports[index];
        if (existing.RatingStatus == "Confirmed")
        {
            return Task.FromResult(new SportProfileResult(false, "confirmed_rating_cannot_be_overwritten",
                "Confirmed skill ratings cannot be replaced by survey.", null));
        }

        var score = request.Score!.Value;
        var band = !string.IsNullOrWhiteSpace(request.Band)
            ? request.Band.Trim()
            : score switch
            {
                <= 20 => "B1",
                <= 40 => "B2",
                <= 60 => "B3",
                <= 80 => "B4",
                _ => "B5"
            };

        var updated = existing with
        {
            CurrentScore = score,
            CurrentBand = band,
            RatingStatus = "Provisional"
        };
        _sports[index] = updated;

        return Task.FromResult(new SportProfileResult(true, null, null, updated));
    }
}
