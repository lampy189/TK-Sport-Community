using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Controllers;

// UC-31: Staff records delivery and the customer's receipt for counter orders.
// The buyer of an online order can also confirm receipt through PlayerGoodsOrdersController.
[ApiController]
[Authorize(Roles = "staff,Staff")]
[Route("api/v1/commerce/goods-orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoodsHandoverController(GoodsPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    [HttpPost("{id:long:min(1)}/deliver")]
    public Task<IActionResult> Deliver(long id, CancellationToken ct) => Mutate(() => service.DeliverAsync(id, ActorId, ct));
    [HttpPost("{id:long:min(1)}/receive")]
    public Task<IActionResult> Receive(long id, CancellationToken ct) => Mutate(() => service.ReceiveAsync(id, ActorId, ct));
    private async Task<IActionResult> Mutate(Func<Task<GoodsOrderView>> action)
    {
        if (ActorId <= 0) return Unauthorized();
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest(new { code = "invalid_request", message = "Yêu cầu không hợp lệ." });
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        { return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        return Ok(await action());
    }
}
