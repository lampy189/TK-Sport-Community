using System.Globalization;
using System.Security.Claims;
using be_da.Domain.Entities;

namespace be_da.Authorization;

public static class AccountPrincipal
{
    public const string Scheme = "TKSC";
    public const string LoginMethod = "tksc:login_method";
    public const string GoogleSubject = "tksc:google_subject";

<<<<<<< HEAD
    public static ClaimsPrincipal Create(Account account, bool developmentLogin = false) => new(new ClaimsIdentity([
=======
    public static ClaimsPrincipal Create(Account account, bool developmentLogin = false, long? selectedVenueId = null) => new(new ClaimsIdentity([
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        new Claim(ClaimTypes.NameIdentifier, account.Id.ToString(CultureInfo.InvariantCulture)),
        new Claim(ClaimTypes.Name, account.FullName),
        new Claim(ClaimTypes.Email, account.Email),
        new Claim(ClaimTypes.Role, account.Role),
        new Claim(GoogleSubject, account.GoogleSubject),
<<<<<<< HEAD
=======
        new Claim(SportCenterConnection.VenueClaim, account.VenueId?.ToString(CultureInfo.InvariantCulture) ?? ""),
        new Claim(CenterSelection.SelectedClaim, selectedVenueId?.ToString(CultureInfo.InvariantCulture) ?? ""),
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        new Claim(LoginMethod, developmentLogin ? "development" : "google")
    ], Scheme));
}
