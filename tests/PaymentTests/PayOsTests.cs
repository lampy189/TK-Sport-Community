using TKSC.Domain.Payments;
using TKSC.Infrastructure.Services;
using TKSC.Infrastructure.Configurations;
using TKSC.Application.DTOs;
using Microsoft.Extensions.Configuration;
using Xunit;

public class PayOsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-20T10:00:00Z");
    [Theory]
    [InlineData(120000, true, 1, true, "paid")]
    [InlineData(12000000, true, 1, true, "review_required")]
    [InlineData(119999, true, 1, true, "review_required")]
    [InlineData(120001, true, 1, true, "review_required")]
    [InlineData(120000, false, 1, true, "failed")]
    [InlineData(120000, true, -1, true, "review_required")]
    [InlineData(120000, true, 0, true, "review_required")]
    [InlineData(120000, true, 1, false, "review_required")]
    public void OnlyExactSuccessfulEligibleUnexpiredPaymentGrantsTickets(long amount, bool success,
        int minutes, bool eligible, string expected) =>
        Assert.Equal(expected, PaymentRules.Decide(120000, amount, success, Now.AddMinutes(minutes), Now, eligible));

    [Fact]
    public void MissingDeadlineRequiresReview() =>
        Assert.Equal("review_required", PaymentRules.Decide(120000, 120000, true, null, Now, true));

    private static PayOsGateway Gateway()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> {
            ["Payments:PayOS:ClientId"] = "test-client",
            ["Payments:PayOS:ApiKey"] = "test-api-key",
            ["Payments:PayOS:ChecksumKey"] = "test-secret",
            ["Payments:PayOS:ReturnUrl"] = "https://example.test/",
            ["Payments:PayOS:CancelUrl"] = "https://example.test/"
        }).Build();
        return new(config, new PayOsClientFactory(config));
    }

    [Fact]
    public void CheckoutUsesWholeVndAndStableAttemptCode()
    {
        var request = Gateway().BuildRequest(12, 42, 120000, Now.AddMinutes(15));
        Assert.Equal(120000, request.Amount);
        Assert.Equal(12, request.OrderCode);
        Assert.Equal("https://example.test/?orderId=42", request.ReturnUrl);
        Assert.Equal(request.ReturnUrl, request.CancelUrl);
        Assert.Equal(Now.AddMinutes(15).ToUnixTimeSeconds(), request.ExpiredAt);
    }

    [Fact]
    public async Task RejectsUnsignedAndTamperedWebhook()
    {
        var ex = await Assert.ThrowsAsync<GatewayException>(() => Gateway().VerifyAsync(
            """{"data":{"orderCode":12,"amount":120000},"signature":"forged"}"""));
        Assert.True(ex.InvalidSignature);
    }

    [Fact]
    public async Task VerifiesSignedWebhookAndDoesNotTrustUnsignedEnvelope()
    {
        var data = new PayOS.Models.Webhooks.WebhookData {
            OrderCode = 12, Amount = 120000, Reference = "BANK1", PaymentLinkId = "link12", Code = "00"
        };
        var sdk = new PayOS.PayOSClient("test-client", "test-api-key", "test-secret");
        var webhook = new PayOS.Models.Webhooks.Webhook {
            Code = "not-authoritative", Success = false, Data = data,
            Signature = sdk.Crypto.CreateSignatureFromObject(data, "test-secret")!
        };
        var verified = await Gateway().VerifyAsync(System.Text.Json.JsonSerializer.Serialize(webhook));
        Assert.True(verified.Success);
        Assert.Equal(120000, verified.Amount);
        data.Amount = 1;
        await Assert.ThrowsAsync<GatewayException>(() => Gateway().VerifyAsync(System.Text.Json.JsonSerializer.Serialize(webhook)));
    }
}
