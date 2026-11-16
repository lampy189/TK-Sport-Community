using TKSC.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using be_da.Infrastructure.Persistence;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Infrastructure.Persistence.Repositories;
public sealed class StoreRepository(Sep490Context context, IConfiguration configuration, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : IStoreRepository
{
    public async Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct)
    {
        var allowed = await CenterProductIdsAsync(scoped, venueId, ct);
        var query = context.Products.AsQueryable();
        if (allowed is not null)
            query = query.Where(p => allowed.Contains(p.Id));
        var products = await query.AsNoTracking().Where(product => product.IsActive != false && (!product.RequiresPriceApproval || OwnerPrice.Version("product",product.Id,DateTimeOffset.UtcNow)!=null)).OrderBy(product => product.Name).Select(product => new StoreProductItem(product.Id, product.Sku, product.Name, product.Category, OwnerPrice.Effective("product", product.Id, product.UnitPriceVnd, DateTimeOffset.UtcNow), product.StockItem != null ? product.StockItem.QuantityOnHand : 0, product.StockItem != null ? product.StockItem.ReorderLevel : 0)).ToListAsync();
        return products;
    }

    public async Task<List<Product>> FindProductsAsync(List<long> productIds)
    {
        var products=await context.Products.Include(p=>p.StockItem).Where(p=>productIds.Contains(p.Id) && p.IsActive!=false && (!p.RequiresPriceApproval || OwnerPrice.Version("product",p.Id,DateTimeOffset.UtcNow)!=null)).ToListAsync();
        var quoteAt=DateTimeOffset.UtcNow;
        var prices=await context.Products.Where(p=>productIds.Contains(p.Id)).Select(p=>new { p.Id, Price=OwnerPrice.Effective("product",p.Id,p.UnitPriceVnd,quoteAt), Version=OwnerPrice.Version("product",p.Id,quoteAt) }).ToDictionaryAsync(p=>p.Id);
        foreach(var product in products)
        {
            product.UnitPriceVnd=prices[product.Id].Price;
            product.QuotedPriceVersionId=prices[product.Id].Version;
            context.Entry(product).Property(p=>p.UnitPriceVnd).OriginalValue=product.UnitPriceVnd;
            context.Entry(product).Property(p=>p.UnitPriceVnd).IsModified=false;
        }
        return products;
    }
    public async Task SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, bool scoped, long? venueId)
    {
        await using var transaction = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync() : null;
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        foreach (var item in orderItems)
        {
            var product = products.Single(p => p.Id == item.ProductId);
            context.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, PriceVersionId = product.QuotedPriceVersionId, Quantity = item.Quantity, UnitPriceVnd = item.UnitPriceVnd, SubtotalVnd = item.SubtotalVnd, Order = order, Product = product });
        }

        await context.SaveChangesAsync();
        // The original order keeps its center when the buyer later changes browsing center.
        // The database default is server-owned; explicitly verify the newly written association.
        if (scoped)
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.orders SET venue_id={venueId} WHERE id={order.Id}");
        var holdExpiry = DateTimeOffset.UtcNow.AddMinutes(15);
        await GoodsStockHolds.Reserve(context, order, holdExpiry, false, default);
        context.AuditLogs.Add(new AuditLog { ActorId=order.StaffId, EntityType="GoodsOrder", EntityId=order.Id, Action="goods_hold", NewValue=System.Text.Json.JsonSerializer.Serialize(new { ExpiresAt=holdExpiry }) });
        await context.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task<long[]?> CenterProductIdsAsync(bool scoped, long? venueId, CancellationToken ct)
    {
        if (!scoped)
            return null;
        var center = venueId;
        await using var connection = new SqlConnection(configuration.GetConnectionString("Sep490"));
        await SportCenterConnection.OpenAsync(connection, ct, dataScope);
        await using var command = new SqlCommand("SELECT id FROM dbo.products WHERE is_active=1 AND (@venue IS NULL OR venue_id=@venue);", connection);
        command.Parameters.Add("@venue", System.Data.SqlDbType.BigInt).Value = (object? )center ?? DBNull.Value;
        var ids = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            ids.Add(reader.GetInt64(0));
        return ids.ToArray();
    }
}
