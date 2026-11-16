namespace be_da.Infrastructure.Persistence;
// Quantity of a stock item held for goods orders (upgrade 018, dbo.goods_stock_reservations): paid holds (Committed)
// and unexpired unpaid holds. Same rule as TR_stock_items_preserve_goods_holds; available to sell or issue = on hand - held.
public static class StockHoldSql
{
    public static string HeldFor(string stockItemId) => $"""
        (SELECT ISNULL(SUM(CONVERT(int, r.quantity)), 0) FROM dbo.goods_stock_reservations r
         WHERE r.stock_item_id = {stockItemId} AND (r.state = 'Committed' OR (r.state = 'Held' AND r.expires_at > SYSUTCDATETIME())))
        """;
}
