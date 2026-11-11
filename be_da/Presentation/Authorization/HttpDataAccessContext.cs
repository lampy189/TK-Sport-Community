using System.Security.Claims;
using be_da.Application.Interfaces.External;

namespace be_da.Presentation.Authorization;
public sealed class HttpDataAccessContext(IHttpContextAccessor accessor) : IDataAccessContext
{
    public string Role => accessor.HttpContext is not { } http ? "service" : http.User.Identity?.IsAuthenticated == true ? http.User.FindFirstValue(ClaimTypes.Role) ?? "denied" : "public";
    public long? VenueId => accessor.HttpContext?.Items["SportCenterId"] as long? ;
    public long? RestrictedVenueId => CenterSelection.RestrictedVenue;
}
