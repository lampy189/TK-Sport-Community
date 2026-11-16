using be_da.Application.DTOs;
using be_da.Domain.Entities;

namespace be_da.Application.Interfaces.Services;
public interface IAccountService
{
    Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName, CancellationToken cancellationToken);
    Task<Account?> FindByGoogleSubjectAsync(string subject, CancellationToken cancellationToken);
    Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken);
    Task<Account?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}
