using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using be_da.Application.Abstractions;
using be_da.Application.Stock;

namespace be_da.Infrastructure.Persistence;

// No allocation table exists yet: quantities live in stock_movements (reference_type 'Session'),
// recipient and close-out decisions in audit_logs (entity_type 'SessionSupply').
public sealed class SqlSessionSupplyStore(string connectionString) : ISessionSupplyStore
{
    private const string SessionSelect = """
        SELECT s.id, s.session_date,
               ISNULL(ts.label, CONCAT(CONVERT(varchar(5), ts.start_time, 108), ' - ', CONVERT(varchar(5), ts.end_time, 108))) AS label,
               sp.name AS sport_name, c.name AS court_name, s.status,
               ISNULL(iss.cnt, 0) - ISNULL(rec.cnt, 0) AS outstanding, ISNULL(rec.cnt, 0) AS reconciled
        FROM dbo.sessions s
        JOIN dbo.time_slots ts ON ts.id = s.time_slot_id
        JOIN dbo.sports sp ON sp.id = s.sport_id
        JOIN dbo.courts c ON c.id = s.court_id
        OUTER APPLY (
            SELECT COUNT(DISTINCT si.product_id) AS cnt
            FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
            WHERE m.reference_type = 'Session' AND m.reference_id = s.id AND m.movement_type = 'SessionIssue'
        ) iss
        OUTER APPLY (
            SELECT COUNT(*) AS cnt FROM dbo.audit_logs a
            WHERE a.entity_type = 'SessionSupply' AND a.entity_id = s.id AND a.action = 'supply_reconciled'
        ) rec
        """;

    // Assigned referees of the session, plus Managers, may receive supplies (RP3 UC-41 A2).
    private const string RecipientsSql = """
        SELECT u.id, u.full_name, r.code
        FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id
        WHERE u.status = 'Active' AND u.deleted_at IS NULL
          AND (@recipientId IS NULL OR u.id = @recipientId)
          AND (r.code = 'Manager' OR (r.code = 'Referee'
               AND EXISTS (SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id = @sessionId AND sr.referee_id = u.id)))
        ORDER BY CASE r.code WHEN 'Referee' THEN 0 ELSE 1 END, u.full_name;
        """;

    private const string AllocationsSql = """
        WITH mv AS (
            SELECT si.product_id,
                   SUM(CASE WHEN m.movement_type = 'SessionIssue' THEN -m.quantity ELSE 0 END) AS issued,
                   SUM(CASE WHEN m.movement_type = 'SessionReturn' THEN m.quantity ELSE 0 END) AS returned
            FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
            WHERE m.reference_type = 'Session' AND m.reference_id = @sessionId
            GROUP BY si.product_id
        )
        SELECT mv.product_id, p.sku, p.name, mv.issued, mv.returned, rcp.recipient_id, u.full_name,
               CAST(CASE WHEN EXISTS (
                   SELECT 1 FROM dbo.audit_logs a
                   WHERE a.entity_type = 'SessionSupply' AND a.entity_id = @sessionId AND a.action = 'supply_reconciled'
                     AND TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) = mv.product_id
               ) THEN 1 ELSE 0 END AS bit) AS reconciled,
               rep.used AS reported_used
        FROM mv
        JOIN dbo.products p ON p.id = mv.product_id
        OUTER APPLY (
            SELECT TOP (1) TRY_CAST(JSON_VALUE(a.new_value, '$.recipientId') AS bigint) AS recipient_id
            FROM dbo.audit_logs a
            WHERE a.entity_type = 'SessionSupply' AND a.entity_id = @sessionId AND a.action = 'supply_issued'
              AND TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) = mv.product_id
            ORDER BY a.id DESC
        ) rcp
        OUTER APPLY (
            SELECT TOP (1) TRY_CAST(JSON_VALUE(a.new_value, '$.used') AS int) AS used
            FROM dbo.audit_logs a
            WHERE a.entity_type = 'SessionSupply' AND a.entity_id = @sessionId AND a.action = 'supply_use_reported'
              AND TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) = mv.product_id
            ORDER BY a.id DESC
        ) rep
        LEFT JOIN dbo.users u ON u.id = rcp.recipient_id
        ORDER BY p.name;
        """;

