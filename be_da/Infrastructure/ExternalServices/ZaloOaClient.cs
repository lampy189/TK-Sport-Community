using System.Net.Http.Json;
using System.Text.Json;
using be_da.Application.DTOs;
using Microsoft.Extensions.Options;

namespace be_da.Infrastructure.ExternalServices;

/// <summary>Transport only. Call from a durable outbox after recipient/booking authorization.
/// A timeout has an unknown delivery outcome; callers must not blindly resend.</summary>
public sealed class ZaloOaClient(HttpClient http, IOptions<ZaloOptions> options)
{
    public Task<string> SendTextAsync(string recipientId, string text, CancellationToken cancellationToken) =>
        SendAsync(recipientId, text, null, cancellationToken);

    public Task<string> SendImageAsync(string recipientId, string text, string attachmentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(attachmentId) || attachmentId.Length > 512)
            throw new ArgumentException("Thiếu mã ảnh Zalo.", nameof(attachmentId));
        return SendAsync(recipientId, text, attachmentId, cancellationToken);
    }

    private async Task<string> SendAsync(string recipientId, string text, string? attachmentId, CancellationToken cancellationToken)
    {
        var config = options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.AccessToken))
            throw new InvalidOperationException("Chưa bật hoặc cấu hình kết nối Zalo OA.");
        if (string.IsNullOrWhiteSpace(recipientId) || recipientId.Length > 100)
            throw new ArgumentException("Thiếu người nhận Zalo.", nameof(recipientId));
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000)
            throw new ArgumentException("Tin nhắn Zalo phải có từ 1 đến 2000 ký tự.", nameof(text));
        var message = new Dictionary<string, object> { ["text"] = text };
        if (attachmentId is not null)
            message["attachment"] = new { type = "template", payload = new {
                template_type = "media", elements = new[] { new { media_type = "image", attachment_id = attachmentId } }
            } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://openapi.zalo.me/v3.0/oa/message/cs");
        request.Headers.Add("access_token", config.AccessToken);
        request.Content = JsonContent.Create(new { recipient = new { user_id = recipientId }, message });
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Zalo chưa xác nhận gửi tin.", null, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!body.RootElement.TryGetProperty("error", out var error) || !error.TryGetInt32(out var code) || code != 0
            || !body.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("message_id", out var id) || id.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(id.GetString()))
            throw new InvalidOperationException("Zalo từ chối hoặc chưa xác nhận gửi tin; kiểm tra quyền, hạn mức và token OA.");
        return id.GetString()!;
    }
}
