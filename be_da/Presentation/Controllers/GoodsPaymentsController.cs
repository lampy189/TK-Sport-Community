using be_da.Application.DTOs;
using TKSC.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
[ApiController]
[Authorize(Roles = "staff,Staff")]
[Route("api/v1/commerce/goods-orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoodsPaymentsController(IGoodsPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.ListAsync(ct));
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Read(long id, CancellationToken ct) => Ok(await service.ReadAsync(id, ct));
    [HttpPost("{id:long}/payment-instructions")]
    public Task<IActionResult> Instructions(long id, CancellationToken ct) => Mutate(() => service.InstructionsAsync(id, ActorId, ct));
    [HttpPost("{id:long}/confirm-transfer")]
    public Task<IActionResult> ConfirmTransfer(long id, CancellationToken ct) => Mutate(() => service.ConfirmTransferAsync(id, ct));
    [HttpPost("{id:long}/confirm-cash")]
    public Task<IActionResult> ConfirmCash(long id, CashRequest body, CancellationToken ct) => Mutate(() => service.CashAsync(id, ActorId, body.ReceivedAmount, ct));
    [HttpPost("{id:long}/invoice")]
    public Task<IActionResult> Invoice(long id, CancellationToken ct) => Mutate(() => service.InvoiceAsync(id, ActorId, ct));
    private async Task<IActionResult> Mutate(Func<Task<GoodsOrderView>> action)
    {
        if (ActorId <= 0)
            return Unauthorized();
        if (Request.Headers["X-TKSC-Request"] != "1")
            return BadRequest();
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        }

        return Ok(await action());
    }
}
