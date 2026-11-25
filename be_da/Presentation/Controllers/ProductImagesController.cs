using System.Security.Claims;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be_da.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Player,Staff,Manager")]
[Route("api/products/{id:long:min(1)}/image")]
public sealed class ProductImagesController(IProductImageService images, IAntiforgery antiforgery) : ControllerBase
{
    private const int MaxBytes = 2 * 1024 * 1024;

    [HttpGet]
    public async Task<IActionResult> Read(long id, CancellationToken ct)
    {
        long? buyerId = null;
        if (User.IsInRole("Player"))
        {
            if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
                return Unauthorized();
            buyerId = actor;
        }
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var image = await images.ReadAsync(id, buyerId, ct);
        return image is null ? NotFound() : File(image.Data, image.ContentType);
    }

    [HttpPut]
    [Authorize(Roles = "Staff,Manager")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBytes + 64 * 1024)]
    public async Task<IActionResult> Upload(long id, IFormFile? file, CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
            return BadRequest(new { code = "invalid_csrf_token" });
        if (file is null || file.Length is <= 0 or > MaxBytes)
            return BadRequest(new { message = "Chọn ảnh JPG, PNG hoặc WebP tối đa 2 MB." });

        using var stream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(stream, ct);
        return ToResponse(await images.UploadAsync(id, stream.ToArray(), ct));
    }

    [HttpDelete]
    [Authorize(Roles = "Staff,Manager")]
    public async Task<IActionResult> RestoreDefault(long id, CancellationToken ct)
    {
        if (!await antiforgery.IsRequestValidAsync(HttpContext))
            return BadRequest(new { code = "invalid_csrf_token" });
        return ToResponse(await images.RestoreAsync(id, ct));
    }

    private IActionResult ToResponse(ProductImageResult result) => result switch
    {
        ProductImageResult.Saved => NoContent(),
        ProductImageResult.Missing => NotFound(new { message = "Không tìm thấy mặt hàng tại cơ sở của bạn." }),
        ProductImageResult.Invalid => BadRequest(new { message = "Tệp đã chọn không phải ảnh JPG, PNG hoặc WebP hợp lệ." }),
        _ => StatusCode(503, new { message = "DB chưa hỗ trợ tải ảnh. Cần chạy migration 023-product-images.sql." })
    };
}
