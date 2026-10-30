using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Stock;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlStockStore(string connectionString) : IStockStore
{
    private const string ProductSelect = """
        SELECT p.id, p.sku, p.name, p.category, p.unit_price_vnd, ISNULL(p.is_active, 0) AS is_active,
               ISNULL(si.quantity_on_hand, 0) AS quantity_on_hand, ISNULL(si.reorder_level, 0) AS reorder_level
        FROM dbo.products p
        LEFT JOIN dbo.stock_items si ON si.product_id = p.id
        """;

    // Manual staff entries have no source document table yet; the operator is kept in created_by.
    private const string StaffEntry = "StaffEntry";

    public async Task<IReadOnlyList<StockProduct>> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var sql = ProductSelect + """

            WHERE (@search IS NULL OR p.sku LIKE @search OR p.name LIKE @search)
              AND (@category IS NULL OR p.category = @category)
              AND (@includeInactive = 1 OR p.is_active = 1)
            ORDER BY ISNULL(p.is_active, 0) DESC, p.name ASC;
            """;
        await using var command = new SqlCommand(sql, connection);
        var search = filter.Search?.Trim();
        command.Parameters.Add("@search", SqlDbType.NVarChar, 260).Value = string.IsNullOrEmpty(search)
            ? DBNull.Value : "%" + EscapeLike(search) + "%";
        command.Parameters.Add("@category", SqlDbType.VarChar, 100).Value = (object?)filter.Category ?? DBNull.Value;
        command.Parameters.Add("@includeInactive", SqlDbType.Bit).Value = filter.IncludeInactive;

        var products = new List<StockProduct>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) products.Add(ReadProduct(reader));
        return products;
    }

    public async Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return await LoadProductAsync(connection, null, productId, cancellationToken);
    }

    public async Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            const string sql = """
                INSERT dbo.products (sku, name, category, unit_price_vnd, is_active)
                VALUES (@sku, @name, @category, @price, 1);
                DECLARE @productId bigint = SCOPE_IDENTITY();
                INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level)
                VALUES (@productId, @quantity, @reorder);
                DECLARE @stockItemId bigint = SCOPE_IDENTITY();
                IF @quantity > 0
                    INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
                    VALUES (@stockItemId, @movementType, @quantity, 'Product', @productId, @staffUserId);
                SELECT @productId;
                """;
            await using var command = new SqlCommand(sql, connection, transaction);
            command.Parameters.Add("@sku", SqlDbType.VarChar, 100).Value = StockRules.NormalizeSku(request.Sku);
            command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = request.Name!.Trim();
            command.Parameters.Add("@category", SqlDbType.VarChar, 100).Value = (object?)request.Category ?? DBNull.Value;
            command.Parameters.Add("@price", SqlDbType.Int).Value = request.UnitPriceVnd;
            command.Parameters.Add("@quantity", SqlDbType.Int).Value = request.InitialQuantity;
            command.Parameters.Add("@reorder", SqlDbType.Int).Value = request.ReorderLevel;
            command.Parameters.Add("@movementType", SqlDbType.VarChar, 100).Value = StockRules.OpeningBalance;
            command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
            var productId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            var product = await LoadProductAsync(connection, transaction, productId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return StockResult.Ok(product!);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return StockResult.Fail("SKU_EXISTS", "Mã hàng (SKU) đã tồn tại.");
        }
    }

    public async Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.products
            SET name = @name, category = @category, unit_price_vnd = @price, is_active = @active
            WHERE id = @productId;
            IF @@ROWCOUNT = 0 BEGIN SELECT CAST(0 AS bit); RETURN; END;
            IF EXISTS (SELECT 1 FROM dbo.stock_items WITH (UPDLOCK, HOLDLOCK) WHERE product_id = @productId)
                UPDATE dbo.stock_items SET reorder_level = @reorder WHERE product_id = @productId;
            ELSE
                INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level) VALUES (@productId, 0, @reorder);
            SELECT CAST(1 AS bit);
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@name", SqlDbType.NVarChar, 255).Value = request.Name!.Trim();
        command.Parameters.Add("@category", SqlDbType.VarChar, 100).Value = (object?)request.Category ?? DBNull.Value;
        command.Parameters.Add("@price", SqlDbType.Int).Value = request.UnitPriceVnd;
        command.Parameters.Add("@active", SqlDbType.Bit).Value = request.IsActive;
        command.Parameters.Add("@reorder", SqlDbType.Int).Value = request.ReorderLevel;
        if (!(bool)(await command.ExecuteScalarAsync(cancellationToken))!)
        {
            await transaction.RollbackAsync(cancellationToken);
            return StockResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy mặt hàng.");
        }
        var product = await LoadProductAsync(connection, transaction, productId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StockResult.Ok(product!);
    }

    public async Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 posted, 1 product missing, 2 inactive, 3 insufficient stock.
        const string sql = """
            DECLARE @active bit;
            SELECT @active = ISNULL(is_active, 0) FROM dbo.products WITH (UPDLOCK, HOLDLOCK) WHERE id = @productId;
            IF @@ROWCOUNT = 0 BEGIN SELECT 1; RETURN; END;
            IF @active = 0 BEGIN SELECT 2; RETURN; END;
            IF NOT EXISTS (SELECT 1 FROM dbo.stock_items WITH (UPDLOCK, HOLDLOCK) WHERE product_id = @productId)
                INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level) VALUES (@productId, 0, 0);
            DECLARE @posted TABLE (id bigint);
            UPDATE dbo.stock_items
            SET quantity_on_hand = quantity_on_hand + @delta
            OUTPUT inserted.id INTO @posted
            WHERE product_id = @productId AND quantity_on_hand + @delta >= 0;
            IF NOT EXISTS (SELECT 1 FROM @posted) BEGIN SELECT 3; RETURN; END;
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
            SELECT id, @movementType, @delta, @referenceType, NULL, @staffUserId FROM @posted;
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@delta", SqlDbType.Int).Value = request.Type == StockRules.Issue ? -request.Quantity : request.Quantity;
        command.Parameters.Add("@movementType", SqlDbType.VarChar, 100).Value = request.Type;
        command.Parameters.Add("@referenceType", SqlDbType.VarChar, 100).Value = StaffEntry;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => StockResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy mặt hàng."),
                2 => StockResult.Fail("PRODUCT_INACTIVE", "Mặt hàng đã ngừng kinh doanh, không thể nhập/xuất."),
                _ => StockResult.Fail("INSUFFICIENT_STOCK", "Số lượng xuất vượt quá tồn kho hiện có.")
            };
        }
        var product = await LoadProductAsync(connection, transaction, productId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StockResult.Ok(product!);
    }

    public async Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using (var exists = new SqlCommand("SELECT COUNT(*) FROM dbo.products WHERE id = @productId;", connection))
        {
            exists.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            if (Convert.ToInt32(await exists.ExecuteScalarAsync(cancellationToken)) == 0) return null;
        }

        // stock_movements has no timestamp column; id order is the posting order.
        const string sql = """
            SELECT TOP (200) m.id, ISNULL(m.movement_type, '') AS movement_type, m.quantity,
                   m.reference_type, m.reference_id, m.created_by, u.full_name
            FROM dbo.stock_movements m
            JOIN dbo.stock_items si ON si.id = m.stock_item_id
            LEFT JOIN dbo.users u ON u.id = m.created_by
            WHERE si.product_id = @productId
            ORDER BY m.id DESC;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        var movements = new List<StockMovementEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            movements.Add(new StockMovementEntry(
                reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt64(4),
                reader.IsDBNull(5) ? null : reader.GetInt64(5),
                reader.IsDBNull(6) ? null : reader.GetString(6)));
        }
        return movements;
    }

    private static async Task<StockProduct?> LoadProductAsync(SqlConnection connection, SqlTransaction? transaction, long productId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(ProductSelect + "\nWHERE p.id = @productId;", connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
    }

    private static StockProduct ReadProduct(SqlDataReader reader) => new(
        reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetInt32(4), reader.GetBoolean(5), reader.GetInt32(6), reader.GetInt32(7));

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
