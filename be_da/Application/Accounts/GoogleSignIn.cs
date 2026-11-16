using System.Net.Mail;
using be_da.Application.Abstractions;
using be_da.Domain.Entities;

namespace be_da.Application.Accounts;

public sealed class SignInRejectedException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed class GoogleSignIn(IAccountStore accounts)
{
    // UC-01/02/49, FR-01.3, BR-47: only trusted provider data reaches this boundary.
    public async Task<Account> ExecuteAsync(string? subject, string? email, string? name,
        bool emailVerified, CancellationToken cancellationToken, string? portal = null)
    {
        if (!emailVerified || string.IsNullOrWhiteSpace(subject) || subject.Length > 255
            || string.IsNullOrWhiteSpace(email) || email.Length > 320
            || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email
            || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 255)
            throw new SignInRejectedException("invalid_google_identity");

        if (!LoginPortal.IsValid(portal)) throw new SignInRejectedException("invalid_login_portal");
        var account = portal is "admin" or "staff"
            ? await accounts.FindByGoogleSubjectAsync(subject, cancellationToken)
                ?? throw new SignInRejectedException("portal_access_denied")
            : await accounts.FindOrRegisterGoogleAsync(subject, email.Trim(), name.Trim(), cancellationToken);
        if (!LoginPortal.Allows(portal, account.Role)) throw new SignInRejectedException("portal_access_denied");
        if (!account.CanSignIn) throw new SignInRejectedException("account_unavailable");
        return account;
    }
}
