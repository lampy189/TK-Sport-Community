using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController, Route("api/v1/owner-policies"), Authorize(Roles="Owner,Manager,owner,manager")]
[ResponseCache(NoStore=true, Location=ResponseCacheLocation.None)]
public sealed class OwnerPoliciesController(IOwnerPolicyService service, IAntiforgery antiforgery) : ControllerBase
{
    private long Actor => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;
    private async Task<IActionResult> Write(Func<Task<OperationResult>> action)
    {
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest(new { message="Thiếu mã yêu cầu ứng dụng." });
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException) { return BadRequest(new { code="invalid_csrf_token",message="Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        return Actor <= 0 ? Unauthorized() : (await action()).ToResponse();
    }
    [HttpGet("options")]
    public Task<object> Options(CancellationToken ct) => service.OptionsAsync(ct);
    [HttpGet]
    public async Task<IActionResult> History(string? code, long? venueId, CancellationToken ct) => Ok(await service.HistoryAsync(code, venueId, ct));
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitPolicyRequest request, CancellationToken ct)
        => await Write(() => service.SubmitAsync(request, Actor, ct));
    [HttpPost("{id:long}/decision"), Authorize(Roles="Owner,owner")]
    public async Task<IActionResult> Decide(long id, DecidePolicyRequest request, CancellationToken ct)
        => await Write(() => service.DecideAsync(id, request, Actor, ct));
}
