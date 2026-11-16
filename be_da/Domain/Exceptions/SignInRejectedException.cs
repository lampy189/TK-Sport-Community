using System.Net.Mail;
using be_da.Domain.Entities;
namespace be_da.Domain.Exceptions;

public sealed class SignInRejectedException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
