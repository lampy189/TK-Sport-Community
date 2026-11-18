namespace TKSC.Application.Services.Notifications;

public static class NotificationLinks
{
    public static string Ticket(long id) => $"/player/home?view=tickets&ticketId={id}";
    public static string GoodsOrder(long id) => $"/player/home?view=goods&goodsOrderId={id}";
    public static string Absolute(string? origin, string path)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("Configure a valid HTTPS website URL (HTTP is allowed only for localhost).");
        return uri.ToString().TrimEnd('/') + path;
    }
}
