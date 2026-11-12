using TKSC.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
        else if (dataScope?.VenueId is long venue)
            query = query.Where(p => p.VenueId == venue);
        var products = await query.AsNoTracking().Where(product => product.IsActive != false).OrderBy(product => product.Name).Select(product => new StoreProductItem(product.Id, product.Sku, product.Name, product.Category, product.UnitPriceVnd, product.StockItem != null ? product.StockItem.QuantityOnHand : 0, product.StockItem != null ? product.StockItem.ReorderLevel : 0, product.StockItem != null ? product.StockItem.QuantityOnHand - product.StockItem.ReservedQuantity : 0)).ToListAsync();
        return products;
    }

    public Task<List<Product>> FindProductsAsync(List<long> productIds) => context.Products.Include(product => product.StockItem).Where(product => productIds.Contains(product.Id) && product.IsActive != false).ToListAsync();
    public async Task<long?> SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, bool scoped, long? venueId)
    {
        await using var transaction = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync() : null;
        // Take the reservation lock before writing the order, so concurrent carts queue instead of deadlocking.
        await context.Database.ExecuteSqlRawAsync(StockReservationSql.LockSql);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        foreach (var item in orderItems)
        {
            var product = products.Single(p => p.Id == item.ProductId);
            context.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, Quantity = item.Quantity, UnitPriceVnd = item.UnitPriceVnd, SubtotalVnd = item.SubtotalVnd, Order = order, Product = product });
        }

        await context.SaveChangesAsync();
        // The original order keeps its center when the buyer later changes browsing center.
        // The database default is server-owned; explicitly verify the newly written association.
        if (scoped)
            await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.orders SET venue_id={venueId} WHERE id={order.Id}");
        // Hold the goods in the same transaction: two buyers cannot both take the last items.
        var shortProduct = await StockReservationSql.ReserveAsync(context.Database.GetDbConnection(), context.Database.CurrentTransaction?.GetDbTransaction(), order.Id, CancellationToken.None);
        if (shortProduct is not null)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            context.ChangeTracker.Clear();
            return shortProduct;
        }

        if (transaction is not null)
            await transaction.CommitAsync();
        return null;
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
