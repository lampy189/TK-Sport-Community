using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.Services;

namespace TKSC.Controllers;

// UC-31: Staff records delivery and the customer's receipt for counter orders.
<<<<<<< HEAD
// Player self-confirmation for online orders needs the order to name its player (not in the current schema).
=======
// The buyer of an online order can also confirm receipt through PlayerGoodsOrdersController.
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
[ApiController]
[Authorize(Roles = "staff,Staff")]
[Route("api/v1/commerce/goods-orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class GoodsHandoverController(GoodsPaymentService service, IAntiforgery antiforgery) : ControllerBase
{
    private long ActorId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
<<<<<<< HEAD
    [HttpPost("{id:long}/deliver")]
    public Task<IActionResult> Deliver(long id, CancellationToken ct) => Mutate(() => service.DeliverAsync(id, ActorId, ct));
    [HttpPost("{id:long}/receive")]
=======
    [HttpPost("{id:long:min(1)}/deliver")]
    public Task<IActionResult> Deliver(long id, CancellationToken ct) => Mutate(() => service.DeliverAsync(id, ActorId, ct));
    [HttpPost("{id:long:min(1)}/receive")]
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    public Task<IActionResult> Receive(long id, CancellationToken ct) => Mutate(() => service.ReceiveAsync(id, ActorId, ct));
    private async Task<IActionResult> Mutate(Func<Task<GoodsOrderView>> action)
    {
        if (ActorId <= 0) return Unauthorized();
<<<<<<< HEAD
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest();
=======
        if (Request.Headers["X-TKSC-Request"] != "1") return BadRequest(new { code = "invalid_request", message = "Yêu cầu không hợp lệ." });
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        { return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." }); }
        return Ok(await action());
    }
}
