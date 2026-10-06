using be_da.Domain.Entities;

namespace be_da.Application.Abstractions;

public interface IAccountStore
{
    // Atomic: find by Google subject, or create a Player and profile. Never link by email.
    Task<Account> FindOrRegisterGoogleAsync(string subject, string email, string fullName,
        CancellationToken cancellationToken);
    Task<Account?> FindByIdAsync(long id, CancellationToken cancellationToken);
}
