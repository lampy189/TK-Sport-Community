using be_da.Domain.Exceptions;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using be_da.Application.Services.Accounts;
using be_da.Domain.Entities;

namespace be_da.Presentation.Authorization;
// Selected browsing center is session state, distinct from a personnel assignment.
public sealed class CenterSelection(IServiceProvider services, IConfiguration configuration)
{
    public const string SelectedClaim = "tksc:selected_center";
    public bool Enabled => configuration.GetValue("SportCenters:Enabled", false);

    public async Task<IReadOnlyList<VenueSummary>> Choices(Account? account, CancellationToken ct)
    {
        var centers = await services.GetRequiredService<ISessionService>().GetVenuesAsync(ct);
        return account is not null && Personnel(account.Role) ? centers.Where(v => v.Id == account.VenueId).ToList() : centers;
    }

    public static bool Personnel(string? role) => role?.ToLowerInvariant()is "staff" or "manager" or "referee";
    public async Task<long?> Resolve(Account account, long? requested, CancellationToken ct)
    {
        if (!Enabled)
            return null;
        if (Personnel(account.Role))
        {
            if (account.VenueId is null || requested.HasValue && requested != account.VenueId)
                throw new SignInRejectedException("center_access_denied");
            requested = account.VenueId;
        }

        if (requested is null)
            return null; // Player must choose before business APIs; Owner may view all.
        if (!(await Choices(account, ct)).Any(v => v.Id == requested))
            throw new SignInRejectedException("center_unavailable");
        return requested;
    }

    private static readonly HttpContextAccessor Accessor = new();
    public static long? CurrentVenue => Scoped ? Accessor.HttpContext?.Items["SportCenterId"] as long? : null;

    public static long? RestrictedVenue
    {
        get
        {
            var http = Accessor.HttpContext;
            if (http?.Items["CenterScopeEnabled"] is not true)
                return null;
            if (http.User.IsInRole("Owner") || http.User.IsInRole("owner"))
                return null;
            return http.Items["SportCenterId"] as long? ;
        }
    }

    public static bool Scoped => Accessor.HttpContext?.Items["CenterScopeEnabled"] is true;
}
