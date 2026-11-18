using be_da.Application.DTOs;
using be_da.Domain.Entities;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class AccountService(IAccountStore repository) : IAccountService
{
    public Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName, CancellationToken cancellationToken) => repository.FindOrRegisterGoogleAsync(subject, email, fullName, cancellationToken);
    public Task<Account?> FindByGoogleSubjectAsync(string subject, CancellationToken cancellationToken) => repository.FindByGoogleSubjectAsync(subject, cancellationToken);
    public Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken) => repository.FindByIdAsync(id, cancellationToken);
    public Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken) => repository.FindByEmailAsync(email, cancellationToken);
}
