using TKSC.Domain.Exceptions;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using TKSC.Infrastructure.ExternalServices.Interfaces;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.External;

namespace TKSC.Infrastructure.ExternalServices;
public sealed class PayOsGateway(IConfiguration config, IPayOsClientFactory factory) : IPaymentGateway
{
    private const long OrderCodesPerNamespace = 1_000_000_000;
    private readonly long orderCodeNamespace = config.GetValue<long>("Payments:PayOS:OrderCodeNamespace");

    private long ToGatewayCode(long paymentId)
    {
        if (orderCodeNamespace == 0)
            return paymentId;
        if (orderCodeNamespace is < 1 or > 9_007_198 || paymentId is < 1 or >= OrderCodesPerNamespace)
            throw new GatewayException("Mã giao dịch nằm ngoài dải mã payOS đã cấu hình.");
        return orderCodeNamespace * OrderCodesPerNamespace + paymentId;
    }

    private long ToPaymentId(long gatewayCode, bool webhook = false)
    {
        if (orderCodeNamespace == 0)
            return gatewayCode;
        var paymentId = gatewayCode % OrderCodesPerNamespace;
        if (gatewayCode / OrderCodesPerNamespace != orderCodeNamespace || paymentId <= 0)
            throw new GatewayException("Mã giao dịch không thuộc dải mã payOS của môi trường này.", webhook);
        return paymentId;
    }

    private string Required(string key) => config[$"Payments:PayOS:{key}"] is { Length: > 0 } value ? value : throw new GatewayException("Chưa cấu hình đầy đủ kênh thanh toán payOS.");
    public void ValidateConfiguration()
    {
        _ = Required("ClientId");
        _ = Required("ApiKey");
        _ = Required("ChecksumKey");
        _ = ToGatewayCode(1);
        foreach (var key in new[]
        {
            "ReturnUrl",
            "CancelUrl"
        }

        )
            if (!Uri.TryCreate(Required(key), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                throw new GatewayException("URL quay về của payOS chưa hợp lệ.");
    }

    public CreatePaymentLinkRequest BuildRequest(long paymentId, long orderId, int amount, DateTimeOffset expiry, bool goods = false) => new()
    {
        OrderCode = ToGatewayCode(paymentId),
        Amount = amount,
        Description = $"TKSC {paymentId}",
        ExpiredAt = expiry.ToUnixTimeSeconds(),
        ReturnUrl = QueryHelpers.AddQueryString(Required("ReturnUrl"), goods ? "goodsOrderId" : "orderId", orderId.ToString(CultureInfo.InvariantCulture)),
        CancelUrl = QueryHelpers.AddQueryString(Required("CancelUrl"), goods ? "goodsOrderId" : "orderId", orderId.ToString(CultureInfo.InvariantCulture))
    };
    public Task<GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt) => CreateLinkAsync(paymentId, orderId, amount, expiresAt, false);
    public Task<GatewayCheckout> CreateGoodsAsync(long paymentId, long goodsOrderId, int amount, DateTimeOffset expiresAt) => CreateLinkAsync(paymentId, goodsOrderId, amount, expiresAt, true);
    private async Task<GatewayCheckout> CreateLinkAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt, bool goods)
    {
        ValidateConfiguration();
        try
        {
            var client = factory.Create();
            try
            {
                var link = await client.PaymentRequests.CreateAsync(BuildRequest(paymentId, orderId, amount, expiresAt, goods));
                return new(ToPaymentId(link.OrderCode), link.Amount, link.PaymentLinkId, link.CheckoutUrl, Status(link.Status)) { QrCode = link.QrCode };
            }
            catch (ApiException)
            {
                // A duplicate or timeout may mean the original create succeeded.
                // Retrieve the same orderCode; do not allocate a new attempt on uncertain outcomes.
                var link = await client.PaymentRequests.GetAsync(ToGatewayCode(paymentId));
                return new(ToPaymentId(link.OrderCode), link.Amount, link.Id, $"https://pay.payos.vn/web/{Uri.EscapeDataString(link.Id)}", Status(link.Status));
            }
        }
        catch (Exception e)when (e is PayOSException or HttpRequestException or TaskCanceledException)
        {
            throw new GatewayException("Chưa nhận được link payOS. Hãy thử lại; hệ thống sẽ giữ nguyên mã giao dịch.");
        }
    }

    public async Task<GatewayStatus> GetAsync(long paymentId)
    {
        try
        {
            var link = await factory.Create().PaymentRequests.GetAsync(ToGatewayCode(paymentId));
            var localPaymentId = ToPaymentId(link.OrderCode);
            return new(localPaymentId, link.AmountPaid, Status(link.Status), link.Id, (link.Transactions ?? []).Select(t => new VerifiedBankEvent(localPaymentId, t.Amount, t.Reference, link.Id, "VND", link.Status == PaymentLinkStatus.Paid, JsonSerializer.Serialize(new { source = "payos_api", payment = link }))).ToList());
        }
        catch (Exception e)when (e is PayOSException or HttpRequestException or TaskCanceledException)
        {
            throw new GatewayException("Chưa lấy được trạng thái từ payOS. Vui lòng thử lại.");
        }
    }

    public async Task<VerifiedBankEvent> VerifyAsync(string payload)
    {
        try
        {
            var webhook = JsonSerializer.Deserialize<Webhook>(payload);
            if (webhook is null)
                throw new GatewayException("Webhook không hợp lệ.", true);
            var data = await factory.Create().Webhooks.VerifyAsync(webhook);
            // Only signed data is authoritative; outer success/code and browser returns are not.
            return new(ToPaymentId(data.OrderCode, webhook: true), data.Amount, data.Reference, data.PaymentLinkId, data.Currency, data.Code == "00", payload);
        }
        catch (Exception e)when (e is WebhookException or JsonException or InvalidSignatureException)
        {
            throw new GatewayException("Chữ ký hoặc dữ liệu webhook không hợp lệ.", true);
        }
    }

    private static string Status(PaymentLinkStatus status) => status.ToString().ToUpperInvariant();
}
