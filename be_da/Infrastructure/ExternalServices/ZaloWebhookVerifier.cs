using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be_da.Application.DTOs;
using Microsoft.Extensions.Options;

namespace be_da.Infrastructure.ExternalServices;

/// <summary>Verifies the original UTF-8 body, never a reserialized JSON object.
/// Durable deduplication by (OA, message ID) is still required before processing.</summary>
public sealed class ZaloWebhookVerifier(IOptions<ZaloOptions> options)
{
    public ZaloInboundText? VerifyText(string rawBody, string signature)
    {
        var config = options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.OaSecretKey)
            || string.IsNullOrWhiteSpace(config.AppId) || string.IsNullOrWhiteSpace(config.OaId)
            || Encoding.UTF8.GetByteCount(rawBody) > 65536 || !signature.StartsWith("mac=", StringComparison.Ordinal)
            || signature.Length != 68) return null;
        try
        {
            using var json = JsonDocument.Parse(rawBody, new JsonDocumentOptions { MaxDepth = 16 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || HasDuplicates(root)) return null;
            var appId = Get(root, "app_id");
            var timestamp = Get(root, "timestamp");
            if (appId != config.AppId || !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var time)
                || time <= 0) return null;
            var actual = Convert.FromHexString(signature[4..]);
            var expected = SHA256.HashData(Encoding.UTF8.GetBytes(appId + rawBody + timestamp + config.OaSecretKey));
            if (!CryptographicOperations.FixedTimeEquals(expected, actual)) return null;
            if (Get(root, "event_name") != "user_send_text"
                || !root.TryGetProperty("recipient", out var recipient) || Get(recipient, "id") != config.OaId
                || !root.TryGetProperty("sender", out var sender) || !root.TryGetProperty("message", out var message)) return null;
            var senderId = Get(sender, "id");
            var messageId = Get(message, "msg_id");
            var text = Get(message, "text");
            if (string.IsNullOrWhiteSpace(senderId) || senderId.Length > 100
                || string.IsNullOrWhiteSpace(messageId) || messageId.Length > 200
                || string.IsNullOrWhiteSpace(text) || text.Length > 1200) return null;
            return new(config.OaId, senderId, messageId, text, time);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            return null;
        }
    }

    private static string? Get(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var item)
        && item.ValueKind == JsonValueKind.String ? item.GetString() : null;

    private static bool HasDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || HasDuplicates(property.Value)) return true;
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) if (HasDuplicates(item)) return true;
        return false;
    }
}
