
namespace TKSC.Domain.Exceptions;
public sealed class GatewayException(string message, bool invalidSignature = false) : Exception(message)
{
    public bool InvalidSignature { get; } = invalidSignature;
}
