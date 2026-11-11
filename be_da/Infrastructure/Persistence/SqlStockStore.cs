using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Stock;

namespace be_da.Infrastructure.Persistence;

public sealed class SqlStockStore(string connectionString) : IStockStore
{
    private const string ProductSelect = """
        SELECT p.id, p.sku, p.name, p.category, p.unit_price_vnd, ISNULL(p.is_active, 0) AS is_active,
<<<<<<< HEAD
               ISNULL(si.quantity_on_hand, 0) AS quantity_on_hand, ISNULL(si.reorder_level, 0) AS reorder_level
=======
               ISNULL(si.quantity_on_hand, 0) AS quantity_on_hand, ISNULL(si.reorder_level, 0) AS reorder_level,
               CAST(CASE WHEN ISNULL(si.quantity_on_hand, 0) > 0 OR EXISTS (
                   SELECT 1 FROM dbo.stock_movements m WHERE m.stock_item_id = si.id) THEN 1 ELSE 0 END AS bit) AS units_locked
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        FROM dbo.products p
        LEFT JOIN dbo.stock_items si ON si.product_id = p.id
        """;

    // Manual staff entries have no source document table yet; the operator is kept in created_by.
    private const string StaffEntry = "StaffEntry";

    public async Task<IReadOnlyList<StockProduct>> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) products.Add(ReadProduct(reader));
        return products;
=======
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken)) products.Add(ReadProduct(reader));
        }
        var units = await LoadUnitsAsync(connection, null, null, cancellationToken);
        return products.Select(p => WithUnits(p, units)).ToList();
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    }

    public async Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        return await LoadProductAsync(connection, null, productId, cancellationToken);
    }

    public async Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
=======
            await ReplaceUnitsAsync(connection, transaction, productId,
                request.Units ?? [new ProductUnitInput(StockUnitRules.DefaultBaseUnit, 1)], cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            var product = await LoadProductAsync(connection, transaction, productId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return StockResult.Ok(product!);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
<<<<<<< HEAD
            return StockResult.Fail("SKU_EXISTS", "Mã hàng (SKU) đã tồn tại.");
=======
            return ex.Message.Contains("UX_product_units_barcode")
                ? StockResult.Fail("BARCODE_EXISTS", "Mã vạch đã được dùng cho mặt hàng khác.")
                : StockResult.Fail("SKU_EXISTS", "Mã hàng (SKU) đã tồn tại.");
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        }
    }

    public async Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        if (request.Units is not null)
        {
            var current = await LoadProductAsync(connection, transaction, productId, cancellationToken);
            if (current is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StockResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy mặt hàng.");
            }
            if (StockUnitRules.ValidateUnitChange(current.Units ?? [], request.Units, current.UnitsLocked) is { } lockError)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StockResult.Fail("UNITS_LOCKED", lockError);
            }
        }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
