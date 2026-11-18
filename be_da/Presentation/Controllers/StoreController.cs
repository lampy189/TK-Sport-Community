using TKSC.Domain.Exceptions;
using be_da.Presentation.Filters;
using be_da.Application.Interfaces.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using be_da.Presentation.Authorization;

namespace TKSC.Presentation.Controllers;
[ApiController]
[Authorize]
[Route("api/v1/store")]
public class StoreController(IStoreService service, IAntiforgery antiforgery) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;

    [HttpGet("products")]
    public async Task<IActionResult> GetOpenProducts() => Ok(await service.ProductsAsync(CenterSelection.Scoped, CenterSelection.CurrentVenue, HttpContext.RequestAborted));
    [HttpPost("orders/cart")]
    [Authorize(Roles = "Player,Staff,Manager")]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (UserId <= 0)
            return Unauthorized();
        // [ValidateAntiForgeryToken] needs MVC view services, which this API-only app does not register
        // (every cart request failed with HTTP 500); validate the token explicitly like the other controllers.
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return BadRequest(new { code = "invalid_csrf_token", message = "Phiên bảo mật đã thay đổi. Vui lòng thử lại." });
        }

        return (await service.CreateCartAsync(request, UserId, User.IsInRole("Player"), User.FindFirstValue(ClaimTypes.Name), CenterSelection.Scoped, CenterSelection.CurrentVenue, idempotencyKey, HttpContext.RequestAborted)).ToResponse();
    }
}
