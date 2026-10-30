using PayOS;

namespace TKSC.Infrastructure.Interfaces;

public interface IPayOsClientFactory
{
    PayOSClient Create();
}
