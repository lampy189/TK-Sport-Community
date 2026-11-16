using be_da.Application.Interfaces.External;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Application.Services.Notifications;

namespace be_da.Infrastructure.ExternalServices;

public sealed class FirebaseClient(IConfiguration config) : IFirebasePushTransport, IDisposable
{
    private FirebaseApp? app;
    private readonly object gate = new();
    public FirebaseMessaging Messaging
    {
        get
        {
            lock (gate)
            {
                app ??= FirebaseApp.Create(new AppOptions {
                    Credential = string.IsNullOrWhiteSpace(config["Firebase:CredentialsPath"])
                        ? GoogleCredential.GetApplicationDefault()
                        : CredentialFactory.FromFile<ServiceAccountCredential>(config["Firebase:CredentialsPath"]!).ToGoogleCredential(),
                    ProjectId = config["Firebase:ProjectId"]
                }, "tksc-push");
                return FirebaseMessaging.GetMessaging(app);
            }
        }
    }
    public void Dispose() => app?.Delete();
    public async Task<bool> SendAsync(string token, Dictionary<string, string> data, CancellationToken ct)
    {
        try
        {
#pragma warning disable CS0618 // Existing web clients register FCM tokens; keep token-based addressing.
            await Messaging.SendAsync(new Message { Token = token, Data = data,
                Webpush = new WebpushConfig { Headers = new Dictionary<string,string> { ["TTL"] = "300", ["Urgency"] = "normal" } }
            }, ct);
#pragma warning restore CS0618
            return true;
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered) { return false; }
    }
}

public sealed class FirebaseNotificationService(Sep490Context db, IFirebasePushTransport client, IConfiguration config,
    ILogger<FirebaseNotificationService> logger) : IFirebaseNotificationService
{
    public async Task SendPushNotificationAsync(long recipientUserId, string title, string body,
        IDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        if (!config.GetValue<bool>("Firebase:Enabled")) return;
        try { await DeliverAsync(recipientUserId, title, body, data, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning("FCM delivery failed for user {UserId} ({ErrorType}).", recipientUserId, ex.GetType().Name); }
    }

    public async Task<string> DeliverAsync(long userId, string title, string body,
        IDictionary<string, string>? data, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId && u.DeletedAt == null && u.Status == "Active", ct)) return "suppressed";
        var tokens = await db.UserFcmTokens.AsNoTracking().Where(t => t.UserId == userId).Select(t => t.FcmToken).Distinct().ToListAsync(ct);
        if (tokens.Count == 0) return "no_device";
        var payload = data is null ? new Dictionary<string, string>() : new Dictionary<string, string>(data);
        payload["title"] = title;
        payload["body"] = body;
        payload["url"] = NotificationLinks.Absolute(config["Firebase:WebUrl"] ?? config["Email:WebUrl"],
            payload.TryGetValue("ticketId", out var ticket) && long.TryParse(ticket, out var id) ? NotificationLinks.Ticket(id) :
            payload.TryGetValue("goodsOrderId", out var goods) && long.TryParse(goods, out var goodsId) ? NotificationLinks.GoodsOrder(goodsId) : "/player/home?view=notifications");
        var delivered = 0;
        var failed = false;
        foreach (var token in tokens)
        {
            if (!await db.UserFcmTokens.AnyAsync(t => t.UserId == userId && t.FcmToken == token, ct)) continue;
            try
            {
                if (await client.SendAsync(token, payload, ct)) delivered++;
                else await db.UserFcmTokens.Where(t => t.UserId == userId && t.FcmToken == token).ExecuteDeleteAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                failed = true;
                logger.LogWarning("FCM device delivery failed ({ErrorType}).", ex.GetType().Name);
            }
        }
        return failed ? "failed" : delivered > 0 ? "sent" : "no_device";
    }
}
