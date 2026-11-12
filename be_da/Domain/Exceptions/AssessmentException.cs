
namespace TKSC.Domain.Exceptions;

public sealed class AssessmentException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
