using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;

namespace TKSC.Infrastructure.Persistence.Repositories;

// All callers own a transaction. Lock inventory rows before reading/changing reservations.
internal static class GoodsStockHolds
{
    private static DbCommand Command(Sep490Context db, string sql, params (string, object?)[] args)
    {
        var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction() ?? throw new InvalidOperationException("Stock hold requires a transaction.");
        cmd.CommandText = sql;
        foreach (var (name, value) in args) { var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value ?? DBNull.Value; cmd.Parameters.Add(p); }
        return cmd;
    }
    public static async Task Ensure(Sep490Context db, CancellationToken ct)
    {
        await using var cmd = Command(db, "SELECT CASE WHEN OBJECT_ID('dbo.goods_stock_reservations','U') IS NOT NULL AND EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID('dbo.TR_stock_items_preserve_goods_holds') AND is_disabled=0) THEN 1 ELSE 0 END");
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 1)
            throw new PaymentException(503, "Chưa kích hoạt giữ tồn. Cần duyệt và áp dụng migration 018-goods-stock-reservations.sql.");
    }
    public static async Task<DateTimeOffset?> Expiry(Sep490Context db, long orderId, CancellationToken ct)
    {
        await Ensure(db, ct);
        await using var cmd = Command(db, "SELECT MIN(expires_at) FROM dbo.goods_stock_reservations WHERE order_id=@id AND state='Held'", ("@id",orderId));
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is DateTimeOffset date ? date : null;
    }
    public static async Task Reserve(Sep490Context db, Order order, DateTimeOffset expiry, bool reacquire, CancellationToken ct)
    {
        await Ensure(db, ct);
        foreach (var line in order.OrderItems.GroupBy(i => i.ProductId).OrderBy(g => g.Key))
        {
            var quantity = line.Sum(i => (long)i.Quantity);
            if (quantity <= 0 || quantity > int.MaxValue) throw new PaymentException(409,"Số lượng giữ tồn không hợp lệ.");
            await using var cmd = Command(db, """
                DECLARE @stock bigint,@onhand bigint,@held bigint,@existing int,@state varchar(20),@expiry datetimeoffset;
                SELECT @stock=si.id,@onhand=si.quantity_on_hand FROM dbo.stock_items si WITH (UPDLOCK,HOLDLOCK)
                  JOIN dbo.products p ON p.id=si.product_id JOIN dbo.orders o ON o.id=@order
                  WHERE si.product_id=@product AND p.is_active=1 AND p.venue_id=o.venue_id;
                IF @stock IS NULL THROW 51010,N'Sản phẩm không còn mở bán hoặc không thuộc kho của đơn.',1;
                SELECT @existing=quantity,@state=state,@expiry=expires_at FROM dbo.goods_stock_reservations WITH (UPDLOCK,HOLDLOCK)
                  WHERE order_id=@order AND stock_item_id=@stock;
                IF @state='Consumed' THROW 51010,N'Đơn đã xuất kho.',1;
                IF @existing IS NOT NULL AND @existing<>@quantity THROW 51010,N'Dòng hàng khác với số lượng đã giữ.',1;
                IF @state='Committed' OR (@state='Held' AND @expiry>SYSUTCDATETIME()) RETURN;
                IF @existing IS NOT NULL AND @reacquire=0 THROW 51010,N'Giữ tồn đã hết hạn hoặc bị hủy. Tạo đơn mới hoặc đối soát.',1;
                SELECT @held=COALESCE(SUM(CONVERT(bigint,quantity)),0) FROM dbo.goods_stock_reservations WITH (UPDLOCK,HOLDLOCK)
                  WHERE stock_item_id=@stock AND order_id<>@order AND (state='Committed' OR (state='Held' AND expires_at>SYSUTCDATETIME()));
                IF @onhand-@held<@quantity THROW 51010,N'Không đủ hàng khả dụng. Có đơn khác đang giữ hàng.',1;
                IF @existing IS NULL INSERT dbo.goods_stock_reservations(order_id,stock_item_id,quantity,expires_at,state)
                  VALUES(@order,@stock,@quantity,@expires,'Held');
                ELSE UPDATE dbo.goods_stock_reservations SET state='Held',expires_at=@expires,updated_at=SYSUTCDATETIME()
                  WHERE order_id=@order AND stock_item_id=@stock;
                """, ("@order",order.Id),("@product",line.Key),("@quantity",quantity),("@expires",expiry),("@reacquire",reacquire));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
    public static async Task<bool> Commit(Sep490Context db, Order order, CancellationToken ct)
    {
        await Ensure(db, ct);
        // A payment arriving after release may never reacquire automatically.
        foreach (var line in order.OrderItems.GroupBy(i=>i.ProductId).OrderBy(g=>g.Key))
        {
            await using var cmd = Command(db,"""
                SELECT COUNT(*) FROM dbo.stock_items si WITH (UPDLOCK,HOLDLOCK)
                JOIN dbo.goods_stock_reservations r WITH (UPDLOCK,HOLDLOCK) ON r.stock_item_id=si.id
                WHERE r.order_id=@order AND si.product_id=@product AND r.quantity=@quantity
                  AND (r.state='Committed' OR (r.state='Held' AND r.expires_at>SYSUTCDATETIME()));
                """,("@order",order.Id),("@product",line.Key),("@quantity",line.Sum(i=>(long)i.Quantity)));
            if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) != 1) return false;
        }
        await using var update = Command(db,"UPDATE dbo.goods_stock_reservations SET state='Committed',updated_at=SYSUTCDATETIME() WHERE order_id=@id AND state='Held'",("@id",order.Id));
        await update.ExecuteNonQueryAsync(ct);
        return order.OrderItems.Count > 0;
    }
    public static async Task Release(Sep490Context db, long id, CancellationToken ct)
    {
        await Ensure(db, ct);
        await using var cmd = Command(db,"UPDATE dbo.goods_stock_reservations SET state='Released',updated_at=SYSUTCDATETIME() WHERE order_id=@id AND state='Held'",("@id",id));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public static async Task Consume(Sep490Context db, Order order, long actor, CancellationToken ct)
    {
        await Ensure(db, ct);
        foreach (var line in order.OrderItems.GroupBy(i=>i.ProductId).OrderBy(g=>g.Key))
        {
            await using var cmd = Command(db,"""
                DECLARE @stock bigint,@state varchar(20),@quantity int;
                SELECT @stock=id FROM dbo.stock_items WITH (UPDLOCK,HOLDLOCK) WHERE product_id=@product;
                SELECT @state=state,@quantity=quantity FROM dbo.goods_stock_reservations WITH (UPDLOCK,HOLDLOCK)
                  WHERE order_id=@order AND stock_item_id=@stock;
                IF @state='Consumed' RETURN;
                IF @state IS NULL OR @state<>'Committed' OR @quantity<>@expected THROW 51010,N'Thiếu giữ tồn đã thanh toán. Cần đối soát trước khi giao.',1;
                UPDATE dbo.goods_stock_reservations SET state='Consumed',updated_at=SYSUTCDATETIME() WHERE order_id=@order AND stock_item_id=@stock;
                UPDATE dbo.stock_items SET quantity_on_hand=quantity_on_hand-@quantity WHERE id=@stock AND quantity_on_hand>=@quantity;
                IF @@ROWCOUNT<>1 THROW 51010,N'Không đủ tồn kho để bàn giao.',1;
                INSERT dbo.stock_movements(stock_item_id,movement_type,quantity,reference_type,reference_id,created_by)
                  VALUES(@stock,'SaleOut',-@quantity,'GoodsOrder',@order,@actor);
                """,("@order",order.Id),("@product",line.Key),("@expected",line.Sum(i=>(long)i.Quantity)),("@actor",actor));
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
    public static async Task Sweep(Sep490Context db, CancellationToken ct)
    {
        await Ensure(db, ct);
        await using var cmd = Command(db,"""
            UPDATE r SET state='Released',updated_at=SYSUTCDATETIME()
            FROM dbo.goods_stock_reservations r JOIN dbo.orders o ON o.id=r.order_id
            WHERE r.state='Held' AND (r.expires_at<=SYSUTCDATETIME() OR o.status='Cancelled');
            """);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
