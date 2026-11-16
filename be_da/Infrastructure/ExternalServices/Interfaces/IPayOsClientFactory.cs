using PayOS;

namespace TKSC.Infrastructure.ExternalServices.Interfaces;
public interface IPayOsClientFactory
{
    PayOSClient Create();
}
