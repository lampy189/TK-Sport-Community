using TKSC.Application.Interfaces.Services;
using be_da.Application.DTOs;
using TKSC.Application.DTOs;
using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
[ApiController, Authorize]
[Route("api/ticket-aftercare")]
public class TicketAftercareController(ITicketAftercareService service, ITicketSupportService support, TKSC.Application.Interfaces.Services.ITicketRecoveryService recovery, IAntiforgery csrf) : ControllerBase
{
    private long Actor => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;

    private async Task<IActionResult> Write(Func<Task<object>> action)
    {
        try
        {
            await csrf.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        }

        return Ok(await action());
    }

    [HttpGet("incidents"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Incidents() => Ok(await service.SessionsAsync(true));
    [HttpGet("sessions"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Sessions() => Ok(await service.SessionsAsync());
    [HttpPost("sessions/{id:long}/issue"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> Issue(long id, IssueRequest input) => Write(() => service.ReportAsync(id, Actor, input.Reason, input.Cancel));
    [HttpGet("tickets/{id:long}/alternatives")]
    public async Task<IActionResult> Alternatives(long id) => Ok(await service.OptionsAsync(id, Actor));
    [HttpPost("tickets/{id:long}/change")]
    public Task<IActionResult> Change(long id, ChangeRequest input) => Write(() => service.RequestChangeAsync(id, input.SessionId, Actor));
    [HttpPost("changes/{id:long}/cancel")]
    public Task<IActionResult> CancelChange(long id) => Write(() => service.CancelChangeAsync(id, Actor));
    [HttpPost("tickets/{id:long}/refund")]
    public Task<IActionResult> Refund(long id) => Write(() => service.RequestRefundAsync(id, Actor));
    [HttpGet("changes"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Changes() => Ok(await service.ChangesAsync());
    [HttpPost("changes/{id:long}/complete"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> CompleteChange(long id, Settlement input) => Write(() => service.CompleteChangeAsync(id, Actor, input.Evidence, input.BankReference));
    [HttpGet("refunds"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Refunds() => Ok(await service.RefundsAsync());
    [HttpPost("refunds/{id:long}/complete"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> CompleteRefund(long id, Settlement input) => Write(() => service.CompleteRefundAsync(id, Actor, input.Evidence, input.BankReference));
    [HttpPost("refunds/{id:long}/decision"), Authorize(Roles="Manager,manager,Owner,owner")]
    public Task<IActionResult> DecideRefund(long id, RefundDecisionInput input) => Write(() => service.DecideRefundAsync(id,Actor,input));
    [HttpPost("refunds/{id:long}/payout"), Authorize(Roles="Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> Payout(long id, RefundPayoutInput input) => Write(() => service.RecordRefundPayoutAsync(id,Actor,input));
    [HttpPost("refunds/{id:long}/fail"), Authorize(Roles="Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> FailRefund(long id, IssueRequest input) => Write(() => service.FailRefundAsync(id,Actor,input.Reason));
    [HttpGet("refunds/{id:long}/history"), Authorize(Roles="Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> RefundHistory(long id) => Ok(await service.RefundHistoryAsync(id));
    [HttpGet("reviews"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Reviews() => Ok(await service.ReviewsAsync());
    [HttpPost("reviews/{id:long}/approve"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public Task<IActionResult> Approve(long id, FlaggedPaymentApprovalRequest input) => Write(() => recovery.Act(Actor,id,"approve",new PaymentRecoveryInput(input.Evidence,input.BankTransactionId)));
    private bool IsStaff => User.IsInRole("Staff") || User.IsInRole("staff") || User.IsInRole("Manager") || User.IsInRole("manager") || User.IsInRole("Owner") || User.IsInRole("owner");
    [HttpGet("inbox"), Authorize(Roles = "Staff,staff,Manager,manager,Owner,owner")]
    public async Task<IActionResult> Inbox() => Ok(await support.Inbox());
    [HttpGet("tickets/{id:long}/messages")]
    public async Task<IActionResult> Messages(long id)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await support.List(id, Actor, IsStaff));
    }

    [HttpPost("tickets/{id:long}/messages"), RequestSizeLimit(3000000)]
    public Task<IActionResult> SendMessage(long id, TicketSupportInput input) => Write(() => support.Send(id, Actor, IsStaff, input.Content, input.ImageBase64));
    [HttpGet("tickets/{id:long}/messages/{messageId:long}/image")]
    public async Task<IActionResult> MessageImage(long id, long messageId)
    {
        var image = await support.Image(id, messageId, Actor, IsStaff);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Bytes, image.Type);
    }

    [HttpPost("tickets/{id:long}/reports"), Authorize(Roles = "Player,player"), RequestSizeLimit(3000000)]
    public Task<IActionResult> Report(long id, TicketReportInput input) => Write(() => support.Report(id, Actor, input.Category, input.Content, input.ImageBase64));
}
