using System.Text;
using be_da.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;

[ApiController]
[Route("api/integrations/zalo")]
public sealed class ZaloWebhookController(IZaloWebhookService service) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("webhook")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        using var reader=new StreamReader(Request.Body,new UTF8Encoding(false,true));
        string body;
        try {body=await reader.ReadToEndAsync(ct);}
        catch(DecoderFallbackException){return BadRequest();}
        var result=await service.ReceiveAsync(body,Request.Headers["X-ZEvent-Signature"].ToString(),ct);
        return result switch {
            ZaloReceiveResult.Disabled=>StatusCode(503),
            ZaloReceiveResult.InvalidSignature=>Unauthorized(),
            ZaloReceiveResult.Stale=>BadRequest(new {code="stale_event"}),
            ZaloReceiveResult.RateLimited=>StatusCode(429),
            _=>Ok(new {received=true})
        };
    }
    [Authorize(Roles="SystemAdmin")]
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken ct)=>Ok(await service.StatusAsync(ct));
}
