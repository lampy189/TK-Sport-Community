using TKSC.Domain.Exceptions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using be_da.Application.DTOs;
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
    public async Task<OperationResult?> FindIdempotentCartAsync(long actorId, string idempotencyKey, string fingerprint, CancellationToken ct)
    {
        var json = await context.AuditLogs.AsNoTracking()
            .Where(a => a.ActorId == actorId && a.Action == "goods_order_idempotency" && a.OldValue == idempotencyKey)
            .OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefaultAsync(ct);
        if (json is null) return null;
        using var document = JsonDocument.Parse(json);
        var saved = document.RootElement;
        if (!saved.TryGetProperty("fingerprint", out var savedFingerprint) || savedFingerprint.GetString() != fingerprint)
            return OperationResult.Conflict(new { code = "IDEMPOTENCY_KEY_REUSED", message = "Khóa này đã được dùng cho một giỏ hàng khác." });
        var orderId = saved.GetProperty("orderId").GetInt64();
        var order = await context.Orders.AsNoTracking().Include(o => o.OrderItems).ThenInclude(i => i.Product).SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
            return OperationResult.Conflict(new { code = "IDEMPOTENCY_RESULT_MISSING", message = "Không thể khôi phục đơn của lần gửi trước. Cần đối soát." });
        var lines = order.OrderItems.OrderBy(i => i.Id).Select(i => new StoreCartItemResult(i.ProductId, i.Product.Name, i.Quantity, i.UnitPriceVnd, i.SubtotalVnd)).ToList();
        DateTimeOffset? expiresAt = saved.TryGetProperty("expiresAt", out var savedExpiry) && savedExpiry.TryGetDateTimeOffset(out var parsedExpiry) ? parsedExpiry : null;
        return OperationResult.Success(new StoreCartOrderResult(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, lines, expiresAt));
    }
    public async Task<OperationResult> SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, long actorId, bool scoped, long? venueId, string idempotencyKey, string fingerprint, DateTimeOffset expiresAt, CancellationToken ct)
    {
        if (scoped && venueId is null)
            return OperationResult.Invalid(new { code = "center_required", message = "Chọn một cơ sở trước khi tạo đơn hàng." });
        await using var transaction = context.Database.CurrentTransaction is null ? await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
        try
        {
            var stockLock = "TKSC:goods-stock";
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={stockLock}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Goods stock is busy.', 1;
                """, ct);
            var idemLock = $"TKSC:goods-cart:{venueId ?? 0}:{actorId}:{idempotencyKey}";
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={idemLock}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
                IF @result < 0 THROW 51000, 'Goods cart is busy.', 1;
                """, ct);

            var prior = await context.AuditLogs.AsNoTracking()
                .Where(a => a.ActorId == actorId && a.Action == "goods_order_idempotency" && a.OldValue == idempotencyKey)
                .OrderByDescending(a => a.Id).Select(a => a.NewValue).FirstOrDefaultAsync(ct);
            if (prior is not null)
            {
                using var document = JsonDocument.Parse(prior);
                var previous = document.RootElement;
                if (!previous.TryGetProperty("fingerprint", out var savedFingerprint) || savedFingerprint.GetString() != fingerprint)
                    return await FinishAsync(transaction, OperationResult.Conflict(new { code = "IDEMPOTENCY_KEY_REUSED", message = "Khóa này đã được dùng cho một giỏ hàng khác." }), ct);
                var previousId = previous.GetProperty("orderId").GetInt64();
                var previousOrder = await context.Orders.AsNoTracking().Include(o => o.OrderItems).ThenInclude(i => i.Product).SingleOrDefaultAsync(o => o.Id == previousId, ct);
                if (previousOrder is null)
                    return await FinishAsync(transaction, OperationResult.Conflict(new { code = "IDEMPOTENCY_RESULT_MISSING", message = "Không thể khôi phục đơn của lần gửi trước. Cần đối soát." }), ct);
                var savedLines = previousOrder.OrderItems.OrderBy(i => i.Id).Select(i => new StoreCartItemResult(i.ProductId, i.Product.Name, i.Quantity, i.UnitPriceVnd, i.SubtotalVnd)).ToList();
                DateTimeOffset? previousExpiry = previous.TryGetProperty("expiresAt", out var savedExpiry) && savedExpiry.TryGetDateTimeOffset(out var parsedExpiry) ? parsedExpiry : null;
                return await FinishAsync(transaction, OperationResult.Success(new StoreCartOrderResult(previousOrder.Id, previousOrder.OrderCode, previousOrder.TotalAmount, previousOrder.Status, previousOrder.PaymentState, previousOrder.HandoverState, savedLines, previousExpiry)), ct);
            }

            foreach (var line in orderItems)
            {
                await using var command = new SqlCommand("""
                    SELECT dbo.OwnerEffectivePrice('product',p.id,p.unit_price_vnd,SYSDATETIMEOFFSET()),ISNULL(si.quantity_on_hand,0),CONVERT(bit,CASE WHEN p.is_active=1 AND (p.requires_price_approval=0 OR dbo.OwnerPriceVersion('product',p.id,SYSDATETIMEOFFSET()) IS NOT NULL) THEN 1 ELSE 0 END)
                    FROM dbo.products p WITH(UPDLOCK,HOLDLOCK)
                    LEFT JOIN dbo.stock_items si WITH(UPDLOCK,HOLDLOCK) ON si.product_id=p.id
                    WHERE p.id=@product AND (@scoped=0 OR p.venue_id=@venue);
                    """, (SqlConnection)context.Database.GetDbConnection(), (SqlTransaction)context.Database.CurrentTransaction!.GetDbTransaction());
                command.Parameters.Add("@product", System.Data.SqlDbType.BigInt).Value = line.ProductId;
                command.Parameters.Add("@scoped", System.Data.SqlDbType.Bit).Value = scoped;
                command.Parameters.Add("@venue", System.Data.SqlDbType.BigInt).Value = (object? )venueId ?? DBNull.Value;
                int currentPrice;
                int onHand;
                bool active;
                await using (var reader = await command.ExecuteReaderAsync(ct))
                {
                    if (!await reader.ReadAsync(ct))
                        return await FinishAsync(transaction, OperationResult.Missing(new { code = "product_not_found", message = "Sản phẩm không thuộc cơ sở đang chọn." }), ct);
                    currentPrice = reader.GetInt32(0);
                    onHand = reader.GetInt32(1);
                    active = reader.GetBoolean(2);
                }
                if (!active || currentPrice != line.UnitPriceVnd)
                    return await FinishAsync(transaction, OperationResult.Conflict(new { code = "price_changed", message = $"Giá hoặc trạng thái của {line.Name} đã thay đổi. Hãy tải lại giỏ hàng." }), ct);
            }

            context.Orders.Add(order);
            await context.SaveChangesAsync(ct);
            foreach (var item in orderItems)
            {
                var product = products.Single(p => p.Id == item.ProductId);
                context.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, PriceVersionId = product.QuotedPriceVersionId, Quantity = item.Quantity, UnitPriceVnd = item.UnitPriceVnd, SubtotalVnd = item.SubtotalVnd, Order = order, Product = product });
            }
            await context.SaveChangesAsync(ct);
            if (scoped)
                await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.orders SET venue_id={venueId} WHERE id={order.Id}", ct);

            await GoodsStockHolds.Reserve(context, order, expiresAt, false, ct);
            if (order.BuyerId is long buyerId)
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status)
                    VALUES({buyerId},'web',N'Đã giữ đơn hàng. Thanh toán trong 15 phút để giữ món.','goods_order_held',{order.Id},'unread')
                    """, ct);
            context.AuditLogs.Add(new AuditLog { ActorId=actorId, EntityType="GoodsOrder", EntityId=order.Id, Action="goods_hold", NewValue=JsonSerializer.Serialize(new { ExpiresAt=expiresAt }) });
            var reservation = JsonSerializer.Serialize(new { expiresAt, lines = orderItems.Select(line => new { productId = line.ProductId, quantity = line.Quantity }) });
            context.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = "goods_stock_reserved", EntityType = "GoodsOrder", EntityId = order.Id, NewValue = reservation });
            context.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = "goods_order_idempotency", EntityType = "GoodsOrder", EntityId = order.Id, OldValue = idempotencyKey, NewValue = JsonSerializer.Serialize(new { fingerprint, orderId = order.Id, expiresAt }) });
            await context.SaveChangesAsync(ct);
            if (transaction is not null) await transaction.CommitAsync(ct);
            return OperationResult.Created(new StoreCartOrderResult(order.Id, order.OrderCode, order.TotalAmount, order.Status, order.PaymentState, order.HandoverState, orderItems, expiresAt));
        }
        catch (SqlException ex) when (ex.Number is 1205 or 51000)
        {
            if (transaction is not null) await transaction.RollbackAsync(CancellationToken.None);
            return OperationResult.Conflict(new { code = "GOODS_ORDER_BUSY", message = "Đơn hàng đang được cập nhật. Hãy tải lại và thử lại." });
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }

    private static async Task<OperationResult> FinishAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction, OperationResult result, CancellationToken ct)
    {
        if (transaction is not null) await transaction.RollbackAsync(ct);
        return result;
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
