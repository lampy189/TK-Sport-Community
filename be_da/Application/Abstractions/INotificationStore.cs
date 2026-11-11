namespace be_da.Application.Abstractions;

public sealed record NotificationFeedItem(
    long Id,
    string? Title,
    long? ReferenceId,
    string? ReferenceType,
    string? Status);

public sealed record NotificationFeed(
    IReadOnlyList<NotificationFeedItem> Items,
    int Unread,
    long Cursor);

public interface INotificationStore
{
    Task<NotificationFeed> GetFeedAsync(long userId, CancellationToken cancellationToken);
    Task<bool> MarkReadAsync(long userId, long notificationId, CancellationToken cancellationToken);
    Task MarkAllReadAsync(long userId, CancellationToken cancellationToken);
    Task<bool> IsActiveUserAsync(long userId, CancellationToken cancellationToken);
}
