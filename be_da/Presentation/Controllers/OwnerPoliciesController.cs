using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController, Route("api/v1/owner-policies"), Authorize(Roles="Owner,Manager,owner,manager")]
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public sealed class OwnerPoliciesController(IOwnerPolicyService service) : ControllerBase
{
    private long Actor => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
    [HttpGet("options")]
    public Task<object> Options(CancellationToken ct) => service.OptionsAsync(ct);
    [HttpGet]
    public async Task<IActionResult> History(string? code, long? venueId, CancellationToken ct) => Ok(await service.HistoryAsync(code, venueId, ct));
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitPolicyRequest request, CancellationToken ct)
        => Actor <= 0 ? Unauthorized() : (await service.SubmitAsync(request, Actor, ct)).ToResponse();
    [HttpPost("{id:long}/decision"), Authorize(Roles="Owner,owner")]
    public async Task<IActionResult> Decide(long id, DecidePolicyRequest request, CancellationToken ct)
        => Actor <= 0 ? Unauthorized() : (await service.DecideAsync(id, request, Actor, ct)).ToResponse();
}
