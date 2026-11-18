using System.Security.Claims;
using be_da.Presentation.Authorization;

namespace be_da.Presentation.Authorization;
public sealed class SportCenterMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IWebHostEnvironment? environment = null)
    {
        if (context.User.Identity?.IsAuthenticated == true && long.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            long? value = long.TryParse(context.User.FindFirstValue("tksc:venue_id"), out var assigned) ? assigned : null;
            if (value is long venueId)
                context.Items["SportCenterId"] = venueId;
            var role = context.User.FindFirstValue(ClaimTypes.Role);
            if (configuration.GetValue("SportCenters:Enabled", false))
            {
                context.Items["CenterScopeEnabled"] = true;
                long? selected = long.TryParse(context.User.FindFirstValue(CenterSelection.SelectedClaim), out var selectedId) ? selectedId : null;
                var effective = CenterSelection.Personnel(role) ? (selected == value ? value : null) : selected;
                context.Items["SportCenterId"] = effective;
                var exempt = context.Request.Path.StartsWithSegments("/api/auth") || context.Request.Path == "/api/sessions/venues";
                if (role?.ToLowerInvariant()is "player" or "staff" or "manager" or "referee" && effective is null && !exempt)
                {
                    context.Response.StatusCode = 409;
                    await context.Response.WriteAsJsonAsync(new { code = "center_selection_required", message = "Vui lòng chọn cơ sở đang hoạt động hoặc liên hệ Owner để được phân công." });
                    return;
                }
            }

            var requireAssignment = environment?.IsDevelopment() != true || configuration.GetValue("SportCenters:RequireAssignment", true);
            if (requireAssignment && new[]
            {
                "staff",
                "manager",
                "referee"
            }.Contains(role?.ToLowerInvariant()) && value is not long && !context.Request.Path.StartsWithSegments("/api/auth") && !context.Request.Path.StartsWithSegments("/api/v1/sport-centers"))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { code = "sport_center_required", message = "Tài khoản chưa được phân công Sport Center. Liên hệ Owner." });
                return;
            }
        }

        await next(context);
    }
}
