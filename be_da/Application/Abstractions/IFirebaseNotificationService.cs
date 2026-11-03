namespace be_da.Application.Abstractions;

public interface IFirebaseNotificationService
{
    Task SendPushNotificationAsync(long recipientUserId, string title, string body, IDictionary<string, string>? data, CancellationToken cancellationToken);
}

