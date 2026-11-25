namespace be_da.Application.DTOs;

public sealed class ZaloConversation
{
    public string? Name { get; set; }
    public int? Score { get; set; }
    public long? ScoreSportId { get; set; }
    public List<SessionSummary> Choices { get; set; } = [];
    public SessionSummary? Selected { get; set; }
    public DateTimeOffset? QuoteExpiresAt { get; set; }
    public long? OrderId { get; set; }
    public string LastReply { get; set; } = "";
}

public sealed record ZaloIntent(string Action, long? SessionId, string? Name, int? Score,
    string? Sport, string? Date, string? Venue, string? StartFrom, string? Clarification);
public sealed record ZaloWork(long Id, long CustomerId, string SenderId, string MessageId, string Text, DateTimeOffset ReceivedAt);
public sealed record ZaloOutgoing(string Text, long? OrderId = null, string? QrCode = null, bool Checkout = false);
public sealed record ZaloBookingConfirmation(long CustomerId, string MessageId, SessionSummary Quote, DateTimeOffset ExpiresAt);
public interface IZaloIntentInterpreter
{
    Task<ZaloIntent?> InterpretAsync(string text, ZaloConversation state, CancellationToken ct);
}
public interface IZaloBookingProcessor
{
    Task ProcessAsync(ZaloWork work,CancellationToken ct);
}
public enum ZaloReceiveResult { Accepted, Disabled, InvalidSignature, Stale, RateLimited }
public sealed record ZaloQueueCount(string Queue,string State,int Count);
public sealed record ZaloRuntimeStatus(bool Enabled,bool WorkerEnabled,IReadOnlyList<ZaloQueueCount> Counts);
public interface IZaloWebhookService
{
    Task<ZaloReceiveResult> ReceiveAsync(string body,string signature,CancellationToken ct);
    Task<ZaloRuntimeStatus> StatusAsync(CancellationToken ct);
}
