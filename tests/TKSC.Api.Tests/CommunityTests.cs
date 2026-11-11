using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
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
using be_da.Application.Interfaces.Repositories;
using be_da.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public class CommunityTests
{
    [Fact]
    public async Task SearchMembers_WithoutAuth_ReturnsUnauthorized()
    {
        await using var app = new CommunityTestApp();
        using var client = app.CreateAnonymousClient();

        var response = await client.GetAsync("/api/community/members");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchMembers_WhenAuthenticated_ReturnsMembers()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/community/members?query=Player");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<List<MemberSearchResult>>();
        Assert.NotNull(members);
        Assert.NotEmpty(members);
        Assert.Equal("Player Two", members[0].FullName);
        Assert.Equal("None", members[0].FriendshipStatus);
    }

    [Fact]
    public async Task GetFriendSuggestions_WhenAuthenticated_ReturnsSharedSlotPlayers()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/community/friends/suggestions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var suggestions = await response.Content.ReadFromJsonAsync<List<FriendSuggestionItem>>();
        Assert.NotNull(suggestions);
        Assert.Single(suggestions);
        Assert.Equal("Player Two", suggestions[0].FullName);
        Assert.Equal(2, suggestions[0].SharedSessionsCount);
    }

    [Fact]
    public async Task SendFriendRequest_WithoutCsrf_ReturnsBadRequest()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendFriendRequest(2);
        var response = await client.PostAsJsonAsync("/api/community/friends/request", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_csrf_token", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SendFriendRequest_ToSelf_ReturnsBadRequest()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendFriendRequest(1);
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/request", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cannot_friend_self", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SendFriendRequest_ValidTarget_CreatesRequestAndDispatchesNotification()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendFriendRequest(2);
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/request", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FriendshipActionResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.FriendshipId);

        Assert.Single(app.Notifications.Sent);
        Assert.Equal(2, app.Notifications.Sent[0].TargetUserId);
        Assert.Contains("Lời mời kết bạn", app.Notifications.Sent[0].Title);
    }

    [Fact]
    public async Task SendFriendRequest_WhenAlreadyPending_ReturnsConflict()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendFriendRequest(2);
        await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/request", body);

        var second = await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/request", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var json = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("friend_request_pending", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task RespondFriendRequest_WhenAccept_UpdatesStatusToAccepted()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddPendingRequest(100, requesterPlayerId: 2, addresseePlayerId: 1);
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new RespondFriendRequest(true);
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/100/respond", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<FriendshipActionResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);

        var friendsResponse = await client.GetAsync("/api/community/friends");
        var friends = await friendsResponse.Content.ReadFromJsonAsync<List<FriendItem>>();
        Assert.NotNull(friends);
        Assert.Contains(friends, f => f.FriendPlayerId == 2);
    }

    [Fact]
    public async Task RespondFriendRequest_WhenDecline_UpdatesStatus()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddPendingRequest(101, requesterPlayerId: 2, addresseePlayerId: 1);
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new RespondFriendRequest(false);
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/friends/101/respond", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RemoveFriend_WhenFriends_RemovesFriendship()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddAcceptedFriend(200, 1, 2);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await SendWithCsrf(client, HttpMethod.Delete, "/api/community/friends/2", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var friendsResponse = await client.GetAsync("/api/community/friends");
        var friends = await friendsResponse.Content.ReadFromJsonAsync<List<FriendItem>>();
        Assert.NotNull(friends);
        Assert.DoesNotContain(friends, f => f.FriendPlayerId == 2);
    }

    [Fact]
    public async Task NotifyChatMessage_WhenNotFriends_ReturnsForbidden()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new NotifyChatMessageRequest(2, "Hello there!");
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/chat/notify-message", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_friends", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task NotifyChatMessage_WhenFriends_DispatchesFcmNotification()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddAcceptedFriend(300, 1, 2);
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new NotifyChatMessageRequest(2, "Chieu nay danh cau khong?");
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/chat/notify-message", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(app.Notifications.Sent, n => n.TargetUserId == 2 && n.Body == "Chieu nay danh cau khong?");
    }

    [Fact]
    public async Task RegisterFcmToken_WithValidToken_ReturnsNoContent()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new RegisterFcmTokenRequest("fcm-device-token-1234567890", "Web");
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/fcm-token", body);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var tokens = await app.FriendshipStore.GetFcmTokensByUserIdAsync(1, default);
        Assert.Contains("fcm-device-token-1234567890", tokens);
    }

    [Fact]
    public async Task GetConversation_WhenNotFriends_ReturnsForbidden()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/community/messages/2");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_friends", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetConversation_WhenFriends_ReturnsMessages()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddAcceptedFriend(10, 1, 2);
        using var client = await app.CreateAuthenticatedClientAsync();

        await app.FriendshipStore.SendMessageAsync(1, 2, "Xin chao!", default);

        var response = await client.GetAsync("/api/community/messages/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<ChatMessageItem>>();
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Equal("Xin chao!", list[0].Content);
        Assert.True(list[0].IsMine);
    }

    [Fact]
    public async Task SendMessage_WhenNotFriends_ReturnsForbidden()
    {
        await using var app = new CommunityTestApp();
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendMessageRequest(2, "Hello");
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/messages", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("not_friends", json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SendMessage_WhenFriends_ReturnsCreatedAndDispatchesPush()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddAcceptedFriend(10, 1, 2);
        using var client = await app.CreateAuthenticatedClientAsync();

        var body = new SendMessageRequest(2, "Hello friend, play badminton today?");
        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/messages", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var msg = await response.Content.ReadFromJsonAsync<ChatMessageItem>();
        Assert.NotNull(msg);
        Assert.Equal("Hello friend, play badminton today?", msg.Content);
        Assert.Equal(1, msg.SenderPlayerId);
        Assert.Equal(2, msg.RecipientPlayerId);

        Assert.Single(app.Notifications.Sent);
        var push = app.Notifications.Sent[0];
        Assert.Equal(2, push.TargetUserId);
        Assert.Contains("Tin nhắn mới từ", push.Title);
        Assert.Equal("chat_message", push.Data?["type"]);
    }

    [Fact]
    public async Task MarkMessagesAsRead_WhenCalled_ReturnsNoContent()
    {
        await using var app = new CommunityTestApp();
        app.FriendshipStore.AddAcceptedFriend(10, 1, 2);
        using var client = await app.CreateAuthenticatedClientAsync();

        var response = await SendWithCsrf(client, HttpMethod.Post, "/api/community/messages/2/read", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendWithCsrf(HttpClient client, HttpMethod method, string path, object? body)
    {
        var csrf = await client.GetFromJsonAsync<JsonElement>("/api/auth/csrf");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await client.SendAsync(request);
    }
}

internal sealed class CommunityTestApp : WebApplicationFactory<Program>
{
    public FakeFriendshipStore FriendshipStore { get; } = new();
    public FakeNotificationService Notifications { get; } = new();
    public FakeAccounts AccountStore { get; } = new();

    public CommunityTestApp()
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
            services.RemoveAll<IFriendshipStore>();
            services.AddSingleton<IFriendshipStore>(FriendshipStore);
            services.RemoveAll<IFirebaseNotificationService>();
            services.AddSingleton<IFirebaseNotificationService>(Notifications);
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

internal sealed class FakeNotificationService : IFirebaseNotificationService
{
    public List<SentPushNotification> Sent { get; } = new();

    public Task SendPushNotificationAsync(long targetUserId, string title, string body, IDictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        Sent.Add(new SentPushNotification(targetUserId, title, body, data));
        return Task.CompletedTask;
    }

    public sealed record SentPushNotification(long TargetUserId, string Title, string Body, IDictionary<string, string>? Data);
}

internal sealed class FakeFriendshipStore : IFriendshipStore
{
    private long _autoId = 1000;
    private readonly List<FriendshipRecord> _friendships = new();
    private readonly Dictionary<long, List<string>> _fcmTokens = new();

    public void AddPendingRequest(long id, long requesterPlayerId, long addresseePlayerId)
    {
        _friendships.Add(new FriendshipRecord(id, requesterPlayerId, addresseePlayerId, "Pending", DateTimeOffset.UtcNow, null));
    }

    public void AddAcceptedFriend(long id, long p1, long p2)
    {
        _friendships.Add(new FriendshipRecord(id, p1, p2, "Accepted", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow));
    }

    public Task<long?> GetPlayerIdByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult<long?>(userId);

    public Task<long?> GetUserIdByPlayerIdAsync(long playerId, CancellationToken cancellationToken) =>
        Task.FromResult<long?>(playerId);

    public Task<IReadOnlyList<MemberSearchResult>> SearchMembersAsync(long currentUserId, string? query, int limit, CancellationToken cancellationToken)
    {
        var other = new MemberSearchResult(2, 2, "Player Two", "https://example.com/p2.jpg", "Male", "None", null,
            new List<SportProfileItem> { new("badminton", "Badminton", 60, "B3", "Provisional") }, "Bio two");
        return Task.FromResult<IReadOnlyList<MemberSearchResult>>(new List<MemberSearchResult> { other });
    }

    public Task<IReadOnlyList<FriendSuggestionItem>> GetFriendSuggestionsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        var item = new FriendSuggestionItem(
            2, 2, "Player Two", "https://example.com/p2.jpg", "Male", "None", null,
            2, "Pickleball · Court 2 (03/11/2026 · 16:00 - 18:00)",
            new List<SportProfileItem> { new("pickleball", "Pickleball", 50, "B2", "Rated") },
            "Bio two"
        );
        return Task.FromResult<IReadOnlyList<FriendSuggestionItem>>(new List<FriendSuggestionItem> { item });
    }

    public Task<IReadOnlyList<FriendItem>> GetFriendsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        var friends = _friendships
            .Where(f => (f.RequesterPlayerId == currentUserId || f.AddresseePlayerId == currentUserId) && f.Status == "Accepted")
            .Select(f =>
            {
                var otherId = f.RequesterPlayerId == currentUserId ? f.AddresseePlayerId : f.RequesterPlayerId;
                return new FriendItem(f.Id, otherId, otherId, $"Player {otherId}", null, f.RespondedAt ?? f.CreatedAt, Array.Empty<SportProfileItem>());
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<FriendItem>>(friends);
    }

    public Task<IReadOnlyList<PendingFriendRequestItem>> GetPendingRequestsAsync(long currentUserId, CancellationToken cancellationToken)
    {
        var items = _friendships
            .Where(f => (f.RequesterPlayerId == currentUserId || f.AddresseePlayerId == currentUserId) && f.Status == "Pending")
            .Select(f =>
            {
                var otherId = f.RequesterPlayerId == currentUserId ? f.AddresseePlayerId : f.RequesterPlayerId;
                var isIncoming = f.AddresseePlayerId == currentUserId;
                return new PendingFriendRequestItem(f.Id, otherId, otherId, $"Player {otherId}", null, f.CreatedAt, isIncoming);
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<PendingFriendRequestItem>>(items);
    }

    public Task<FriendshipActionResult> SendFriendRequestAsync(long currentUserId, long targetPlayerId, CancellationToken cancellationToken)
    {
        if (currentUserId == targetPlayerId)
            return Task.FromResult(new FriendshipActionResult(false, "cannot_friend_self", "Cannot friend yourself", null));

        var existing = _friendships.FirstOrDefault(f =>
            ((f.RequesterPlayerId == currentUserId && f.AddresseePlayerId == targetPlayerId) ||
             (f.RequesterPlayerId == targetPlayerId && f.AddresseePlayerId == currentUserId)) &&
            (f.Status == "Pending" || f.Status == "Accepted"));

        if (existing is not null)
        {
            if (existing.Status == "Accepted")
                return Task.FromResult(new FriendshipActionResult(false, "already_friends", "Already friends", null));
            return Task.FromResult(new FriendshipActionResult(false, "friend_request_pending", "Request pending", null));
        }

        var id = ++_autoId;
        _friendships.Add(new FriendshipRecord(id, currentUserId, targetPlayerId, "Pending", DateTimeOffset.UtcNow, null));
        return Task.FromResult(new FriendshipActionResult(true, null, null, id));
    }

    public Task<FriendshipActionResult> RespondFriendRequestAsync(long currentUserId, long friendshipId, bool accept, CancellationToken cancellationToken)
    {
        var f = _friendships.FirstOrDefault(x => x.Id == friendshipId);
        if (f is null || f.Status != "Pending")
            return Task.FromResult(new FriendshipActionResult(false, "request_not_found", "Request not found", null));

        if (f.AddresseePlayerId != currentUserId)
            return Task.FromResult(new FriendshipActionResult(false, "unauthorized_to_respond", "Unauthorized", null));

        _friendships.Remove(f);
        _friendships.Add(f with { Status = accept ? "Accepted" : "Declined", RespondedAt = DateTimeOffset.UtcNow });
        return Task.FromResult(new FriendshipActionResult(true, null, null, friendshipId));
    }

    public Task<FriendshipActionResult> RemoveFriendAsync(long currentUserId, long friendPlayerId, CancellationToken cancellationToken)
    {
        var f = _friendships.FirstOrDefault(x =>
            ((x.RequesterPlayerId == currentUserId && x.AddresseePlayerId == friendPlayerId) ||
             (x.RequesterPlayerId == friendPlayerId && x.AddresseePlayerId == currentUserId)) &&
            (x.Status == "Accepted" || x.Status == "Pending"));

        if (f is null)
            return Task.FromResult(new FriendshipActionResult(false, "friendship_not_found", "Not found", null));

        _friendships.Remove(f);
        return Task.FromResult(new FriendshipActionResult(true, null, null, null));
    }

    public Task<bool> AreFriendsAsync(long currentUserId, long otherPlayerId, CancellationToken cancellationToken)
    {
        var isFriend = _friendships.Any(x =>
            ((x.RequesterPlayerId == currentUserId && x.AddresseePlayerId == otherPlayerId) ||
             (x.RequesterPlayerId == otherPlayerId && x.AddresseePlayerId == currentUserId)) &&
            x.Status == "Accepted");
        return Task.FromResult(isFriend);
    }

    public Task SaveFcmTokenAsync(long userId, string fcmToken, string? deviceType, CancellationToken cancellationToken)
    {
        if (!_fcmTokens.TryGetValue(userId, out var list))
        {
            list = new List<string>();
            _fcmTokens[userId] = list;
        }
        if (!list.Contains(fcmToken)) list.Add(fcmToken);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetFcmTokensByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        var tokens = _fcmTokens.TryGetValue(userId, out var list) ? list : new List<string>();
        return Task.FromResult<IReadOnlyList<string>>(tokens);
    }

    private readonly List<ChatMessageItem> _messages = new();
    private long _msgAutoId = 2000;

    public Task<ChatMessageItem?> SendMessageAsync(long currentUserId, long recipientPlayerId, string content, CancellationToken cancellationToken)
    {
        var msg = new ChatMessageItem(
            ++_msgAutoId,
            currentUserId,
            currentUserId,
            $"Player {currentUserId}",
            recipientPlayerId,
            recipientPlayerId,
            content,
            false,
            DateTimeOffset.UtcNow,
            true
        );
        _messages.Add(msg);
        return Task.FromResult<ChatMessageItem?>(msg);
    }

    public Task<IReadOnlyList<ChatMessageItem>> GetConversationAsync(long currentUserId, long otherPlayerId, int limit, CancellationToken cancellationToken)
    {
        var list = _messages
            .Where(m => (m.SenderPlayerId == currentUserId && m.RecipientPlayerId == otherPlayerId) ||
                        (m.SenderPlayerId == otherPlayerId && m.RecipientPlayerId == currentUserId))
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<ChatMessageItem>>(list);
    }

    public Task<bool> MarkMessagesAsReadAsync(long currentUserId, long senderPlayerId, CancellationToken cancellationToken)
    {
        for (int i = 0; i < _messages.Count; i++)
        {
            if (_messages[i].SenderPlayerId == senderPlayerId && _messages[i].RecipientPlayerId == currentUserId)
            {
                _messages[i] = _messages[i] with { IsRead = true };
            }
        }
        return Task.CompletedTask.ContinueWith(_ => true, cancellationToken);
    }
}
