namespace be_da.Application.DTOs;

public sealed class ZaloOptions
{
    public bool Enabled { get; set; }
    public string AppId { get; set; } = "";
    public string OaId { get; set; } = "";
    public string OaSecretKey { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string TokenFile { get; set; } = "";
    public long? VenueId { get; set; }
    public bool WorkerEnabled { get; set; } = true;
}

public sealed record ZaloInboundText(string OaId, string SenderId, string MessageId, string Text, long Timestamp);
