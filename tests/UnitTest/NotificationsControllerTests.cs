using System.Security.Claims;
using be_da.Application.Abstractions;
using TKSC.Controllers;
using be_da.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class NotificationsControllerTests
{
    private const long UserId = 42;
    private const long NotificationId = 108;
    private static readonly CancellationToken TestCancellationToken = CancellationToken.None;

    [Fact]
    public async Task List_ReturnsFeedForCurrentUserAndSetsNoStoreHeader()
    {
        var feed = CreateFeed();
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.GetFeedAsync(UserId, TestCancellationToken)).ReturnsAsync(feed);
        var controller = CreateController(store);

        var result = await controller.List(TestCancellationToken);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Same(feed.Items, GetProperty<IReadOnlyList<NotificationFeedItem>>(response.Value, "items"));
        Assert.Equal(2, GetProperty<int>(response.Value, "unread"));
        Assert.Equal(NotificationId, GetProperty<long>(response.Value, "cursor"));
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
        store.Verify(x => x.GetFeedAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Read_WhenCsrfIsInvalid_ReturnsBadRequestWithoutUpdating()
    {
        var store = new Mock<INotificationStore>();
        var antiforgery = InvalidAntiforgery();
        var controller = CreateController(store, antiforgery);

        var result = await controller.Read(NotificationId, TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Read_WhenNotificationBelongsToUser_ReturnsNoContent()
    {
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.MarkReadAsync(UserId, NotificationId, TestCancellationToken)).ReturnsAsync(true);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(store, antiforgery);

        var result = await controller.Read(NotificationId, TestCancellationToken);

        Assert.IsType<NoContentResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.MarkReadAsync(UserId, NotificationId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Read_WhenNotificationIsNotFoundForUser_ReturnsNotFound()
    {
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.MarkReadAsync(UserId, NotificationId, TestCancellationToken)).ReturnsAsync(false);
        var controller = CreateController(store);

        var result = await controller.Read(NotificationId, TestCancellationToken);

        Assert.IsType<NotFoundResult>(result);
        store.Verify(x => x.MarkReadAsync(UserId, NotificationId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task ReadAll_WhenCsrfIsInvalid_ReturnsBadRequestWithoutUpdating()
    {
        var store = new Mock<INotificationStore>();
        var controller = CreateController(store, InvalidAntiforgery());

        var result = await controller.ReadAll(TestCancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid_csrf_token", GetProperty<string>(badRequest.Value, "code"));
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReadAll_WhenCsrfIsValid_MarksCurrentUsersNotificationsRead()
    {
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.MarkAllReadAsync(UserId, TestCancellationToken)).Returns(Task.CompletedTask);
        var antiforgery = ValidAntiforgery();
        var controller = CreateController(store, antiforgery);

        var result = await controller.ReadAll(TestCancellationToken);

        Assert.IsType<NoContentResult>(result);
        antiforgery.Verify(x => x.ValidateRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        store.Verify(x => x.MarkAllReadAsync(UserId, TestCancellationToken), Times.Once);
    }

    [Fact]
    public async Task Stream_WhenUserIsInactive_StopsAfterInitialRetryEvent()
    {
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.IsActiveUserAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        using var body = new MemoryStream();
        var controller = CreateController(store, responseBody: body);

        await controller.Stream(TestCancellationToken);

        Assert.Equal("text/event-stream", controller.Response.ContentType);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("no", controller.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.Equal("retry: 3000\n\n", System.Text.Encoding.UTF8.GetString(body.ToArray()));
        store.Verify(x => x.IsActiveUserAsync(UserId, TestCancellationToken), Times.Once);
        store.Verify(x => x.GetFeedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Stream_WhenActiveUserHasNotifications_WritesFeedAndKeepalive()
    {
        var store = new Mock<INotificationStore>();
        store.Setup(x => x.IsActiveUserAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.GetFeedAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(CreateFeed());
        using var cancellation = new CancellationTokenSource();
        using var body = new CancelOnKeepaliveStream(cancellation);
        var controller = CreateController(store, responseBody: body);

        await controller.Stream(cancellation.Token);

        var output = System.Text.Encoding.UTF8.GetString(body.ToArray());
        Assert.Contains("event: notifications\n", output);
        Assert.Contains("\"unread\":2", output);
        Assert.Contains(": keepalive\n\n", output);
        Assert.Equal("text/event-stream", controller.Response.ContentType);
        store.Verify(x => x.IsActiveUserAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.GetFeedAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static NotificationsController CreateController(
        Mock<INotificationStore> store,
        Mock<IAntiforgery>? antiforgery = null,
        string? userIdClaim = "42",
        Stream? responseBody = null)
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));
        if (responseBody is not null)
            context.Response.Body = responseBody;

        return new NotificationsController(store.Object, (antiforgery ?? ValidAntiforgery()).Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static NotificationFeed CreateFeed()
    {
        IReadOnlyList<NotificationFeedItem> items =
        [
            new(NotificationId, "New message", 17, "message", "unread"),
            new(107, "Friend request", 22, "friendship", "unread")
        ];
        return new NotificationFeed(items, 2, NotificationId);
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

    private sealed class CancelOnKeepaliveStream(CancellationTokenSource cancellation) : MemoryStream
    {
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(buffer, cancellationToken);
            if (System.Text.Encoding.UTF8.GetString(buffer.Span).Contains(": keepalive", StringComparison.Ordinal))
                cancellation.Cancel();
        }
    }
}
