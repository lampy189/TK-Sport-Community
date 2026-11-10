using System.Security.Claims;
using be_da.Application.Abstractions;
using be_da.Application.Community;
using be_da.Controllers;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class CommunityControllerTests
{
    private const long UserId = 42;
    private const long FriendPlayerId = 9;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task SearchMembers_ReturnsMembersForCurrentUser()
    {
        IReadOnlyList<MemberSearchResult> members =
        [
            new(9, 90, "Player Nine", null, null, "None", null, Array.Empty<SportProfileItem>())
        ];
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.SearchMembersAsync(UserId, "player", 10, TestCancellationToken)).ReturnsAsync(members);
        var controller = CreateController(store);

        var result = await controller.SearchMembers("player", 10, TestCancellationToken);

        Assert.Same(members, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.SearchMembersAsync(UserId, "player", 10, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetFriendSuggestions_ReturnsSuggestionsForCurrentUser()
    {
        IReadOnlyList<FriendSuggestionItem> suggestions =
        [
            new(9, 90, "Player Nine", null, null, "None", null, 2, null, Array.Empty<SportProfileItem>())
        ];
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.GetFriendSuggestionsAsync(UserId, TestCancellationToken)).ReturnsAsync(suggestions);
        var controller = CreateController(store);

        var result = await controller.GetFriendSuggestions(TestCancellationToken);

        Assert.Same(suggestions, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetFriendSuggestionsAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetFriends_ReturnsFriendsForCurrentUser()
    {
        IReadOnlyList<FriendItem> friends =
        [
            new(100, FriendPlayerId, 90, "Player Nine", null, DateTimeOffset.UtcNow, Array.Empty<SportProfileItem>())
        ];
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.GetFriendsAsync(UserId, TestCancellationToken)).ReturnsAsync(friends);
        var controller = CreateController(store);

        var result = await controller.GetFriends(TestCancellationToken);

        Assert.Same(friends, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetFriendsAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetPendingRequests_ReturnsRequestsForCurrentUser()
    {
        IReadOnlyList<PendingFriendRequestItem> requests =
        [
            new(100, FriendPlayerId, 90, "Player Nine", null, DateTimeOffset.UtcNow, true)
        ];
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.GetPendingRequestsAsync(UserId, TestCancellationToken)).ReturnsAsync(requests);
        var controller = CreateController(store);

        var result = await controller.GetPendingRequests(TestCancellationToken);

        Assert.Same(requests, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetPendingRequestsAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendFriendRequest_WhenCsrfInvalid_ReturnsBadRequest()
    {
        var store = new Mock<IFriendshipStore>();
        var antiforgery = InvalidAntiforgery();
        var controller = CreateController(store, antiforgery);

        var result = await controller.SendFriendRequest(new SendFriendRequest(FriendPlayerId), TestCancellationToken);

        Assert.Equal("invalid_csrf_token", GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-2L)]
    public async Task SendFriendRequest_WhenTargetIdIsInvalid_ReturnsBadRequest(long? targetId)
    {
        var store = new Mock<IFriendshipStore>();
        var controller = CreateController(store);

        var result = await controller.SendFriendRequest(new SendFriendRequest(targetId), TestCancellationToken);

        Assert.Equal("invalid_target_player_id", GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("target_player_not_found", 404)]
    [InlineData("already_friends", 409)]
    [InlineData("friend_request_pending", 409)]
    [InlineData("other_error", 400)]
    public async Task SendFriendRequest_WhenStoreRejectsRequest_MapsErrorCode(string errorCode, int expectedStatus)
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.SendFriendRequestAsync(UserId, FriendPlayerId, TestCancellationToken))
            .ReturnsAsync(new FriendshipActionResult(false, errorCode, "Request rejected", null));
        var notifications = new Mock<IFirebaseNotificationService>();
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.SendFriendRequest(new SendFriendRequest(FriendPlayerId), TestCancellationToken);

        var response = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(errorCode, GetProperty<string>(response.Value, "code"));
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SendFriendRequest_WhenSuccessful_SendsNotificationAndReturnsResult()
    {
        var action = new FriendshipActionResult(true, null, null, 101);
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.SendFriendRequestAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(action);
        store.Setup(x => x.GetUserIdByPlayerIdAsync(FriendPlayerId, TestCancellationToken)).ReturnsAsync(90L);
        var notifications = new Mock<IFirebaseNotificationService>();
        notifications.Setup(x => x.SendPushNotificationAsync(
            90, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), TestCancellationToken))
            .Returns(Task.CompletedTask);
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.SendFriendRequest(new SendFriendRequest(FriendPlayerId), TestCancellationToken);

        Assert.Same(action, Assert.IsType<OkObjectResult>(result).Value);
        notifications.Verify(x => x.SendPushNotificationAsync(
            90, "Lời mời kết bạn mới", It.Is<string>(body => body.Contains("Test User")),
            It.Is<IDictionary<string, string>?>(data => data != null && data["type"] == "friend_request" && data["friendshipId"] == "101"),
            TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RespondFriendRequest_WhenDecisionIsMissing_ReturnsBadRequest()
    {
        var store = new Mock<IFriendshipStore>();
        var controller = CreateController(store);

        var result = await controller.RespondFriendRequest(100, new RespondFriendRequest(null), TestCancellationToken);

        Assert.Equal("invalid_decision", GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("request_not_found", 404)]
    [InlineData("unauthorized_to_respond", 403)]
    [InlineData("other_error", 400)]
    public async Task RespondFriendRequest_WhenStoreRejectsDecision_MapsErrorCode(string errorCode, int expectedStatus)
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.RespondFriendRequestAsync(UserId, 100, true, TestCancellationToken))
            .ReturnsAsync(new FriendshipActionResult(false, errorCode, "Decision rejected", null));
        var controller = CreateController(store);

        var result = await controller.RespondFriendRequest(100, new RespondFriendRequest(true), TestCancellationToken);

        if (errorCode == "unauthorized_to_respond")
        {
            Assert.IsType<ForbidResult>(result);
            return;
        }

        Assert.Equal(expectedStatus, Assert.IsAssignableFrom<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task RespondFriendRequest_WhenAccepted_SendsNotificationToRequester()
    {
        var action = new FriendshipActionResult(true, null, null, 100);
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.RespondFriendRequestAsync(UserId, 100, true, TestCancellationToken)).ReturnsAsync(action);
        store.Setup(x => x.GetPlayerIdByUserIdAsync(UserId, TestCancellationToken)).ReturnsAsync(7L);
        store.Setup(x => x.GetFriendsAsync(UserId, TestCancellationToken)).ReturnsAsync(
            (IReadOnlyList<FriendItem>)[new(100, FriendPlayerId, 90, "Player Nine", null, DateTimeOffset.UtcNow, Array.Empty<SportProfileItem>())]);
        var notifications = new Mock<IFirebaseNotificationService>();
        notifications.Setup(x => x.SendPushNotificationAsync(
            90, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), TestCancellationToken))
            .Returns(Task.CompletedTask);
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.RespondFriendRequest(100, new RespondFriendRequest(true), TestCancellationToken);

        Assert.Same(action, Assert.IsType<OkObjectResult>(result).Value);
        notifications.Verify(x => x.SendPushNotificationAsync(
            90, "Lời mời kết bạn đã được chấp nhận", It.Is<string>(body => body.Contains("Test User")),
            It.Is<IDictionary<string, string>?>(data => data != null && data["type"] == "friend_accepted" && data["friendshipId"] == "100"),
            TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RespondFriendRequest_WhenDeclined_DoesNotSendNotification()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.RespondFriendRequestAsync(UserId, 100, false, TestCancellationToken))
            .ReturnsAsync(new FriendshipActionResult(true, null, null, 100));
        var notifications = new Mock<IFirebaseNotificationService>();
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.RespondFriendRequest(100, new RespondFriendRequest(false), TestCancellationToken);

        Assert.IsType<OkObjectResult>(result);
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveFriend_WhenSuccessful_ReturnsNoContent()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.RemoveFriendAsync(UserId, FriendPlayerId, TestCancellationToken))
            .ReturnsAsync(new FriendshipActionResult(true, null, null, null));
        var controller = CreateController(store);

        var result = await controller.RemoveFriend(FriendPlayerId, TestCancellationToken);

        Assert.IsType<NoContentResult>(result);
        store.Verify(x => x.RemoveFriendAsync(UserId, FriendPlayerId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RemoveFriend_WhenFriendshipDoesNotExist_ReturnsNotFound()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.RemoveFriendAsync(UserId, FriendPlayerId, TestCancellationToken))
            .ReturnsAsync(new FriendshipActionResult(false, "friendship_not_found", "Not found", null));
        var controller = CreateController(store);

        var result = await controller.RemoveFriend(FriendPlayerId, TestCancellationToken);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("friendship_not_found", GetProperty<string>(notFound.Value, "code"));
    }

    [Fact]
    public async Task NotifyChatMessage_WhenUsersAreNotFriends_ReturnsForbidden()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(false);
        var notifications = new Mock<IFirebaseNotificationService>();
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.NotifyChatMessage(new NotifyChatMessageRequest(FriendPlayerId, "Hello"), TestCancellationToken);

        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Equal("not_friends", GetProperty<string>(((ObjectResult)result).Value, "code"));
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotifyChatMessage_WhenFriends_SendsPushNotification()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(true);
        store.Setup(x => x.GetUserIdByPlayerIdAsync(FriendPlayerId, TestCancellationToken)).ReturnsAsync(90L);
        store.Setup(x => x.GetPlayerIdByUserIdAsync(UserId, TestCancellationToken)).ReturnsAsync(7L);
        var notifications = new Mock<IFirebaseNotificationService>();
        notifications.Setup(x => x.SendPushNotificationAsync(
            90, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), TestCancellationToken))
            .Returns(Task.CompletedTask);
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.NotifyChatMessage(new NotifyChatMessageRequest(FriendPlayerId, "  Hi!  "), TestCancellationToken);

        Assert.Equal(StatusCodes.Status200OK, Assert.IsType<OkObjectResult>(result).StatusCode);
        notifications.Verify(x => x.SendPushNotificationAsync(
            90, "Tin nhắn mới từ Test User", "Hi!",
            It.Is<IDictionary<string, string>?>(data => data != null && data["type"] == "chat_message" && data["senderPlayerId"] == "7"),
            TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetConversation_WhenUsersAreNotFriends_ReturnsForbiddenWithoutReadingMessages()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(false);
        var controller = CreateController(store);

        var result = await controller.GetConversation(FriendPlayerId, 25, TestCancellationToken);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("not_friends", GetProperty<string>(forbidden.Value, "code"));
        store.Verify(x => x.GetConversationAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetConversation_WhenFriends_ReturnsMessagesWithRequestedLimit()
    {
        IReadOnlyList<ChatMessageItem> messages =
        [
            new(1, 7, UserId, "Test User", FriendPlayerId, 90, "Hi", false, DateTimeOffset.UtcNow, true)
        ];
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(true);
        store.Setup(x => x.GetConversationAsync(UserId, FriendPlayerId, 25, TestCancellationToken)).ReturnsAsync(messages);
        var controller = CreateController(store);

        var result = await controller.GetConversation(FriendPlayerId, 25, TestCancellationToken);

        Assert.Same(messages, Assert.IsType<OkObjectResult>(result).Value);
        store.Verify(x => x.GetConversationAsync(UserId, FriendPlayerId, 25, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WhenUsersAreNotFriends_ReturnsForbidden()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(false);
        var controller = CreateController(store);

        var result = await controller.SendMessage(new SendMessageRequest(FriendPlayerId, "Hello"), TestCancellationToken);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal("not_friends", GetProperty<string>(forbidden.Value, "code"));
        store.Verify(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendMessage_WhenSuccessful_CreatesMessageAndSendsNotification()
    {
        var message = new ChatMessageItem(101, 7, UserId, "Test User", FriendPlayerId, 90, "Hello friend", false, DateTimeOffset.UtcNow, true);
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.AreFriendsAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(true);
        store.Setup(x => x.SendMessageAsync(UserId, FriendPlayerId, "Hello friend", TestCancellationToken)).ReturnsAsync(message);
        var notifications = new Mock<IFirebaseNotificationService>();
        notifications.Setup(x => x.SendPushNotificationAsync(
            90, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDictionary<string, string>?>(), TestCancellationToken))
            .Returns(Task.CompletedTask);
        var controller = CreateController(store, notifications: notifications);

        var result = await controller.SendMessage(new SendMessageRequest(FriendPlayerId, "  Hello friend  "), TestCancellationToken);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.Same(message, ((ObjectResult)result).Value);
        store.Verify(x => x.SendMessageAsync(UserId, FriendPlayerId, "Hello friend", TestCancellationToken), Times.Once);
        notifications.Verify(x => x.SendPushNotificationAsync(
            90, "Tin nhắn mới từ Test User", "Hello friend",
            It.Is<IDictionary<string, string>?>(data => data != null && data["type"] == "chat_message" && data["messageId"] == "101"),
            TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task MarkMessagesAsRead_WhenCsrfValid_ReturnsNoContent()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.MarkMessagesAsReadAsync(UserId, FriendPlayerId, TestCancellationToken)).ReturnsAsync(true);
        var controller = CreateController(store);

        var result = await controller.MarkMessagesAsRead(FriendPlayerId, TestCancellationToken);

        Assert.IsType<NoContentResult>(result);
        store.Verify(x => x.MarkMessagesAsReadAsync(UserId, FriendPlayerId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task RegisterFcmToken_WhenTokenIsTooShort_ReturnsBadRequest()
    {
        var store = new Mock<IFriendshipStore>();
        var controller = CreateController(store);

        var result = await controller.RegisterFcmToken(new RegisterFcmTokenRequest("short", " Web "), TestCancellationToken);

        Assert.Equal("invalid_fcm_token", GetProperty<string>(Assert.IsType<BadRequestObjectResult>(result).Value, "code"));
        store.Verify(x => x.SaveFcmTokenAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterFcmToken_WhenValid_TrimsAndSavesTokenAndDeviceType()
    {
        var store = new Mock<IFriendshipStore>();
        store.Setup(x => x.SaveFcmTokenAsync(UserId, "valid-fcm-token", "Web", TestCancellationToken)).Returns(Task.CompletedTask);
        var controller = CreateController(store);

        var result = await controller.RegisterFcmToken(new RegisterFcmTokenRequest(" valid-fcm-token ", " Web "), TestCancellationToken);

        Assert.IsType<NoContentResult>(result);
        store.Verify(x => x.SaveFcmTokenAsync(UserId, "valid-fcm-token", "Web", TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReadAction_WhenUserIdClaimIsInvalid_ReturnsUnauthorizedWithoutCallingStore()
    {
        var store = new Mock<IFriendshipStore>();
        var controller = CreateController(store, userIdClaim: "not-a-number");

        var result = await controller.GetFriends(TestCancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
        store.VerifyNoOtherCalls();
    }

    private static CommunityController CreateController(
        Mock<IFriendshipStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        Mock<IFirebaseNotificationService>? notifications = null,
        string? userIdClaim = "42",
        string? userName = "Test User")
    {
        var context = new DefaultHttpContext();
        var claims = new List<Claim>();
        if (userIdClaim is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
        if (userName is not null)
            claims.Add(new Claim(ClaimTypes.Name, userName));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "UnitTest"));

        return new CommunityController(
            store.Object,
            (notifications ?? new Mock<IFirebaseNotificationService>()).Object,
            (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static Mock<IAntiforgery> ValidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery.Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        return antiforgery;
    }

    private static Mock<IAntiforgery> InvalidAntiforgery()
    {
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("Invalid CSRF token"));
        return antiforgery;
    }

    private static T? GetProperty<T>(object? value, string name) =>
        (T?)value?.GetType().GetProperty(name)?.GetValue(value);
}
