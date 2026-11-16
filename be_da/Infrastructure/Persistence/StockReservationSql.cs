using System.Data;
using System.Data.Common;

namespace TKSC.Infrastructure.Persistence;
// Stock holds of goods orders (upgrade 017). Every statement checks the stock row in the same UPDATE,
// so two buyers can never reserve more than is available, and nothing goes below zero.
// The caller supplies the open transaction; order state columns are written by the caller's EF context.
public static class StockReservationSql
{
    public const string LockName = "TKSC:stock-reserve";
    // Reservations run one at a time (a short, transaction-owned lock): concurrent carts for the same last items
    // queue instead of deadlocking. The lock is re-entrant inside one transaction.
    public const string LockSql = """
        DECLARE @reserveLock int;
        EXEC @reserveLock = sys.sp_getapplock @Resource = N'TKSC:stock-reserve', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @reserveLock < 0 THROW 51000, 'Stock reservation is busy.', 1;
        """;
    private const string NeedSql = LockSql + """
        DECLARE @need TABLE (product_id bigint PRIMARY KEY, qty int NOT NULL);
        INSERT @need (product_id, qty)
        SELECT product_id, SUM(CAST(quantity AS int)) FROM dbo.order_items WHERE order_id = @orderId GROUP BY product_id;
        """;

    // Holds every line of the order or none. Returns the first product that is short, or null when all are held.
    public static async Task<long?> ReserveAsync(DbConnection connection, DbTransaction? transaction, long orderId, CancellationToken ct)
    {
        const string sql = NeedSql + """
            DECLARE @short bigint = (
                SELECT TOP (1) n.product_id FROM @need n
                LEFT JOIN dbo.stock_items si WITH (UPDLOCK, HOLDLOCK) ON si.product_id = n.product_id
                WHERE si.id IS NULL OR si.quantity_on_hand - si.reserved_quantity < n.qty
                ORDER BY n.product_id);
            IF @short IS NOT NULL BEGIN SELECT @short; RETURN; END;
            UPDATE si SET reserved_quantity = si.reserved_quantity + n.qty
            FROM dbo.stock_items si JOIN @need n ON n.product_id = si.product_id
            WHERE si.quantity_on_hand - si.reserved_quantity >= n.qty;
            IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @need) THROW 51001, 'Stock changed while reserving.', 1;
            SELECT CAST(0 AS bigint);
            """;
        var result = Convert.ToInt64(await ScalarAsync(connection, transaction, sql, orderId, null, ct));
        return result == 0 ? null : result;
    }

    // Gives the order's held quantities back to available stock. Called once, when the order leaves the Held state.
    public static Task ReleaseAsync(DbConnection connection, DbTransaction? transaction, long orderId, CancellationToken ct) => ScalarAsync(connection, transaction, NeedSql + """
        UPDATE si SET reserved_quantity = si.reserved_quantity - n.qty
        FROM dbo.stock_items si JOIN @need n ON n.product_id = si.product_id
        WHERE si.reserved_quantity >= n.qty;
        IF @@ROWCOUNT <> (SELECT COUNT(*) FROM @need) THROW 51002, 'Reserved stock does not match the order.', 1;
        SELECT CAST(0 AS bigint);
        """, orderId, null, ct);

    // Posts the sale once: one SaleOut per product, on hand down, and the hold (if any) consumed.
    // Returns 0 when posted now, -1 when it was already posted, or the id of a product without enough stock.
    public static async Task<long> ConsumeAsync(DbConnection connection, DbTransaction? transaction, long orderId, long actorId, bool held, CancellationToken ct)
    {
        const string sql = NeedSql + """
            IF EXISTS (SELECT 1 FROM dbo.stock_movements WHERE reference_type = 'GoodsOrder' AND reference_id = @orderId AND movement_type = 'SaleOut')
                BEGIN SELECT CAST(-1 AS bigint); RETURN; END;
            DECLARE @short bigint = (
                SELECT TOP (1) n.product_id FROM @need n
                LEFT JOIN dbo.stock_items si WITH (UPDLOCK, HOLDLOCK) ON si.product_id = n.product_id
                WHERE si.id IS NULL OR si.quantity_on_hand < n.qty
                   OR (@held = 1 AND si.reserved_quantity < n.qty)
                   OR (@held = 0 AND si.quantity_on_hand - si.reserved_quantity < n.qty)
                ORDER BY n.product_id);
            IF @short IS NOT NULL BEGIN SELECT @short; RETURN; END;
            DECLARE @posted TABLE (stock_item_id bigint, qty int);
            UPDATE si SET quantity_on_hand = si.quantity_on_hand - n.qty,
                          reserved_quantity = si.reserved_quantity - CASE WHEN @held = 1 THEN n.qty ELSE 0 END
            OUTPUT inserted.id, n.qty INTO @posted
            FROM dbo.stock_items si JOIN @need n ON n.product_id = si.product_id;
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
            SELECT stock_item_id, 'SaleOut', -qty, 'GoodsOrder', @orderId, @actorId FROM @posted;
            SELECT CAST(0 AS bigint);
            """;
        return Convert.ToInt64(await ScalarAsync(connection, transaction, sql, orderId, command =>
        {
            Add(command, "@actorId", DbType.Int64, actorId);
            Add(command, "@held", DbType.Boolean, held);
        }, ct));
    }

    private static async Task<object?> ScalarAsync(DbConnection connection, DbTransaction? transaction, string sql, long orderId, Action<DbCommand>? configure, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        Add(command, "@orderId", DbType.Int64, orderId);
        configure?.Invoke(command);
        return await command.ExecuteScalarAsync(ct);
    }

    private static void Add(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
