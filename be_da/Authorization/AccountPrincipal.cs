using System.Globalization;
using System.Security.Claims;
using be_da.Domain.Entities;

namespace be_da.Authorization;

public static class AccountPrincipal
{
    public const string Scheme = "TKSC";
    public const string GoogleSubject = "tksc:google_subject";

    public static ClaimsPrincipal Create(Account account) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString(CultureInfo.InvariantCulture)),
        new Claim(ClaimTypes.Name, account.FullName),
        new Claim(ClaimTypes.Email, account.Email),
        new Claim(ClaimTypes.Role, account.Role),
        new Claim(GoogleSubject, account.GoogleSubject)
    ], Scheme));
}
