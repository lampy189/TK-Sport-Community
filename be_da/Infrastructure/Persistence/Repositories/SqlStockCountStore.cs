using be_da.Application.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Validators;

namespace be_da.Infrastructure.Persistence.Repositories;
// RP3 UC-43 / UC-44 plus stock figures for reports. Stock only changes through an approved adjustment movement,
// posted once (UX_stock_movements_stock_count). Every query filters by the caller's center.
public sealed class SqlStockCountStore(string connectionString, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : IStockCountStore
{
    private const string CountSelect = """
        SELECT sc.id, si.product_id, p.sku, p.name, ISNULL(sc.system_quantity, 0), sc.counted_quantity, ISNULL(sc.difference, 0),
               sc.entered_unit, sc.entered_quantity, sc.status, sc.reason, sc.evidence, sc.counted_by, ISNULL(cu.full_name, N''),
               sc.counted_at, sc.approved_by, du.full_name, sc.decided_at, sc.decision_note, sc.applied_movement_id,
               ISNULL(bu.unit_name, N'Cái'), si.quantity_on_hand, sc.stock_version
        FROM dbo.stock_counts sc
        JOIN dbo.stock_items si ON si.id = sc.stock_item_id
        JOIN dbo.products p ON p.id = si.product_id
        LEFT JOIN dbo.users cu ON cu.id = sc.counted_by
        LEFT JOIN dbo.users du ON du.id = sc.approved_by
        LEFT JOIN dbo.product_units bu ON bu.product_id = p.id AND bu.factor_to_base = 1
        WHERE (@venue IS NULL OR p.venue_id = @venue)
        """;
    private long? Venue => dataScope?.VenueId;

    public async Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await LoadSheetAsync(connection, null, productId, Venue, cancellationToken);
    }

    public async Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 saved, 1 product/stock missing, 2 stock changed since the sheet was loaded, 3 reason required,
        // 9 unknown unit, 10 too large.
        const string sql = """
            DECLARE @si bigint, @onHand int;
            SELECT @si = si.id, @onHand = si.quantity_on_hand
            FROM dbo.products p JOIN dbo.stock_items si WITH (UPDLOCK, HOLDLOCK) ON si.product_id = p.id
            WHERE p.id = @productId AND (@venue IS NULL OR p.venue_id = @venue);
            IF @si IS NULL BEGIN SELECT 1, CAST(0 AS bigint); RETURN; END;
            IF (SELECT ISNULL(MAX(id), 0) FROM dbo.stock_movements WHERE stock_item_id = @si) <> @stockVersion
                BEGIN SELECT 2, CAST(0 AS bigint); RETURN; END;
            DECLARE @factor int, @unit nvarchar(50);
            SELECT @factor = factor_to_base, @unit = unit_name FROM dbo.product_units
            WHERE product_id = @productId AND ((@unitName IS NULL AND factor_to_base = 1) OR unit_name = @unitName);
            IF @factor IS NULL AND @unitName IS NULL SELECT @factor = 1, @unit = NULL;
            IF @factor IS NULL BEGIN SELECT 9, CAST(0 AS bigint); RETURN; END;
            DECLARE @big bigint = CAST(@counted AS bigint) * @factor;
            IF @big > @maxBase BEGIN SELECT 10, CAST(0 AS bigint); RETURN; END;
            DECLARE @baseCounted int = CAST(@big AS int);
            DECLARE @difference int = @baseCounted - @onHand;
            IF @difference <> 0 AND @reason IS NULL BEGIN SELECT 3, CAST(0 AS bigint); RETURN; END;
            DECLARE @new TABLE (id bigint);
            INSERT dbo.stock_counts (stock_item_id, counted_quantity, system_quantity, difference, counted_by, status,
                                     reason, evidence, entered_unit, entered_quantity, stock_version)
            OUTPUT inserted.id INTO @new
            VALUES (@si, @baseCounted, @onHand, @difference, @staffUserId, CASE WHEN @difference = 0 THEN 'NoDifference' ELSE 'Pending' END,
                    @reason, @evidence, @unit, @counted, @stockVersion);
            DECLARE @id bigint = (SELECT id FROM @new);
            IF @difference <> 0
                INSERT dbo.approvals (entity_type, entity_id, requested_by, status) VALUES ('StockCount', @id, @staffUserId, 'Pending');
            SELECT 0, @id;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = request.ProductId;
        command.Parameters.Add("@counted", SqlDbType.Int).Value = request.CountedQuantity;
        command.Parameters.Add("@stockVersion", SqlDbType.BigInt).Value = request.StockVersion;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        command.Parameters.Add("@reason", SqlDbType.NVarChar, StockCountRules.MaxTextLength).Value = (object? )StockCountRules.Normalize(request.Reason) ?? DBNull.Value;
        command.Parameters.Add("@evidence", SqlDbType.NVarChar, StockCountRules.MaxTextLength).Value = (object? )StockCountRules.Normalize(request.Evidence) ?? DBNull.Value;
        var unit = StockUnitRules.NormalizeUnitName(request.UnitName);
        command.Parameters.Add("@unitName", SqlDbType.NVarChar, 50).Value = unit.Length == 0 ? DBNull.Value : unit;
        command.Parameters.Add("@maxBase", SqlDbType.BigInt).Value = StockUnitRules.MaxBaseQuantity;
        AddVenue(command, Venue);
        int status;
        long id;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            await reader.ReadAsync(cancellationToken);
            status = reader.GetInt32(0);
            id = reader.GetInt64(1);
        }

        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => StockCountResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy mặt hàng trong kho của cơ sở."),
                2 => StockCountResult.Fail("STOCK_CHANGED", "Tồn kho vừa thay đổi trong lúc kiểm kê. Đã tải lại số liệu, hãy đối chiếu và đếm lại.", await LoadSheetAsync(connection, null, request.ProductId, Venue, cancellationToken)),
                3 => StockCountResult.Fail("VALIDATION_FAILED", "Nhập lý do chênh lệch trước khi gửi đề xuất điều chỉnh."),
                9 => StockCountResult.Fail("UNIT_NOT_FOUND", "Mặt hàng không có đơn vị này."),
                _ => StockCountResult.Fail("QUANTITY_TOO_LARGE", "Số lượng quy đổi vượt giới hạn cho phép.")};
        }

        var view = await LoadCountAsync(connection, transaction, id, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StockCountResult.Ok(view!);
    }

    public async Task<IReadOnlyList<StockCountView>> ListAsync(string? status, long? productId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(CountSelect + """

              AND (@status IS NULL OR sc.status = @status)
              AND (@productId IS NULL OR si.product_id = @productId)
            ORDER BY CASE WHEN sc.status = 'Pending' THEN 0 ELSE 1 END, sc.id DESC
            OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY;
            """, connection);
        command.Parameters.Add("@status", SqlDbType.VarChar, 20).Value = (object? )status ?? DBNull.Value;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = (object? )productId ?? DBNull.Value;
        AddVenue(command, Venue);
        var counts = new List<StockCountView>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            counts.Add(ReadCount(reader));
        return counts;
    }

    public async Task<StockCountView?> GetAsync(long id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await LoadCountAsync(connection, null, id, Venue, cancellationToken);
    }

    // UC-44: an independent person approves or rejects. Approval posts one Adjustment movement linked to the count;
    // a retry of the same decision returns the stored result; a rejection changes no stock.
    public async Task<StockCountResult> DecideAsync(long id, long deciderId, bool approve, string? note, int? expectedDifference, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 done (now or before), 1 missing, 2 already decided otherwise, 3 self approval, 4 proposal changed,
        // 5 adjustment would go below held/zero stock.
        const string sql = """
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lock < 0 THROW 51000, 'Stock count is busy.', 1;
            DECLARE @status varchar(20), @difference int, @countedBy bigint, @si bigint;
            SELECT @status = sc.status, @difference = ISNULL(sc.difference, 0), @countedBy = sc.counted_by, @si = sc.stock_item_id
            FROM dbo.stock_counts sc WITH (UPDLOCK, HOLDLOCK)
            JOIN dbo.stock_items si ON si.id = sc.stock_item_id JOIN dbo.products p ON p.id = si.product_id
            WHERE sc.id = @id AND (@venue IS NULL OR p.venue_id = @venue);
            IF @status IS NULL BEGIN SELECT 1; RETURN; END;
            IF (@approve = 1 AND @status = 'Applied') OR (@approve = 0 AND @status = 'Rejected') BEGIN SELECT 0; RETURN; END;
            IF @status <> 'Pending' BEGIN SELECT 2; RETURN; END;
            IF @countedBy = @deciderId BEGIN SELECT 3; RETURN; END;
            IF @approve = 0
            BEGIN
                UPDATE dbo.stock_counts SET status = 'Rejected', approved_by = @deciderId, decided_at = SYSDATETIMEOFFSET(), decision_note = @note WHERE id = @id;
                UPDATE dbo.approvals SET approved_by = @deciderId, status = 'Rejected' WHERE entity_type = 'StockCount' AND entity_id = @id;
                INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
                VALUES (@deciderId, 'stock_adjustment_rejected', 'StockCount', @id, CONCAT('{"difference":', @difference, '}'));
                SELECT 0; RETURN;
            END;
            IF @expected IS NULL OR @expected <> @difference BEGIN SELECT 4; RETURN; END;
            DECLARE @posted TABLE (id bigint);
            UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand + @difference
            OUTPUT inserted.id INTO @posted
            WHERE id = @si AND quantity_on_hand + @difference >= reserved_quantity;
            IF NOT EXISTS (SELECT 1 FROM @posted) BEGIN SELECT 5; RETURN; END;
            DECLARE @movement TABLE (id bigint);
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
            OUTPUT inserted.id INTO @movement
            VALUES (@si, 'Adjustment', @difference, 'StockCount', @id, @deciderId);
            UPDATE dbo.stock_counts
            SET status = 'Applied', approved_by = @deciderId, decided_at = SYSDATETIMEOFFSET(), decision_note = @note,
                applied_movement_id = (SELECT id FROM @movement)
            WHERE id = @id;
            UPDATE dbo.approvals SET approved_by = @deciderId, status = 'Approved' WHERE entity_type = 'StockCount' AND entity_id = @id;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@deciderId, 'stock_adjustment_applied', 'StockCount', @id, CONCAT('{"difference":', @difference, '}'));
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:stock-count:{id}";
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
        command.Parameters.Add("@deciderId", SqlDbType.BigInt).Value = deciderId;
        command.Parameters.Add("@approve", SqlDbType.Bit).Value = approve;
        command.Parameters.Add("@note", SqlDbType.NVarChar, StockCountRules.MaxTextLength).Value = (object? )StockCountRules.Normalize(note) ?? DBNull.Value;
        command.Parameters.Add("@expected", SqlDbType.Int).Value = (object? )expectedDifference ?? DBNull.Value;
        AddVenue(command, Venue);
        int status;
        try
        {
            status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        }
        catch (SqlException ex)when (ex.Number is 2601 or 2627)
        {
            // UX_stock_movements_stock_count: the adjustment was already posted by a concurrent request.
            await transaction.RollbackAsync(CancellationToken.None);
            var existing = await GetAsync(id, cancellationToken);
            return existing is null ? StockCountResult.Fail("COUNT_NOT_FOUND", "Không tìm thấy phiếu kiểm kê.") : StockCountResult.Ok(existing);
        }

        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => StockCountResult.Fail("COUNT_NOT_FOUND", "Không tìm thấy phiếu kiểm kê."),
                2 => StockCountResult.Fail("COUNT_DECIDED", "Đề xuất này đã được quyết định trước đó."),
                3 => StockCountResult.Fail("SELF_APPROVAL", "Không được tự duyệt đề xuất do chính mình tạo."),
                4 => StockCountResult.Fail("COUNT_CHANGED", "Số chênh lệch khác với số bạn đã xem. Tải lại đề xuất trước khi duyệt."),
                _ => StockCountResult.Fail("ADJUSTMENT_BELOW_RESERVED", "Điều chỉnh làm tồn kho thấp hơn số đang giữ cho đơn hoặc âm kho. Cần xử lý đơn đang giữ trước.")};
        }

        var view = await LoadCountAsync(connection, transaction, id, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return StockCountResult.Ok(view!);
    }

    public async Task<SupplyReport> GetSupplyReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        // Movements without a timestamp (before upgrade 010) cannot be placed in a period and are left out.
        const string sql = """
            DECLARE @start datetimeoffset(0) = TODATETIMEOFFSET(CAST(@from AS datetime2), '+07:00');
            DECLARE @end datetimeoffset(0) = TODATETIMEOFFSET(DATEADD(day, 1, CAST(@to AS datetime2)), '+07:00');
            WITH mv AS (
                SELECT si.product_id, m.movement_type, m.quantity
                FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
                WHERE m.created_at >= @start AND m.created_at < @end
            ), gap AS (
                SELECT TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) AS product_id,
                       SUM(CASE WHEN TRY_CAST(JSON_VALUE(a.new_value, '$.difference') AS int) > 0
                                THEN TRY_CAST(JSON_VALUE(a.new_value, '$.difference') AS int) ELSE 0 END) AS unaccounted
                FROM dbo.audit_logs a JOIN dbo.sessions s ON s.id = a.entity_id
                WHERE a.entity_type = 'SessionSupply' AND a.action = 'supply_reconciled' AND s.session_date BETWEEN @from AND @to
                GROUP BY TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint)
            )
            SELECT p.id, p.sku, p.name, p.category, ISNULL(bu.unit_name, N'Cái'),
                   ISNULL(SUM(CASE WHEN mv.movement_type IN ('Receipt', 'OpeningBalance') THEN mv.quantity END), 0),
                   ISNULL(-SUM(CASE WHEN mv.movement_type = 'SaleOut' THEN mv.quantity END), 0),
                   ISNULL(-SUM(CASE WHEN mv.movement_type = 'SessionIssue' THEN mv.quantity END), 0),
                   ISNULL(SUM(CASE WHEN mv.movement_type = 'SessionReturn' THEN mv.quantity END), 0),
                   ISNULL(-SUM(CASE WHEN mv.movement_type = 'Issue' THEN mv.quantity END), 0),
                   ISNULL(SUM(CASE WHEN mv.movement_type = 'Adjustment' AND mv.quantity > 0 THEN mv.quantity END), 0),
                   ISNULL(-SUM(CASE WHEN mv.movement_type = 'Adjustment' AND mv.quantity < 0 THEN mv.quantity END), 0),
                   ISNULL(MAX(gap.unaccounted), 0),
                   ISNULL(MAX(si.quantity_on_hand), 0), ISNULL(MAX(si.reserved_quantity), 0)
            FROM dbo.products p
            LEFT JOIN dbo.stock_items si ON si.product_id = p.id
            LEFT JOIN mv ON mv.product_id = p.id
            LEFT JOIN gap ON gap.product_id = p.id
            LEFT JOIN dbo.product_units bu ON bu.product_id = p.id AND bu.factor_to_base = 1
            WHERE (@venue IS NULL OR p.venue_id = @venue)
            GROUP BY p.id, p.sku, p.name, p.category, bu.unit_name
            ORDER BY p.name;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@from", SqlDbType.Date).Value = from.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@to", SqlDbType.Date).Value = to.ToDateTime(TimeOnly.MinValue);
        AddVenue(command, Venue);
        var lines = new List<SupplyReportLine>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var issued = reader.GetInt32(7);
            var returned = reader.GetInt32(8);
            lines.Add(new SupplyReportLine(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4),
                reader.GetInt32(5), reader.GetInt32(6), issued, returned, issued - returned, reader.GetInt32(9), reader.GetInt32(10), reader.GetInt32(11), reader.GetInt32(12), reader.GetInt32(13), reader.GetInt32(14)));
        }

        return new SupplyReport(from, to, Venue, lines);
    }

    public async Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT p.id, p.sku, p.name, ISNULL(si.quantity_on_hand, 0),
                   ISNULL((SELECT SUM(m.quantity) FROM dbo.stock_movements m WHERE m.stock_item_id = si.id), 0),
                   ISNULL(si.reserved_quantity, 0),
                   ISNULL((SELECT SUM(CAST(oi.quantity AS int)) FROM dbo.order_items oi JOIN dbo.orders o ON o.id = oi.order_id
                           WHERE oi.product_id = p.id AND o.reservation_state = 'Held'), 0)
            FROM dbo.products p LEFT JOIN dbo.stock_items si ON si.product_id = p.id
            WHERE (@venue IS NULL OR p.venue_id = @venue)
            ORDER BY p.name;
            """, connection);
        AddVenue(command, Venue);
        var lines = new List<StockReconciliationLine>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            lines.Add(new StockReconciliationLine(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6)));
        return lines;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        return connection;
    }

    private static async Task<StockCountSheet?> LoadSheetAsync(SqlConnection connection, SqlTransaction? transaction, long productId, long? venue, CancellationToken cancellationToken)
    {
        StockCountSheet? sheet;
        await using (var command = new SqlCommand("""
            SELECT p.id, p.sku, p.name, si.quantity_on_hand, si.reserved_quantity,
                   (SELECT ISNULL(MAX(m.id), 0) FROM dbo.stock_movements m WHERE m.stock_item_id = si.id),
                   ISNULL(bu.unit_name, N'Cái')
            FROM dbo.products p JOIN dbo.stock_items si ON si.product_id = p.id
            LEFT JOIN dbo.product_units bu ON bu.product_id = p.id AND bu.factor_to_base = 1
            WHERE p.id = @productId AND (@venue IS NULL OR p.venue_id = @venue);
            """, connection, transaction))
        {
            command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            AddVenue(command, venue);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            sheet = await reader.ReadAsync(cancellationToken) ? new StockCountSheet(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.GetInt64(5), reader.GetString(6), []) : null;
        }

        if (sheet is null)
            return null;
        var units = new List<ProductUnit>();
        await using (var command = new SqlCommand("SELECT id, unit_name, factor_to_base, barcode FROM dbo.product_units WHERE product_id = @productId ORDER BY factor_to_base;", connection, transaction))
        {
            command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                units.Add(new ProductUnit(reader.GetInt64(0), reader.GetString(1), reader.GetInt32(2), reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return sheet with { Units = units };
    }

    private static async Task<StockCountView?> LoadCountAsync(SqlConnection connection, SqlTransaction? transaction, long id, long? venue, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(CountSelect + "\n  AND sc.id = @id;", connection, transaction);
        command.Parameters.Add("@id", SqlDbType.BigInt).Value = id;
        AddVenue(command, venue);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCount(reader) : null;
    }

    private static StockCountView ReadCount(SqlDataReader r) => new(r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetInt32(5), r.GetInt32(6),
        r.IsDBNull(7) ? null : r.GetString(7), r.IsDBNull(8) ? null : r.GetInt32(8), r.GetString(9), r.IsDBNull(10) ? null : r.GetString(10), r.IsDBNull(11) ? null : r.GetString(11),
        r.GetInt64(12), r.GetString(13), r.IsDBNull(14) ? null : r.GetDateTimeOffset(14), r.IsDBNull(15) ? null : r.GetInt64(15), r.IsDBNull(16) ? null : r.GetString(16),
        r.IsDBNull(17) ? null : r.GetDateTimeOffset(17), r.IsDBNull(18) ? null : r.GetString(18), r.IsDBNull(19) ? null : r.GetInt64(19), r.GetString(20), r.GetInt32(21), r.IsDBNull(22) ? null : r.GetInt64(22));
    private static void AddVenue(SqlCommand command, long? venue) => command.Parameters.Add("@venue", SqlDbType.BigInt).Value = (object? )venue ?? DBNull.Value;
}
