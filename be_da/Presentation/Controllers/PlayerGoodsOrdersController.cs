using TKSC.Application.DTOs;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Presentation.Controllers;
// UC-31 (SWL 3 "Receive order"): a Player sees their own online orders and confirms receipt after Staff delivers.
[ApiController]
[Authorize(Roles = "player,Player")]
[Route("api/v1/commerce/my-goods-orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlayerGoodsOrdersController(IGoodsPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => ActorId <= 0 ? Unauthorized() : Ok(await service.ListForBuyerAsync(ActorId, ct));
    [HttpGet("{id:long:min(1)}")]
    public async Task<IActionResult> Read(long id, CancellationToken ct) => ActorId <= 0 ? Unauthorized() : Ok(await service.ReadForBuyerAsync(id, ActorId, ct));
    [HttpGet("{id:long:min(1)}/invoice")]
    public async Task<IActionResult> Invoice(long id, CancellationToken ct) => ActorId <= 0 ? Unauthorized() : Ok(await service.InvoiceForBuyerAsync(id, ActorId, ct));
    [HttpPost("{id:long:min(1)}/payment-instructions")]
    public Task<IActionResult> Checkout(long id, CancellationToken ct) => Mutate(async () => await service.CheckoutForBuyerAsync(id, ActorId, ct));
    [HttpPost("{id:long:min(1)}/refresh")]
    public Task<IActionResult> Refresh(long id, CancellationToken ct) => Mutate(async () => await service.RefreshForBuyerAsync(id, ActorId, ct));
    [HttpPost("{id:long:min(1)}/cancel")]
    public Task<IActionResult> Cancel(long id, CancellationToken ct) => Mutate(async () => await service.CancelAsync(id, ActorId, ActorId, ct));
    [HttpPost("{id:long:min(1)}/receive")]
    public Task<IActionResult> Receive(long id, CancellationToken ct) => Mutate(async () => await service.ReceiveAsBuyerAsync(id, ActorId, ct));
    private async Task<IActionResult> Mutate(Func<Task<object>> action)
    {
        if (ActorId <= 0)
            return Unauthorized();
        if (Request.Headers["X-TKSC-Request"] != "1")
            return BadRequest(new { code = "invalid_request" });
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