=======
        try
        {
            // Nothing references product_units.id: movements keep the unit name they were entered in.
            if (request.Units is not null) await ReplaceUnitsAsync(connection, transaction, productId, request.Units, cancellationToken);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return StockResult.Fail("BARCODE_EXISTS", "Mã vạch đã được dùng cho mặt hàng khác.");
        }
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        var product = await LoadProductAsync(connection, transaction, productId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StockResult.Ok(product!);
    }

    public async Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 posted, 1 product missing, 2 inactive, 3 insufficient stock.
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 posted, 1 product missing, 2 inactive, 3 insufficient stock, 4 unknown unit, 5 too large to post.
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        const string sql = """
            DECLARE @active bit;
            SELECT @active = ISNULL(is_active, 0) FROM dbo.products WITH (UPDLOCK, HOLDLOCK) WHERE id = @productId;
            IF @@ROWCOUNT = 0 BEGIN SELECT 1; RETURN; END;
            IF @active = 0 BEGIN SELECT 2; RETURN; END;
<<<<<<< HEAD
=======
            IF NOT EXISTS (SELECT 1 FROM dbo.product_units WHERE product_id = @productId AND factor_to_base = 1)
                INSERT dbo.product_units (product_id, unit_name, factor_to_base) VALUES (@productId, N'Cái', 1);
            DECLARE @factor int, @unit nvarchar(50);
            SELECT @factor = factor_to_base, @unit = unit_name FROM dbo.product_units
            WHERE product_id = @productId AND ((@unitName IS NULL AND factor_to_base = 1) OR unit_name = @unitName);
            IF @factor IS NULL BEGIN SELECT 4; RETURN; END;
            DECLARE @baseQuantity bigint = CAST(@quantity AS bigint) * @factor;
            IF @baseQuantity > @maxBase BEGIN SELECT 5; RETURN; END;
            DECLARE @delta int = @sign * CAST(@baseQuantity AS int);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            IF NOT EXISTS (SELECT 1 FROM dbo.stock_items WITH (UPDLOCK, HOLDLOCK) WHERE product_id = @productId)
                INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level) VALUES (@productId, 0, 0);
            DECLARE @posted TABLE (id bigint);
            UPDATE dbo.stock_items
            SET quantity_on_hand = quantity_on_hand + @delta
            OUTPUT inserted.id INTO @posted
            WHERE product_id = @productId AND quantity_on_hand + @delta >= 0;
            IF NOT EXISTS (SELECT 1 FROM @posted) BEGIN SELECT 3; RETURN; END;
<<<<<<< HEAD
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
            SELECT id, @movementType, @delta, @referenceType, NULL, @staffUserId FROM @posted;
=======
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by, entered_unit, entered_quantity)
            SELECT id, @movementType, @delta, @referenceType, NULL, @staffUserId, @unit, @quantity FROM @posted;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
<<<<<<< HEAD
        command.Parameters.Add("@delta", SqlDbType.Int).Value = request.Type == StockRules.Issue ? -request.Quantity : request.Quantity;
=======
        var unitName = StockUnitRules.NormalizeUnitName(request.UnitName);
        command.Parameters.Add("@unitName", SqlDbType.NVarChar, 50).Value = unitName.Length == 0 ? DBNull.Value : unitName;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = request.Quantity;
        command.Parameters.Add("@sign", SqlDbType.Int).Value = request.Type == StockRules.Issue ? -1 : 1;
        command.Parameters.Add("@maxBase", SqlDbType.BigInt).Value = StockUnitRules.MaxBaseQuantity;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
=======
                4 => StockResult.Fail("UNIT_NOT_FOUND", "Mặt hàng không có đơn vị này."),
                5 => StockResult.Fail("QUANTITY_TOO_LARGE", "Số lượng quy đổi vượt giới hạn cho phép."),
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
        await connection.OpenAsync(cancellationToken);
=======
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        await using (var exists = new SqlCommand("SELECT COUNT(*) FROM dbo.products WHERE id = @productId;", connection))
        {
            exists.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            if (Convert.ToInt32(await exists.ExecuteScalarAsync(cancellationToken)) == 0) return null;
        }

<<<<<<< HEAD
        // stock_movements has no timestamp column; id order is the posting order.
        const string sql = """
            SELECT TOP (200) m.id, ISNULL(m.movement_type, '') AS movement_type, m.quantity,
                   m.reference_type, m.reference_id, m.created_by, u.full_name
