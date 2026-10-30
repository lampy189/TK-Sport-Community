using Microsoft.Extensions.Configuration;
using PayOS;
using TKSC.Application.DTOs;
using TKSC.Infrastructure.Interfaces;

namespace TKSC.Infrastructure.Configurations;

public sealed class PayOsClientFactory(IConfiguration config) : IPayOsClientFactory
{
    private string Required(string key) => config[$"Payments:PayOS:{key}"] is { Length: > 0 } value
        ? value : throw new GatewayException("Chưa cấu hình đầy đủ kênh thanh toán payOS.");
    public PayOSClient Create() => new(new PayOSOptions {
        ClientId = Required("ClientId"), ApiKey = Required("ApiKey"), ChecksumKey = Required("ChecksumKey"),
        TimeoutMs = 15000, MaxRetries = 0
    });
}
