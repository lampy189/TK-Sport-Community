using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Globalization;
using System.Security.Claims;
using be_da.Domain.Entities;

namespace be_da.Presentation.Authorization;
public static class AccountPrincipal
{
    public const string Scheme = "TKSC";
    public const string LoginMethod = "tksc:login_method";
    public const string GoogleSubject = "tksc:google_subject";
    public static ClaimsPrincipal Create(Account account, bool developmentLogin = false, long? selectedVenueId = null) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, account.Id.ToString(CultureInfo.InvariantCulture)), new Claim(ClaimTypes.Name, account.FullName), new Claim(ClaimTypes.Email, account.Email), new Claim(ClaimTypes.Role, account.Role), new Claim(GoogleSubject, account.GoogleSubject), new Claim("tksc:venue_id", account.VenueId?.ToString(CultureInfo.InvariantCulture) ?? ""), new Claim(CenterSelection.SelectedClaim, selectedVenueId?.ToString(CultureInfo.InvariantCulture) ?? ""), new Claim(LoginMethod, developmentLogin ? "development" : "google")], Scheme));
}
