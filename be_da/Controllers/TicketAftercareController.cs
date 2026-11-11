using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TKSC.Application.Services;
namespace TKSC.Controllers;
[ApiController,Authorize]
[Route("api/ticket-aftercare")]
public class TicketAftercareController(TicketAftercareService service,TicketSupportService support,IAntiforgery csrf):ControllerBase
{
 private long Actor=>long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:-1;
 private async Task<IActionResult> Write(Func<Task<object>> action)
 {
  try{await csrf.ValidateRequestAsync(HttpContext);}catch(AntiforgeryValidationException){return BadRequest(new{code="invalid_csrf_token",message="Phiên bảo mật đã thay đổi. Vui lòng thử lại."});}
  return Ok(await action());
 }
<<<<<<< HEAD
=======
 [HttpGet("incidents"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Incidents()=>Ok(await service.SessionsAsync(true));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
 [HttpGet("sessions"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Sessions()=>Ok(await service.SessionsAsync());
 [HttpPost("sessions/{id:long}/issue"),Authorize(Roles="Staff,staff,Manager,manager")]
 public Task<IActionResult> Issue(long id,IssueRequest input)=>Write(()=>service.ReportAsync(id,Actor,input.Reason,input.Cancel));
 [HttpGet("tickets/{id:long}/alternatives")]
 public async Task<IActionResult> Alternatives(long id)=>Ok(await service.OptionsAsync(id,Actor));
 [HttpPost("tickets/{id:long}/change")]
 public Task<IActionResult> Change(long id,ChangeRequest input)=>Write(()=>service.RequestChangeAsync(id,input.SessionId,Actor));
 [HttpPost("changes/{id:long}/cancel")]
 public Task<IActionResult> CancelChange(long id)=>Write(()=>service.CancelChangeAsync(id,Actor));
 [HttpPost("tickets/{id:long}/refund")]
 public Task<IActionResult> Refund(long id)=>Write(()=>service.RequestRefundAsync(id,Actor));
 [HttpGet("changes"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Changes()=>Ok(await service.ChangesAsync());
 [HttpPost("changes/{id:long}/complete"),Authorize(Roles="Staff,staff,Manager,manager")]
 public Task<IActionResult> CompleteChange(long id,Settlement input)=>Write(()=>service.CompleteChangeAsync(id,Actor,input.Evidence,input.BankReference));
 [HttpGet("refunds"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Refunds()=>Ok(await service.RefundsAsync());
 [HttpPost("refunds/{id:long}/complete"),Authorize(Roles="Staff,staff,Manager,manager")]
 public Task<IActionResult> CompleteRefund(long id,Settlement input)=>Write(()=>service.CompleteRefundAsync(id,Actor,input.Evidence,input.BankReference));
 [HttpGet("reviews"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Reviews()=>Ok(await service.ReviewsAsync());
 [HttpPost("reviews/{id:long}/approve"),Authorize(Roles="Staff,staff,Manager,manager")]
 public Task<IActionResult> Approve(long id,FlaggedPaymentApprovalRequest input)=>Write(()=>service.ApprovePaymentAsync(id,input.BankTransactionId,Actor,input.Evidence));
 private bool IsStaff => User.IsInRole("Staff") || User.IsInRole("staff") || User.IsInRole("Manager") || User.IsInRole("manager");
 [HttpGet("inbox"),Authorize(Roles="Staff,staff,Manager,manager")]
 public async Task<IActionResult> Inbox()=>Ok(await support.Inbox());
 [HttpGet("tickets/{id:long}/messages")]
 public async Task<IActionResult> Messages(long id)
 {
  Response.Headers.CacheControl = "no-store";
  return Ok(await support.List(id,Actor,IsStaff));
 }
 [HttpPost("tickets/{id:long}/messages"),RequestSizeLimit(3000000)]
 public Task<IActionResult> SendMessage(long id,TicketSupportInput input)=>Write(()=>support.Send(id,Actor,IsStaff,input.Content,input.ImageBase64));
 [HttpGet("tickets/{id:long}/messages/{messageId:long}/image")]
 public async Task<IActionResult> MessageImage(long id,long messageId)
 {
  var image=await support.Image(id,messageId,Actor,IsStaff);
  Response.Headers.CacheControl="no-store";
  Response.Headers["X-Content-Type-Options"]="nosniff";
  return File(image.Bytes,image.Type);
 }
<<<<<<< HEAD
=======
 [HttpPost("tickets/{id:long}/reports"),Authorize(Roles="Player,player"),RequestSizeLimit(3000000)]
 public Task<IActionResult> Report(long id,TicketReportInput input)=>Write(()=>support.Report(id,Actor,input.Category,input.Content,input.ImageBase64));
 public record TicketReportInput(string? Category,string? Content,string? ImageBase64);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
 public record TicketSupportInput(string? Content,string? ImageBase64);
 public record IssueRequest(string? Reason,bool Cancel);
 public record ChangeRequest(long SessionId);
 public record Settlement(string? Evidence,string? BankReference);
 public record FlaggedPaymentApprovalRequest(long BankTransactionId,string? Evidence);
}
