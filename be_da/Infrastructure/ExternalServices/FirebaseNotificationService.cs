using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;

namespace be_da.Infrastructure.ExternalServices;
public sealed class FirebaseNotificationService(IFriendshipStore friendshipStore, ILogger<FirebaseNotificationService> logger) : IFirebaseNotificationService
{
    public async Task SendPushNotificationAsync(long recipientUserId, string title, string body, IDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = await friendshipStore.GetFcmTokensByUserIdAsync(recipientUserId, cancellationToken);
            if (tokens.Count == 0)
            {
                logger.LogInformation("No registered FCM tokens for user {UserId}. Skipping push notification '{Title}'.", recipientUserId, title);
                return;
            }

            logger.LogInformation("Sending Firebase Push Notification to {TokenCount} device(s) for User {UserId}: [{Title}] {Body}", tokens.Count, recipientUserId, title, body);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch FCM push notification for user {UserId}.", recipientUserId);
        }
    }
}
