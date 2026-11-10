using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using be_da.Authorization;
using Microsoft.Data.SqlClient;

namespace TKSC.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/store")]
public class StoreController(Sep490Context context, IConfiguration configuration) : ControllerBase
{
    private long UserId => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : -1;

    [HttpGet("products")]
    public async Task<IActionResult> GetOpenProducts()
    {
        var allowed = await CenterProductIds();
        var query = context.Products.AsQueryable();
        if (allowed is not null) query = query.Where(p => allowed.Contains(p.Id));
        var products = await query
            .AsNoTracking()
            .Where(product => product.IsActive != false)
            .OrderBy(product => product.Name)
            .Select(product => new StoreProductItem(
                product.Id,
                product.Sku,
                product.Name,
                product.Category,
                product.UnitPriceVnd,
                product.StockItem != null ? product.StockItem.QuantityOnHand : 0,
                product.StockItem != null ? product.StockItem.ReorderLevel : 0))
            .ToListAsync();

        return Ok(products);
    }

    [HttpPost("orders/cart")]
    [Authorize(Roles = "Player,Staff,Manager")]
    public async Task<IActionResult> CreateCart([FromBody] CreateCartRequest request)
    {
        if (UserId <= 0)
            return Unauthorized();

        var items = request.Items ?? [];
        if (items.Count == 0)
            return BadRequest(new { code = "empty_cart", message = "Vui lòng chọn ít nhất một món hàng." });

        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        var allowed = await CenterProductIds();
        if (CenterSelection.Scoped && CenterSelection.CurrentVenue is null)
            return BadRequest(new { code = "center_required", message = "Chọn một cơ sở trước khi tạo đơn hàng." });
        if (allowed is not null && productIds.Except(allowed).Any())
            return NotFound(new { code = "product_not_found", message = "Sản phẩm không thuộc cơ sở đang chọn." });
        var products = await context.Products
            .Include(product => product.StockItem)
            .Where(product => productIds.Contains(product.Id) && product.IsActive != false)
            .ToListAsync();

        if (products.Count != productIds.Count)
            return NotFound(new { code = "product_not_found", message = "Một số sản phẩm không còn mở bán hoặc không tồn tại." });

        var orderItems = new List<StoreCartItemResult>();
        var totalAmount = 0;
        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return BadRequest(new { code = "invalid_quantity", message = "Số lượng phải lớn hơn 0." });

            var product = products.SingleOrDefault(candidate => candidate.Id == item.ProductId)
                ?? throw new InvalidOperationException("Product not found while building cart.");

            var quantityOnHand = product.StockItem?.QuantityOnHand ?? 0;
            if (item.Quantity > quantityOnHand)
                return Conflict(new
                {
                    code = "stock_unavailable",
                    message = $"Sản phẩm '{product.Name}' chỉ còn {quantityOnHand} trong kho. Vui lòng giảm số lượng đặt hàng."
                });

            var subtotal = item.Quantity * product.UnitPriceVnd;
            totalAmount += subtotal;
            orderItems.Add(new StoreCartItemResult(product.Id, product.Name, item.Quantity, product.UnitPriceVnd, subtotal));
        }

        var orderCode = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var order = new Order
        {
            OrderCode = orderCode,
            StaffId = UserId,
            BuyerId = User.IsInRole("Player") ? UserId : null,
            CustomerName = User.FindFirstValue(ClaimTypes.Name) ?? "Khách hàng",
            TotalAmount = totalAmount,
            Status = "Cart",
            PaymentState = "Unpaid",
            HandoverState = "Pending"
        };

        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync() : null;
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        foreach (var item in orderItems)
        {
            var product = products.Single(p => p.Id == item.ProductId);
            context.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPriceVnd = item.UnitPriceVnd,
                SubtotalVnd = item.SubtotalVnd,
                Order = order,
                Product = product
            });
        }

        await context.SaveChangesAsync();

        // The original order keeps its center when the buyer later changes browsing center.
        // The database default is server-owned; explicitly verify the newly written association.
        if (CenterSelection.Scoped)
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.orders SET venue_id={CenterSelection.CurrentVenue} WHERE id={order.Id}");
        if (transaction is not null) await transaction.CommitAsync();

        return Ok(new StoreCartOrderResult(
            order.Id,
            order.OrderCode,
            order.TotalAmount,
            order.Status,
            order.PaymentState,
            order.HandoverState,
            orderItems));
    }

    private async Task<long[]?> CenterProductIds()
    {
        if (!CenterSelection.Scoped) return null;
        var center = CenterSelection.CurrentVenue;
        await using var connection = new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection, HttpContext.RequestAborted);
        await using var command = new SqlCommand("SELECT id FROM dbo.products WHERE is_active=1 AND (@venue IS NULL OR venue_id=@venue);", connection);
        command.Parameters.Add("@venue", System.Data.SqlDbType.BigInt).Value = (object?)center ?? DBNull.Value;
        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(HttpContext.RequestAborted);
        while (await reader.ReadAsync(HttpContext.RequestAborted)) ids.Add(reader.GetInt64(0));
        return ids.ToArray();
    }
}
