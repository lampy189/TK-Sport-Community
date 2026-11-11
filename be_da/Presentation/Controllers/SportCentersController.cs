using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Data;
using System.Security.Claims;
using be_da.Presentation.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;
[ApiController, Authorize, Route("api/v1/sport-centers")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SportCentersController(ISportCenterService service, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Owner,Manager,Staff,Referee,owner,manager,staff,referee")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await service.ListAsync(ct);
        return Ok(new { canViewAll = User.IsInRole("Owner") || User.IsInRole("owner"), assignedVenueId = HttpContext.Items["SportCenterId"], selectedVenueId = CenterSelection.CurrentVenue, centers = rows });
    }

    [HttpGet("personnel"), Authorize(Roles = "Owner,owner")]
    public async Task<IActionResult> Personnel(CancellationToken ct)
    {
        var rows = await service.PersonnelAsync(ct);
        return Ok(rows);
    }

    [HttpPut("personnel/{id:long}"), Authorize(Roles = "Owner,owner")]
    public async Task<IActionResult> Assign(long id, AssignRequest request, CancellationToken ct)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token" });
        }

        var changed = await service.AssignAsync(id, request.VenueId, long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct);
        return changed ? NoContent() : BadRequest(new { message = "Nhân sự hoặc Sport Center không hợp lệ." });
    }
}
