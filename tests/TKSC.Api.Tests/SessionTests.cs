using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using be_da.Application.Abstractions;
using be_da.Application.Accounts;
using be_da.Application.Sessions;
using be_da.Controllers;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SessionTests
{
    [Fact]
    public async Task GetTimeSlots_Anonymous_ReturnsConfiguredVenueTimeSlotsOrderedByStartTime()
    {
        await using var app = new SessionApp();
        using var client = app.Client();

        var response = await client.GetAsync("/api/sessions/time-slots");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var slots = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(slots);
        Assert.Equal(6, slots.Count);
        Assert.Equal(1, slots[0].GetProperty("id").GetInt64());
        Assert.Equal("08:00 - 10:00", slots[0].GetProperty("label").GetString());
        Assert.Equal(6, slots[5].GetProperty("id").GetInt64());
        Assert.Equal("20:00 - 22:00", slots[5].GetProperty("label").GetString());
    }

    [Fact]
    public async Task BrowseSessions_FilterByDateRange_ReturnsSessionsWithinWeek()
    {
        await using var app = new SessionApp();
        var monday = new DateOnly(2026, 10, 19);
        var tuesday = new DateOnly(2026, 10, 20);
        var sunday = new DateOnly(2026, 10, 25);
        var nextWeek = new DateOnly(2026, 10, 26);

        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B2", 8, 2, date: tuesday));
        app.SessionStore.Sessions.Add(CreateTestSession(2, "Pickleball", "B1", 4, 1, date: sunday, sportId: 2));
        app.SessionStore.Sessions.Add(CreateTestSession(3, "Badminton", "B1", 8, 0, date: nextWeek));

        using var client = app.Client();
        var sessions = await client.GetFromJsonAsync<List<JsonElement>>(
            $"/api/sessions?fromDate={monday:yyyy-MM-dd}&toDate={sunday:yyyy-MM-dd}");
        Assert.NotNull(sessions);
        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, x => x.GetProperty("id").GetInt64() == 1);
        Assert.Contains(sessions, x => x.GetProperty("id").GetInt64() == 2);
    }

    [Fact]
    public async Task BrowseSessions_Anonymous_ReturnsOpenSessions_WithNullEligibility()
    {
        await using var app = new SessionApp();
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", 8, 2));

        using var client = app.Client();
        var response = await client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sessions = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        Assert.NotNull(sessions);
        Assert.Single(sessions);
        var s = sessions[0];
        Assert.Equal(1, s.GetProperty("id").GetInt64());
        Assert.Equal("Badminton", s.GetProperty("sportName").GetString());
        Assert.Equal(8, s.GetProperty("capacity").GetInt16());
        Assert.Equal(6, s.GetProperty("availableSlots").GetInt16());
        Assert.Equal(JsonValueKind.Null, s.GetProperty("isEligibleSuggestion").ValueKind);
    }

    [Fact]
    public async Task BrowseSessions_FilterBySportAndDate_ReturnsMatchingSessions()
    {
        await using var app = new SessionApp();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);

        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", 8, 0, date: today, sportId: 1));
        app.SessionStore.Sessions.Add(CreateTestSession(2, "Pickleball", "P1", 4, 0, date: today, sportId: 2));
        app.SessionStore.Sessions.Add(CreateTestSession(3, "Badminton", "B2", 6, 0, date: tomorrow, sportId: 1));

        using var client = app.Client();

        // Filter by sportId
        var resSport = await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions?sportId=2");
        Assert.NotNull(resSport);
        Assert.Single(resSport);
        Assert.Equal(2, resSport[0].GetProperty("id").GetInt64());

        // Filter by date
        var resDate = await client.GetFromJsonAsync<List<JsonElement>>($"/api/sessions?date={tomorrow:yyyy-MM-dd}");
        Assert.NotNull(resDate);
        Assert.Single(resDate);
        Assert.Equal(3, resDate[0].GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task BrowseSessions_AuthenticatedPlayer_SuggestsEligibilityCorrectly()
    {
        await using var app = new SessionApp();
        var player = new Account(10, "player@example.test", "Player One", "Player", "Active", "sub-10", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        // Player profile: Badminton band B1
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, "B1", 75, "Rated");

        // Sessions: Session 1 is B1 (match), Session 2 is B2 (mismatch)
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", 8, 0, sportId: 1));
        app.SessionStore.Sessions.Add(CreateTestSession(2, "Badminton", "B2", 8, 0, sportId: 1));

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var sessions = await client.GetFromJsonAsync<List<JsonElement>>("/api/sessions");
        Assert.NotNull(sessions);
        Assert.Equal(2, sessions.Count);

        var s1 = sessions.First(x => x.GetProperty("id").GetInt64() == 1);
        Assert.True(s1.GetProperty("isEligibleSuggestion").GetBoolean());

        var s2 = sessions.First(x => x.GetProperty("id").GetInt64() == 2);
        Assert.False(s2.GetProperty("isEligibleSuggestion").GetBoolean());
        Assert.Equal("lệch band kỹ năng", s2.GetProperty("ineligibilityReason").GetString());
    }

    [Fact]
    public async Task GetSessionById_ReturnsCourtTimeCapacityAndPublicParticipants()
    {
        await using var app = new SessionApp();
        var session = CreateTestSession(5, "Badminton", "B1", 8, 1);
        app.SessionStore.Sessions.Add(session);
        app.SessionStore.Participants[5] = new List<SessionParticipant>
        {
            new(101, 20, "Nguyễn Văn A", "https://img.example/ava.png", "B1", "Confirmed")
        };

        using var client = app.Client();
        var response = await client.GetAsync("/api/sessions/5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content.ReadFromJsonAsync<JsonElement>();
        var summary = details.GetProperty("summary");
        Assert.Equal(5, summary.GetProperty("id").GetInt64());
        Assert.Equal("Court 1", summary.GetProperty("courtName").GetString());
        Assert.Equal("TK Sport Center", summary.GetProperty("venueName").GetString());
        Assert.Equal("16:00 - 18:00", summary.GetProperty("timeLabel").GetString());
        Assert.Equal(8, summary.GetProperty("capacity").GetInt16());
        Assert.Equal(7, summary.GetProperty("availableSlots").GetInt16());

        var participants = details.GetProperty("publicParticipants").EnumerateArray().ToList();
        Assert.Single(participants);
        var p = participants[0];
        Assert.Equal(20, p.GetProperty("playerId").GetInt64());
        Assert.Equal("Nguyễn Văn A", p.GetProperty("fullName").GetString());
        Assert.Equal("B1", p.GetProperty("currentBand").GetString());
        Assert.False(p.TryGetProperty("email", out _));
        Assert.False(p.TryGetProperty("phone", out _));
    }

    [Fact]
    public async Task GetSessionById_NotFound_Returns404()
    {
        await using var app = new SessionApp();
        using var client = app.Client();
        var response = await client.GetAsync("/api/sessions/999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var err = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SESSION_NOT_FOUND", err.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_Unauthenticated_Returns401()
    {
        await using var app = new SessionApp();
        using var client = app.Client();
        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_NonPlayerRole_Returns403()
    {
        await using var app = new SessionApp();
        var manager = new Account(2, "manager@example.test", "Manager Lam", "Manager", "Active", "sub-2", false);
        app.AccountStore.Accounts[manager.Email] = manager;
        app.AccountStore.AccountsById[manager.Id] = manager;

        using var client = app.Client();
        await DevLogin(client, manager.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_SkillIneligible_NoProfile_Returns400_SKILL_INELIGIBLE()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        // Session requires Badminton B1, but player has no Badminton profile
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", 8, 0, sportId: 1));

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SKILL_INELIGIBLE", error.GetProperty("code").GetString());
        Assert.Equal("chưa có hồ sơ môn này", error.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Register_SkillIneligible_PendingSurvey_Returns400_SKILL_INELIGIBLE()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", 8, 0, sportId: 1));
        // Player has profile but survey pending (no band / unrated)
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, null, null, "PendingSurvey");

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SKILL_INELIGIBLE", error.GetProperty("code").GetString());
        Assert.Equal("chưa làm survey", error.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Register_SkillIneligible_BandMismatch_Returns400_SKILL_INELIGIBLE()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        // Session requires B2, player is B1
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B2", 8, 0, sportId: 1));
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, "B1", 70, "Rated");

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SKILL_INELIGIBLE", error.GetProperty("code").GetString());
        Assert.Equal("lệch band kỹ năng", error.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Register_SkillEligible_WithApprovedException_OverridesBandMismatch()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        // Session is B2, player is B1, BUT player has approved exception for Session 1
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B2", 8, 0, sportId: 1));
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, "B1", 70, "Rated");
        app.SessionStore.ApprovedExceptions.Add((1, player.Id));

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.Equal("Pending", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Register_SessionFull_Returns400_SESSION_FULL()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        // Capacity 4, Occupied 4 -> FULL
        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", capacity: 4, occupied: 4, sportId: 1));
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, "B1", 80, "Rated");

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SESSION_FULL", error.GetProperty("code").GetString());
        Assert.Contains("Buổi chơi đã đủ người", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Register_Success_CreatesPendingOrderAndTicket()
    {
        await using var app = new SessionApp();
        var player = new Account(1, "player@example.test", "Test Player", "Player", "Active", "sub-1", false);
        app.AccountStore.Accounts[player.Email] = player;
        app.AccountStore.AccountsById[player.Id] = player;

        app.SessionStore.Sessions.Add(CreateTestSession(1, "Badminton", "B1", capacity: 8, occupied: 3, sportId: 1, priceVnd: 60000));
        app.SessionStore.Profiles[(player.Id, 1)] = new PlayerSportProfileInfo(player.Id, 1, "B1", 80, "Rated");

        using var client = app.Client();
        await DevLogin(client, player.Email);

        var response = await PostWithCsrf(client, "/api/sessions/1/register");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        Assert.NotNull(body.GetProperty("orderCode").GetString());
        Assert.StartsWith("SBO-", body.GetProperty("orderCode").GetString()!);
        Assert.NotNull(body.GetProperty("ticketCode").GetString());
        Assert.StartsWith("TKT-", body.GetProperty("ticketCode").GetString()!);
        Assert.Equal("Pending", body.GetProperty("status").GetString());
        Assert.Equal(60000, body.GetProperty("amountVnd").GetInt32());
        Assert.NotNull(body.GetProperty("expiresAt").GetString());
    }

    private static SessionSummary CreateTestSession(
        long id,
        string sportName,
        string? band,
        short capacity,
        short occupied,
        long sportId = 1,
        DateOnly? date = null,
        int priceVnd = 50000)
    {
        var d = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        return new SessionSummary(
            Id: id,
            SportId: sportId,
            SportCode: sportName.ToLowerInvariant(),
            SportName: sportName,
            CourtId: 1,
            CourtName: "Court 1",
            VenueName: "TK Sport Center",
            TimeSlotId: 1,
            StartTime: new TimeSpan(16, 0, 0),
            EndTime: new TimeSpan(18, 0, 0),
            TimeLabel: "16:00 - 18:00",
            SessionDate: d,
            Band: band,
            Capacity: capacity,
            PriceVnd: priceVnd,
            Status: "Open",
            CreatedBy: 1,
            OccupiedPlaces: occupied
        );
    }

    private static async Task DevLogin(HttpClient client, string email)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/dev-login");
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        request.Content = JsonContent.Create(new { Email = email });
        var res = await client.SendAsync(request);
        res.EnsureSuccessStatusCode();
    }

    private static async Task<HttpResponseMessage> PostWithCsrf(HttpClient client, string path)
    {
        var token = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token.GetProperty("token").GetString());
        return await client.SendAsync(request);
    }
}

internal sealed class SessionApp : WebApplicationFactory<Program>
{
    public FakeSessionAccounts AccountStore { get; } = new();
    public FakeSessionStore SessionStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = "test-client",
            ["Authentication:Google:ClientSecret"] = "test-secret"
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountStore>();
            services.AddSingleton<IAccountStore>(AccountStore);
            services.RemoveAll<ISessionStore>();
            services.AddSingleton<ISessionStore>(SessionStore);
            services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme,
                options => options.Backchannel = new HttpClient(new GoogleStub(() => true)));
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
}

internal sealed class FakeSessionAccounts : IAccountStore
{
    public Dictionary<string, Account> Accounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<long, Account> AccountsById { get; } = new();

    public Task<Account?> FindByGoogleSubjectAsync(string subject, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.Values.FirstOrDefault(account => account.GoogleSubject == subject));
    public Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(AccountsById.TryGetValue(id, out var acc) ? acc : null);

    public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.TryGetValue(email, out var acc) ? acc : null);

    public Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName, CancellationToken cancellationToken)
    {
        if (Accounts.TryGetValue(email, out var existing)) return Task.FromResult(existing);
        var created = new Account(1, email, fullName, "Player", "Active", subject, false);
        Accounts[email] = created;
        AccountsById[created.Id] = created;
        return Task.FromResult(created);
    }
}

internal sealed class FakeSessionStore : ISessionStore
{
    public List<TimeSlotSummary> TimeSlots { get; } = new()
    {
        new(1, new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0), "08:00 - 10:00"),
        new(2, new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0), "10:00 - 12:00"),
        new(3, new TimeSpan(14, 0, 0), new TimeSpan(16, 0, 0), "14:00 - 16:00"),
        new(4, new TimeSpan(16, 0, 0), new TimeSpan(18, 0, 0), "16:00 - 18:00"),
        new(5, new TimeSpan(18, 0, 0), new TimeSpan(20, 0, 0), "18:00 - 20:00"),
        new(6, new TimeSpan(20, 0, 0), new TimeSpan(22, 0, 0), "20:00 - 22:00")
    };

    public List<SessionSummary> Sessions { get; } = new();
    public Dictionary<long, List<SessionParticipant>> Participants { get; } = new();
    public Dictionary<(long PlayerId, long SportId), PlayerSportProfileInfo> Profiles { get; } = new();
    public HashSet<(long SessionId, long PlayerId)> ApprovedExceptions { get; } = new();
    public HashSet<(long SessionId, long PlayerId)> RegisteredTickets { get; } = new();

    public Task<IReadOnlyList<TimeSlotSummary>> GetTimeSlotsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TimeSlotSummary>>(TimeSlots.OrderBy(t => t.StartTime).ThenBy(t => t.Id).ToList());

    public Task<IReadOnlyList<SessionSummary>> GetSessionsAsync(SessionFilter filter, long? currentUserId, CancellationToken cancellationToken)
    {
        var list = Sessions.AsEnumerable();
        if (filter.SportId.HasValue) list = list.Where(s => s.SportId == filter.SportId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Sport))
            list = list.Where(s => s.SportName.Equals(filter.Sport, StringComparison.OrdinalIgnoreCase) ||
                                   s.SportCode.Equals(filter.Sport, StringComparison.OrdinalIgnoreCase));
        if (filter.Date.HasValue) list = list.Where(s => s.SessionDate == filter.Date.Value);
        if (filter.FromDate.HasValue) list = list.Where(s => s.SessionDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) list = list.Where(s => s.SessionDate <= filter.ToDate.Value);
        if (filter.TimeSlotId.HasValue) list = list.Where(s => s.TimeSlotId == filter.TimeSlotId.Value);

        var raw = list.ToList();
        if (!currentUserId.HasValue)
        {
            return Task.FromResult<IReadOnlyList<SessionSummary>>(raw);
        }

        var result = new List<SessionSummary>(raw.Count);
        foreach (var s in raw)
        {
            if (ApprovedExceptions.Contains((s.Id, currentUserId.Value)))
            {
                result.Add(s with { IsEligibleSuggestion = true });
            }
            else if (string.IsNullOrWhiteSpace(s.Band))
            {
                result.Add(s with { IsEligibleSuggestion = true });
            }
            else if (Profiles.TryGetValue((currentUserId.Value, s.SportId), out var p))
            {
                if (!string.IsNullOrWhiteSpace(p.CurrentBand) && string.Equals(p.CurrentBand, s.Band, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(s with { IsEligibleSuggestion = true });
                }
                else
                {
                    result.Add(s with { IsEligibleSuggestion = false, IneligibilityReason = "lệch band kỹ năng" });
                }
            }
            else
            {
                result.Add(s with { IsEligibleSuggestion = false, IneligibilityReason = "chưa có hồ sơ môn này" });
            }
        }

        return Task.FromResult<IReadOnlyList<SessionSummary>>(result);
    }

    public Task<SessionDetails?> GetSessionByIdAsync(long id, long? currentUserId, CancellationToken cancellationToken)
    {
        var session = Sessions.FirstOrDefault(s => s.Id == id);
        if (session == null) return Task.FromResult<SessionDetails?>(null);

        Participants.TryGetValue(id, out var parts);
        parts ??= new List<SessionParticipant>();

        return Task.FromResult<SessionDetails?>(new SessionDetails(session, parts));
    }

    public Task<RegisterSessionResult> RegisterSessionAsync(long userId, long sessionId, CancellationToken cancellationToken)
    {
        var session = Sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
            return Task.FromResult(new RegisterSessionResult(false, "SESSION_NOT_FOUND", "Buổi chơi không tồn tại."));

        // Bước A: Check Sport-Specific Skill Eligibility
        bool hasException = ApprovedExceptions.Contains((sessionId, userId));
        if (!hasException && !string.IsNullOrWhiteSpace(session.Band))
        {
            if (!Profiles.TryGetValue((userId, session.SportId), out var profile))
            {
                return Task.FromResult(new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa có hồ sơ kỹ năng cho môn này.", "chưa có hồ sơ môn này"));
            }

            if (string.IsNullOrWhiteSpace(profile.CurrentBand) || profile.CurrentScore == null ||
                string.Equals(profile.RatingStatus, "PendingSurvey", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(profile.RatingStatus, "Unrated", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new RegisterSessionResult(false, "SKILL_INELIGIBLE", "Người chơi chưa làm khảo sát kỹ năng.", "chưa làm survey"));
            }

            if (!string.Equals(profile.CurrentBand.Trim(), session.Band.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new RegisterSessionResult(false, "SKILL_INELIGIBLE", $"Band kỹ năng ({profile.CurrentBand}) không khớp với yêu cầu của buổi chơi ({session.Band}).", "lệch band kỹ năng"));
            }
        }

        if (RegisteredTickets.Contains((sessionId, userId)))
        {
            return Task.FromResult(new RegisterSessionResult(false, "ALREADY_REGISTERED", "Bạn đã có vé hoặc đang giữ chỗ cho buổi chơi này."));
        }

        // Bước B: Check Available Slots
        if (session.OccupiedPlaces >= session.Capacity)
        {
            return Task.FromResult(new RegisterSessionResult(false, "SESSION_FULL", "Buổi chơi đã đủ người."));
        }

        // Bước C: Record Pending Registration
        var orderId = 1000L + Random.Shared.Next(1, 999);
        var ticketId = 2000L + Random.Shared.Next(1, 999);
        var orderCode = $"SBO-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        var ticketCode = $"TKT-{orderId}-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        RegisteredTickets.Add((sessionId, userId));

        return Task.FromResult(new RegisterSessionResult(
            Success: true,
            ErrorCode: null,
            Message: "Giữ chỗ thành công. Vui lòng thanh toán trước khi hết hạn.",
            Reason: null,
            OrderId: orderId,
            OrderCode: orderCode,
            TicketId: ticketId,
            TicketCode: ticketCode,
            AmountVnd: session.PriceVnd,
            ExpiresAt: expiresAt
        ));
    }
}
