using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ZaloTransportTests
{
    private static ZaloOptions Config() => new() { Enabled = true, AppId = "app", OaId = "oa", OaSecretKey = "test-secret", AccessToken = "test-token" };
    private const string Body = """{"app_id":"app","recipient":{"id":"oa"},"sender":{"id":"customer"},"event_name":"user_send_text","message":{"text":"Tối nay còn sân không?","msg_id":"m1"},"timestamp":"1795000000000"}""";
    private static string Sign(string body) => "mac=" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("app" + body + "1795000000000" + "test-secret"))).ToLowerInvariant();

    [Fact]
    public void Verifies_original_utf8_body_and_extracts_sender()
    {
        var result = new ZaloWebhookVerifier(Options.Create(Config())).VerifyText(Body, Sign(Body));
        Assert.NotNull(result);
        Assert.Equal("customer", result.SenderId);
        Assert.Equal("m1", result.MessageId);
        Assert.Equal("Tối nay còn sân không?", result.Text);
    }

    [Fact]
    public void Rejects_tampering_even_when_parsed_json_is_equivalent()
    {
        Assert.Null(new ZaloWebhookVerifier(Options.Create(Config())).VerifyText(Body + " ", Sign(Body)));
    }

    [Theory]
    [InlineData("oa", "other-oa")]
    [InlineData("app", "other-app")]
    [InlineData("user_send_text", "oa_send_text")]
    public void Rejects_wrong_destination_or_outgoing_event(string before, string after)
    {
        var body = Body.Replace($"\"{before}\"", $"\"{after}\"");
        Assert.Null(new ZaloWebhookVerifier(Options.Create(Config())).VerifyText(body, Sign(body)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("mac=invalid")]
    [InlineData("mac=gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    public void Rejects_invalid_signature_without_throwing(string signature) =>
        Assert.Null(new ZaloWebhookVerifier(Options.Create(Config())).VerifyText(Body, signature));

    [Fact]
    public void Rejects_duplicate_json_keys()
    {
        var body = Body.Replace("\"app_id\":\"app\"", "\"app_id\":\"app\",\"app_id\":\"app\"");
        Assert.Null(new ZaloWebhookVerifier(Options.Create(Config())).VerifyText(body, Sign(body)));
    }

    [Fact]
    public void Disabled_configuration_rejects_webhooks()
    {
        var config = Config(); config.Enabled = false;
        Assert.Null(new ZaloWebhookVerifier(Options.Create(config)).VerifyText(Body, Sign(Body)));
    }

    [Fact]
    public async Task Sends_qr_attachment_to_exact_recipient_with_header_token()
    {
        var handler = new CaptureHandler("""{"error":0,"data":{"message_id":"sent-1"}}""");
        using var http = new HttpClient(handler);
        var client = new ZaloOaClient(http, Options.Create(Config()));
        Assert.Equal("sent-1", await client.SendImageAsync("customer", "Đơn A: 100.000đ", "uploaded-qr", default));
        Assert.Equal("https://openapi.zalo.me/v3.0/oa/message/cs", handler.Url);
        Assert.Equal("test-token", handler.Token);
        Assert.DoesNotContain("test-token", handler.Body!);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("customer", json.RootElement.GetProperty("recipient").GetProperty("user_id").GetString());
        Assert.Equal("uploaded-qr", json.RootElement.GetProperty("message").GetProperty("attachment")
            .GetProperty("payload").GetProperty("elements")[0].GetProperty("attachment_id").GetString());
    }

    [Theory]
    [InlineData("{\"error\":-216,\"message\":\"quota\"}")]
    [InlineData("{\"error\":0}")]
    public async Task Does_not_report_success_for_business_error_or_missing_receipt(string response)
    {
        using var http = new HttpClient(new CaptureHandler(response));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ZaloOaClient(http, Options.Create(Config()))
            .SendTextAsync("customer", "Xin chào", default));
    }

    private sealed class CaptureHandler(string response) : HttpMessageHandler
    {
        public string? Url, Token, Body;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.AbsoluteUri;
            Token = request.Headers.GetValues("access_token").Single();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(HttpStatusCode.OK) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
    }
}
