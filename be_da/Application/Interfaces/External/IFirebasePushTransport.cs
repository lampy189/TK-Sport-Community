namespace be_da.Application.Interfaces.External;
public interface IFirebasePushTransport
{
    // False means FCM positively identified an unregistered token; other failures throw.
    Task<bool> SendAsync(string token, Dictionary<string, string> data, CancellationToken ct);
}
