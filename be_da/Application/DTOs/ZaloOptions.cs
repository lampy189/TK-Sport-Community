namespace be_da.Application.DTOs;

public sealed class ZaloOptions
{
    public bool Enabled { get; set; }
    public string AppId { get; set; } = "";
    public string OaId { get; set; } = "";
    public string OaSecretKey { get; set; } = "";
    public string AccessToken { get; set; } = "";
}

public sealed record ZaloInboundText(string OaId, string SenderId, string MessageId, string Text, long Timestamp);
