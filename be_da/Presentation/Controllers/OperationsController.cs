using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using be_da.Presentation.Filters;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace be_da.Presentation.Controllers;
[ApiController,Authorize,Route("api/operations")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class OperationsController(IOperationsService service,IAntiforgery antiforgery):ControllerBase
{
    private long Actor=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string Role=>User.FindFirstValue(ClaimTypes.Role)!;
    [HttpGet("setup"),Authorize(Roles="Owner,Manager")]
    public async Task<IActionResult> Setup(CancellationToken ct)=>Ok(await service.SetupAsync(ct));
    [HttpPost("venues"),Authorize(Roles="Owner")]
    public Task<IActionResult> CreateVenue(VenueEdit r,CancellationToken ct)=>Mutate(()=>service.SaveVenueAsync(Actor,null,r,ct));
    [HttpPut("venues/{id:long}"),Authorize(Roles="Owner")]
    public Task<IActionResult> EditVenue(long id,VenueEdit r,CancellationToken ct)=>Mutate(()=>service.SaveVenueAsync(Actor,id,r,ct));
    [HttpPost("courts"),Authorize(Roles="Owner,Manager")]
    public Task<IActionResult> CreateCourt(CourtEdit r,CancellationToken ct)=>Mutate(()=>service.SaveCourtAsync(Actor,null,r,ct));
    [HttpPut("courts/{id:long}"),Authorize(Roles="Owner,Manager")]
    public Task<IActionResult> EditCourt(long id,CourtEdit r,CancellationToken ct)=>Mutate(()=>service.SaveCourtAsync(Actor,id,r,ct));
    [HttpPut("personnel/{id:long}"),Authorize(Roles="Owner")]
    public Task<IActionResult> Personnel(long id,PersonnelEdit r,CancellationToken ct)=>Mutate(()=>service.PersonnelAsync(Actor,id,r,ct));
    [HttpGet("access"),Authorize(Roles="Owner,Manager,SystemAdmin")]
    public async Task<IActionResult> Access(CancellationToken ct)=>Ok(await service.AccessAsync(Actor,Role,ct));
    [HttpPost("access"),Authorize(Roles="Owner,Manager,SystemAdmin")]
    public Task<IActionResult> Propose(AccessProposal r,CancellationToken ct)=>Mutate(()=>service.ProposeAsync(Actor,Role,r,ct));
    [HttpPost("access/{id:long}/decision"),Authorize(Roles="Owner,Manager")]
    public Task<IActionResult> Decide(long id,ApprovalDecision r,CancellationToken ct)=>Mutate(()=>service.DecideAsync(Actor,Role,id,r,ct));
    [HttpPost("access/{id:long}/execute"),Authorize(Roles="SystemAdmin")]
    public Task<IActionResult> Execute(long id,CancellationToken ct)=>Mutate(()=>service.ExecuteAsync(Actor,id,ct));
    [HttpPost("access/{id:long}/verify"),Authorize(Roles="SystemAdmin")]
    public Task<IActionResult> Verify(long id,CancellationToken ct)=>Mutate(()=>service.VerifyAsync(Actor,id,ct));
    [HttpGet("overview"),Authorize(Roles="Owner,Manager,Staff,Referee,SystemAdmin")]
    public async Task<IActionResult> Overview(CancellationToken ct)=>Ok(await service.OverviewAsync(Actor,ct));
    [HttpPost("feedback"),Authorize(Roles="Owner,Manager,Staff,Referee,SystemAdmin")]
    public Task<IActionResult> Feedback(TrialFeedback r,CancellationToken ct)=>Mutate(()=>service.FeedbackAsync(Actor,r,ct));
    private async Task<IActionResult> Mutate(Func<Task<OperationResult>> action)
    {
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch(AntiforgeryValidationException) { return BadRequest(new { code="invalid_csrf_token" }); }
        return (await action()).ToResponse();
    }
}