    public async Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, long? refereeId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var command = new SqlCommand(SessionSelect + """

            WHERE s.session_date = @date AND s.status <> 'Cancelled'
              AND (@refereeId IS NULL OR EXISTS (
                  SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id = s.id AND sr.referee_id = @refereeId))
            ORDER BY ts.start_time, c.name;
            """, connection);
        command.Parameters.Add("@date", SqlDbType.Date).Value = date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = (object?)refereeId ?? DBNull.Value;
        var sessions = new List<SupplySession>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) sessions.Add(ReadSession(reader));
        return sessions;
    }

    public async Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        return await LoadDetailsAsync(connection, null, sessionId, cancellationToken);
    }

    public async Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 posted, 1 session missing, 2 session cancelled, 3 recipient not allowed,
        // 4 allocation already closed, 5 product missing, 6 product inactive, 7 insufficient stock.
        var sql = """
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lock < 0 THROW 51000, 'Session supplies are busy.', 1;
            DECLARE @status varchar(100);
            SELECT @status = status FROM dbo.sessions WHERE id = @sessionId;
            IF @@ROWCOUNT = 0 BEGIN SELECT 1; RETURN; END;
            IF @status = 'Cancelled' BEGIN SELECT 2; RETURN; END;
            DECLARE @recipients TABLE (id bigint, full_name nvarchar(255), code varchar(100));
            INSERT @recipients EXEC sp_executesql @recipientsSql, N'@sessionId bigint, @recipientId bigint', @sessionId, @recipientId;
            IF NOT EXISTS (SELECT 1 FROM @recipients) BEGIN SELECT 3; RETURN; END;
            IF EXISTS (SELECT 1 FROM dbo.audit_logs WHERE entity_type = 'SessionSupply' AND entity_id = @sessionId
                AND action = 'supply_reconciled' AND TRY_CAST(JSON_VALUE(new_value, '$.productId') AS bigint) = @productId)
                BEGIN SELECT 4; RETURN; END;
            DECLARE @active bit;
            SELECT @active = ISNULL(is_active, 0) FROM dbo.products WHERE id = @productId;
            IF @@ROWCOUNT = 0 BEGIN SELECT 5; RETURN; END;
            IF @active = 0 BEGIN SELECT 6; RETURN; END;
            DECLARE @posted TABLE (id bigint);
            UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand - @quantity
            OUTPUT inserted.id INTO @posted
            WHERE product_id = @productId AND quantity_on_hand >= @quantity;
            IF NOT EXISTS (SELECT 1 FROM @posted) BEGIN SELECT 7; RETURN; END;
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
            SELECT id, 'SessionIssue', -@quantity, 'Session', @sessionId, @staffUserId FROM @posted;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@staffUserId, 'supply_issued', 'SessionSupply', @sessionId, @audit);
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = request.ProductId;
        command.Parameters.Add("@recipientId", SqlDbType.BigInt).Value = request.RecipientId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = request.Quantity;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        command.Parameters.Add("@recipientsSql", SqlDbType.NVarChar, -1).Value = RecipientsSql;
        command.Parameters.Add("@audit", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(new
        { productId = request.ProductId, quantity = request.Quantity, recipientId = request.RecipientId });
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("SESSION_NOT_FOUND", "Không tìm thấy buổi chơi."),
                2 => SupplyResult.Fail("SESSION_CANCELLED", "Buổi chơi đã hủy, không thể cấp vật tư."),
                3 => SupplyResult.Fail("INVALID_RECIPIENT", "Người nhận phải là trọng tài được phân công cho buổi này hoặc Quản lý."),
                4 => SupplyResult.Fail("ALLOCATION_CLOSED", "Vật tư này đã chốt sử dụng cho buổi chơi, không cấp thêm."),
                5 => SupplyResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy vật tư."),
                6 => SupplyResult.Fail("PRODUCT_INACTIVE", "Vật tư đã ngừng sử dụng."),
                _ => SupplyResult.Fail("INSUFFICIENT_STOCK", "Không đủ tồn kho để cấp số lượng này.")
            };
        }
        var details = await LoadDetailsAsync(connection, transaction, sessionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    public async Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.session_referees WHERE session_id = @sessionId AND referee_id = @refereeId;", connection);
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = refereeId;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 recorded, 1 not assigned, 2 nothing issued, 3 already closed, 4 use exceeds outstanding.
        const string sql = """
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lock < 0 THROW 51000, 'Session supplies are busy.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.session_referees WHERE session_id = @sessionId AND referee_id = @refereeId)
                BEGIN SELECT 1; RETURN; END;
            IF EXISTS (SELECT 1 FROM dbo.audit_logs WHERE entity_type = 'SessionSupply' AND entity_id = @sessionId
                AND action = 'supply_reconciled' AND TRY_CAST(JSON_VALUE(new_value, '$.productId') AS bigint) = @productId)
                BEGIN SELECT 3; RETURN; END;
            DECLARE @issued int, @returned int;
            SELECT @issued = ISNULL(SUM(CASE WHEN m.movement_type = 'SessionIssue' THEN -m.quantity ELSE 0 END), 0),
                   @returned = ISNULL(SUM(CASE WHEN m.movement_type = 'SessionReturn' THEN m.quantity ELSE 0 END), 0)
            FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
            WHERE m.reference_type = 'Session' AND m.reference_id = @sessionId AND si.product_id = @productId;
            IF @issued = 0 BEGIN SELECT 2; RETURN; END;
            IF @used > @issued - @returned BEGIN SELECT 4; RETURN; END;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@refereeId, 'supply_use_reported', 'SessionSupply', @sessionId,
                CONCAT('{"productId":', @productId, ',"used":', @used, '}'));
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@used", SqlDbType.Int).Value = request.UsedQuantity;
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = refereeId;
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("NOT_ASSIGNED", "Bạn không được phân công cho buổi chơi này."),
                2 => SupplyResult.Fail("ALLOCATION_NOT_FOUND", "Vật tư này chưa được cấp cho buổi chơi."),
                3 => SupplyResult.Fail("ALLOCATION_CLOSED", "Nhân viên đã chốt vật tư này, không sửa số đã dùng."),
                _ => SupplyResult.Fail("USE_EXCEEDS_ISSUED", "Số đã dùng vượt quá số được cấp.")
            };
        }
        var details = await LoadDetailsAsync(connection, transaction, sessionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    public async Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await be_da.Authorization.SportCenterConnection.OpenAsync(connection, cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 closed now, 1 session missing, 2 nothing issued, 3 return exceeds outstanding, 9 already closed.
        const string sql = """
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @lock < 0 THROW 51000, 'Session supplies are busy.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE id = @sessionId) BEGIN SELECT 1; RETURN; END;
            IF EXISTS (SELECT 1 FROM dbo.audit_logs WHERE entity_type = 'SessionSupply' AND entity_id = @sessionId
                AND action = 'supply_reconciled' AND TRY_CAST(JSON_VALUE(new_value, '$.productId') AS bigint) = @productId)
                BEGIN SELECT 9; RETURN; END;
            DECLARE @stockItemId bigint, @issued int, @returned int;
            SELECT @stockItemId = MIN(si.id),
                   @issued = ISNULL(SUM(CASE WHEN m.movement_type = 'SessionIssue' THEN -m.quantity ELSE 0 END), 0),
                   @returned = ISNULL(SUM(CASE WHEN m.movement_type = 'SessionReturn' THEN m.quantity ELSE 0 END), 0)
            FROM dbo.stock_movements m JOIN dbo.stock_items si ON si.id = m.stock_item_id
            WHERE m.reference_type = 'Session' AND m.reference_id = @sessionId AND si.product_id = @productId;
            IF @issued = 0 BEGIN SELECT 2; RETURN; END;
            IF @returnQuantity > @issued - @returned BEGIN SELECT 3; RETURN; END;
            IF @returnQuantity > 0
            BEGIN
                UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand + @returnQuantity WHERE id = @stockItemId;
                INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
                VALUES (@stockItemId, 'SessionReturn', @returnQuantity, 'Session', @sessionId, @staffUserId);
            END;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@staffUserId, 'supply_reconciled', 'SessionSupply', @sessionId,
                CONCAT('{"productId":', @productId, ',"returned":', @returned + @returnQuantity,
                       ',"used":', @issued - @returned - @returnQuantity, '}'));
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@returnQuantity", SqlDbType.Int).Value = request.ReturnedQuantity;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status is not (0 or 9))
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("SESSION_NOT_FOUND", "Không tìm thấy buổi chơi."),
                2 => SupplyResult.Fail("ALLOCATION_NOT_FOUND", "Vật tư này chưa được cấp cho buổi chơi."),
                _ => SupplyResult.Fail("RETURN_EXCEEDS_ISSUED", "Số trả lại vượt quá số đã cấp.")
            };
        }
        var details = await LoadDetailsAsync(connection, transaction, sessionId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    private static async Task<SessionSupplyDetails?> LoadDetailsAsync(SqlConnection connection, SqlTransaction? transaction, long sessionId, CancellationToken cancellationToken)
    {
        SupplySession? session;
        await using (var command = new SqlCommand(SessionSelect + "\nWHERE s.id = @sessionId;", connection, transaction))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            session = await reader.ReadAsync(cancellationToken) ? ReadSession(reader) : null;
        }
        if (session is null) return null;

        var recipients = new List<SupplyRecipient>();
        await using (var command = new SqlCommand(RecipientsSql, connection, transaction))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
            command.Parameters.Add("@recipientId", SqlDbType.BigInt).Value = DBNull.Value;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                recipients.Add(new SupplyRecipient(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        }

        var allocations = new List<SupplyAllocation>();
        await using (var command = new SqlCommand(AllocationsSql, connection, transaction))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                allocations.Add(new SupplyAllocation(
                    reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4),
                    reader.IsDBNull(5) ? null : reader.GetInt64(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetBoolean(7),
                    reader.IsDBNull(8) ? null : reader.GetInt32(8)));
            }
        }
        return new SessionSupplyDetails(session, recipients, allocations);
    }

    private static SupplySession ReadSession(SqlDataReader reader) => new(
        reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetString(2), reader.GetString(3),
        reader.GetString(4), reader.GetString(5), reader.GetInt32(6), reader.GetInt32(7));
}
