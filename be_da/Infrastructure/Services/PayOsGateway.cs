using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using TKSC.Infrastructure.Interfaces;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces;

namespace TKSC.Infrastructure.Services;

public sealed class PayOsGateway(IConfiguration config, IPayOsClientFactory factory) : IPaymentGateway
{
    private string Required(string key) => config[$"Payments:PayOS:{key}"] is { Length: > 0 } value
        ? value : throw new GatewayException("Chưa cấu hình đầy đủ kênh thanh toán payOS.");
    public void ValidateConfiguration()
    {
        _ = Required("ClientId"); _ = Required("ApiKey"); _ = Required("ChecksumKey");
        foreach (var key in new[] { "ReturnUrl", "CancelUrl" })
            if (!Uri.TryCreate(Required(key), UriKind.Absolute, out var uri)
                || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new GatewayException("URL quay về của payOS chưa hợp lệ.");
    }

    public CreatePaymentLinkRequest BuildRequest(long paymentId, long orderId, int amount, DateTimeOffset expiry) => new()
    {
        OrderCode = paymentId, Amount = amount, Description = $"TKSC {paymentId}",
        ExpiredAt = expiry.ToUnixTimeSeconds(),
        ReturnUrl = QueryHelpers.AddQueryString(Required("ReturnUrl"), "orderId", orderId.ToString(CultureInfo.InvariantCulture)),
        CancelUrl = QueryHelpers.AddQueryString(Required("CancelUrl"), "orderId", orderId.ToString(CultureInfo.InvariantCulture))
    };

    public async Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt)
    {
        ValidateConfiguration();
        try
        {
            var client = factory.Create();
            try
            {
                var link = await client.PaymentRequests.CreateAsync(BuildRequest(paymentId, orderId, amount, expiresAt));
                return new(link.OrderCode, link.Amount, link.PaymentLinkId, link.CheckoutUrl, Status(link.Status));
            }
            catch (ApiException)
            {
                // A duplicate or timeout may mean the original create succeeded.
                // Retrieve the same orderCode; do not allocate a new attempt on uncertain outcomes.
                var link = await client.PaymentRequests.GetAsync(paymentId);
                return new(link.OrderCode, link.Amount, link.Id,
                    $"https://pay.payos.vn/web/{Uri.EscapeDataString(link.Id)}", Status(link.Status));
            }
        }
        catch (Exception e) when (e is PayOSException or HttpRequestException or TaskCanceledException)
        {
            throw new GatewayException("Chưa nhận được link payOS. Hãy thử lại; hệ thống sẽ giữ nguyên mã giao dịch.");
        }
    }

    public async Task<GatewayStatus> GetAsync(long paymentId)
    {
        try
        {
            var link = await factory.Create().PaymentRequests.GetAsync(paymentId);
            return new(link.OrderCode, link.AmountPaid, Status(link.Status), link.Id,
                (link.Transactions ?? []).Select(t => new VerifiedBankEvent(link.OrderCode, t.Amount, t.Reference,
                    link.Id, "VND", link.Status == PaymentLinkStatus.Paid,
                    JsonSerializer.Serialize(new { source = "payos_api", payment = link }))).ToList());
        }
        catch (Exception e) when (e is PayOSException or HttpRequestException or TaskCanceledException)
        {
            throw new GatewayException("Chưa lấy được trạng thái từ payOS. Vui lòng thử lại.");
        }
    }

    public async Task<VerifiedBankEvent> VerifyAsync(string payload)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<Webhook>(payload);
            if (webhook is null) throw new GatewayException("Webhook không hợp lệ.", true);
            var data = await factory.Create().Webhooks.VerifyAsync(webhook);
            // Only signed data is authoritative; outer success/code and browser returns are not.
            return new(data.OrderCode, data.Amount, data.Reference, data.PaymentLinkId,
                data.Currency, data.Code == "00", payload);
        }
        catch (Exception e) when (e is WebhookException or JsonException or InvalidSignatureException)
        {
            throw new GatewayException("Chữ ký hoặc dữ liệu webhook không hợp lệ.", true);
        }
    }

    private static string Status(PaymentLinkStatus status) => status.ToString().ToUpperInvariant();
}