=======
        // Movements older than upgrade 010 have no timestamp; id order is the posting order.
        const string sql = """
            SELECT TOP (200) m.id, ISNULL(m.movement_type, '') AS movement_type, m.quantity,
                   m.reference_type, m.reference_id, m.created_by, u.full_name,
                   m.created_at, m.entered_unit, m.entered_quantity, m.source_ref
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
<<<<<<< HEAD
                reader.IsDBNull(6) ? null : reader.GetString(6)));
=======
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetDateTimeOffset(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        }
        return movements;
    }

<<<<<<< HEAD
=======
    public async Task<StockCodeLookup?> LookupCodeAsync(string code, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT TOP (1) p.id, p.sku, p.name, ISNULL(p.is_active, 0), p.unit_price_vnd, ISNULL(si.quantity_on_hand, 0),
                   ISNULL(b.unit_name, u.unit_name), u.unit_name, u.factor_to_base, u.barcode
            FROM dbo.product_units u
            JOIN dbo.products p ON p.id = u.product_id
            LEFT JOIN dbo.stock_items si ON si.product_id = p.id
            LEFT JOIN dbo.product_units b ON b.product_id = p.id AND b.factor_to_base = 1
            WHERE u.barcode = @code OR (p.sku = @code AND u.factor_to_base = 1)
            ORDER BY CASE WHEN u.barcode = @code THEN 0 ELSE 1 END;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@code", SqlDbType.VarChar, 100).Value = code;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new StockCodeLookup(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
            reader.GetInt32(4), reader.GetInt32(5), reader.GetString(6), reader.GetString(7), reader.GetInt32(8),
            reader.IsDBNull(9) ? null : reader.GetString(9));
    }

    public async Task<BatchReceiptResult> PostBatchReceiptAsync(BatchReceiptRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var sourceRef = StockReceiptRules.NormalizeSourceRef(request.SourceRef)!;
        var catalog = await LoadCatalogAsync(connection, cancellationToken);

        var resolved = new List<ResolvedReceiptLine>();
        var errors = new List<BatchLineError>();
        for (var i = 0; i < request.Lines!.Count; i++)
        {
            var (line, error) = StockReceiptRules.ResolveLine(i + 1, request.Lines[i], catalog);
            if (line is not null) resolved.Add(line);
            if (error is not null) errors.Add(error);
        }
        if (errors.Count > 0)
            return BatchReceiptResult.Fail("INVALID_LINES", $"Có {errors.Count} dòng không hợp lệ, chưa nhập dòng nào.", errors);
        var merged = StockReceiptRules.MergeLines(resolved);
        if (merged.Any(m => m.BaseQuantity > StockUnitRules.MaxBaseQuantity))
            return BatchReceiptResult.Fail("QUANTITY_TOO_LARGE", "Tổng số lượng quy đổi của một mặt hàng vượt giới hạn cho phép.");

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var lockCommand = new SqlCommand("""
                DECLARE @lock int;
                EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                IF @lock < 0 THROW 51000, 'Stock receipt is busy.', 1;
                """, connection, transaction))
            {
                lockCommand.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = "TKSC:stock-receipt:" + sourceRef;
                await lockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var posted = new Dictionary<long, int>();
            await using (var existing = new SqlCommand("""
                SELECT si.product_id, SUM(m.quantity)
                FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
                WHERE m.source_ref = @sourceRef
                GROUP BY si.product_id;
                """, connection, transaction))
            {
                existing.Parameters.Add("@sourceRef", SqlDbType.VarChar, 100).Value = sourceRef;
                await using var reader = await existing.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken)) posted[reader.GetInt64(0)] = reader.GetInt32(1);
            }
            if (posted.Count > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StockReceiptRules.MatchesPosted(merged, posted)
                    ? BatchReceiptResult.Ok(merged, alreadyPosted: true)
                    : BatchReceiptResult.Fail("SOURCE_REF_USED", $"Mã phiếu {sourceRef} đã dùng cho một phiếu nhập khác.");
            }

            foreach (var line in merged)
            {
                await using var command = new SqlCommand("""
                    IF NOT EXISTS (SELECT 1 FROM dbo.stock_items WITH (UPDLOCK, HOLDLOCK) WHERE product_id = @productId)
                        INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level) VALUES (@productId, 0, 0);
                    DECLARE @posted TABLE (id bigint);
                    UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand + @quantity
                    OUTPUT inserted.id INTO @posted
                    WHERE product_id = @productId;
                    INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by, entered_unit, entered_quantity, source_ref)
                    SELECT id, @movementType, @quantity, 'Receipt', NULL, @staffUserId, @enteredUnit, @enteredQuantity, @sourceRef FROM @posted;
                    """, connection, transaction);
                command.Parameters.Add("@productId", SqlDbType.BigInt).Value = line.ProductId;
                command.Parameters.Add("@quantity", SqlDbType.Int).Value = line.BaseQuantity;
                command.Parameters.Add("@movementType", SqlDbType.VarChar, 100).Value = StockRules.Receipt;
                command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
                command.Parameters.Add("@enteredUnit", SqlDbType.NVarChar, 50).Value = (object?)line.EnteredUnit ?? DBNull.Value;
                command.Parameters.Add("@enteredQuantity", SqlDbType.Int).Value = (object?)line.EnteredQuantity ?? DBNull.Value;
                command.Parameters.Add("@sourceRef", SqlDbType.VarChar, 100).Value = sourceRef;
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
            return BatchReceiptResult.Ok(merged);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return BatchReceiptResult.Fail("SOURCE_REF_USED", $"Mã phiếu {sourceRef} đã dùng cho một phiếu nhập khác.");
        }
    }

    private static async Task<IReadOnlyList<ReceiptCatalogItem>> LoadCatalogAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var units = await LoadUnitsAsync(connection, null, null, cancellationToken);
        var catalog = new List<ReceiptCatalogItem>();
        await using var command = new SqlCommand("SELECT id, sku, name, ISNULL(is_active, 0) FROM dbo.products;", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            catalog.Add(new ReceiptCatalogItem(id, reader.GetString(1), reader.GetString(2), reader.GetBoolean(3),
                units.TryGetValue(id, out var list) ? list : []));
        }
        return catalog;
    }

    private static async Task<Dictionary<long, List<ProductUnit>>> LoadUnitsAsync(SqlConnection connection, SqlTransaction? transaction, long? productId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT id, product_id, unit_name, factor_to_base, barcode
            FROM dbo.product_units
            WHERE @productId IS NULL OR product_id = @productId
            ORDER BY product_id, factor_to_base;
            """, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = (object?)productId ?? DBNull.Value;
        var units = new Dictionary<long, List<ProductUnit>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var owner = reader.GetInt64(1);
            if (!units.TryGetValue(owner, out var list)) units[owner] = list = [];
            list.Add(new ProductUnit(reader.GetInt64(0), reader.GetString(2), reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        return units;
    }

    private static async Task ReplaceUnitsAsync(SqlConnection connection, SqlTransaction transaction, long productId, IReadOnlyList<ProductUnitInput> units, CancellationToken cancellationToken)
    {
        await using (var delete = new SqlCommand("DELETE dbo.product_units WHERE product_id = @productId;", connection, transaction))
        {
            delete.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach (var unit in units)
        {
            await using var insert = new SqlCommand(
                "INSERT dbo.product_units (product_id, unit_name, factor_to_base, barcode) VALUES (@productId, @name, @factor, @barcode);",
                connection, transaction);
            insert.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            insert.Parameters.Add("@name", SqlDbType.NVarChar, StockUnitRules.MaxUnitNameLength).Value = StockUnitRules.NormalizeUnitName(unit.UnitName);
            insert.Parameters.Add("@factor", SqlDbType.Int).Value = unit.FactorToBase;
            insert.Parameters.Add("@barcode", SqlDbType.VarChar, 64).Value = (object?)StockUnitRules.NormalizeBarcode(unit.Barcode) ?? DBNull.Value;
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static StockProduct WithUnits(StockProduct product, IReadOnlyDictionary<long, List<ProductUnit>> units)
    {
        var list = units.TryGetValue(product.Id, out var found) ? found : [];
        return product with { Units = list, BaseUnit = list.FirstOrDefault(u => u.IsBase)?.UnitName ?? StockUnitRules.DefaultBaseUnit };
    }

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    private static async Task<StockProduct?> LoadProductAsync(SqlConnection connection, SqlTransaction? transaction, long productId, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(ProductSelect + "\nWHERE p.id = @productId;", connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
<<<<<<< HEAD
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
=======
        StockProduct? product;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            product = await reader.ReadAsync(cancellationToken) ? ReadProduct(reader) : null;
        }
        return product is null ? null : WithUnits(product, await LoadUnitsAsync(connection, transaction, productId, cancellationToken));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    }

    private static StockProduct ReadProduct(SqlDataReader reader) => new(
        reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
<<<<<<< HEAD
        reader.GetInt32(4), reader.GetBoolean(5), reader.GetInt32(6), reader.GetInt32(7));
=======
        reader.GetInt32(4), reader.GetBoolean(5), reader.GetInt32(6), reader.GetInt32(7), UnitsLocked: reader.GetBoolean(8));
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
