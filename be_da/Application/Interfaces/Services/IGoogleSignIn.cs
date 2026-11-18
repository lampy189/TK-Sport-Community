using be_da.Domain.Exceptions;
using be_da.Application.DTOs;
using System.Net.Mail;
using be_da.Application.Interfaces.Repositories;
using be_da.Domain.Entities;
using be_da.Application.Services.Accounts;

namespace be_da.Application.Interfaces.Services;
public interface IGoogleSignIn
{
    Task<Account> ExecuteAsync(string? subject, string? email, string? name, bool emailVerified, CancellationToken cancellationToken, string? portal = null);
}
