
namespace TKSC.Domain.Exceptions;
public sealed class PaymentException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
