using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Controllers;

// UC-31: Staff records delivery and the customer's receipt for counter orders.
// Player self-confirmation for online orders needs the order to name its player (not in the current schema).
[ApiController]
[Authorize(Roles = "staff,Staff")]
[Route("api/v1/commerce/goods-orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoodsHandoverController(GoodsPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpPost("{id:long}/deliver")]
    public Task<IActionResult> Deliver(long id, CancellationToken ct) => Mutate(() => service.DeliverAsync(id, ActorId, ct));
    [HttpPost("{id:long}/receive")]
    public Task<IActionResult> Receive(long id, CancellationToken ct) => Mutate(() => service.ReceiveAsync(id, ActorId, ct));
    private async Task<IActionResult> Mutate(Func<Task<GoodsOrderView>> action)
    {
        if (ActorId <= 0) return Unauthorized();
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest();
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        { return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        return Ok(await action());
    }
}
