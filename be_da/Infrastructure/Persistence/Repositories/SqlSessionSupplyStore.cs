using be_da.Application.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Validators;

namespace be_da.Infrastructure.Persistence.Repositories;
// Quantities live in stock_movements (reference_type 'Session'); recipient, use reports and close-out decisions
// in audit_logs (entity_type 'SessionSupply'); referee requests in session_supply_requests (upgrade 017).
// Every query filters by the caller's center itself because Row-Level Security may be disabled on a database.
public sealed class SqlSessionSupplyStore(string connectionString, be_da.Application.Interfaces.External.IDataAccessContext? dataScope = null) : ISessionSupplyStore
{
    private const string SessionSelect = """
        SELECT s.id, s.session_date,
               ISNULL(ts.label, CONCAT(CONVERT(varchar(5), ts.start_time, 108), ' - ', CONVERT(varchar(5), ts.end_time, 108))) AS label,
               sp.name AS sport_name, c.name AS court_name, s.status,
               ISNULL(iss.cnt, 0) - ISNULL(rec.cnt, 0) AS outstanding, ISNULL(rec.cnt, 0) AS reconciled,
               ISNULL(req.cnt, 0) AS pending_requests
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
        OUTER APPLY (
            SELECT COUNT(*) AS cnt FROM dbo.session_supply_requests r WHERE r.session_id = s.id AND r.status = 'Pending'
        ) req
        """;
    // Assigned referees of the session, plus Managers of the session's center, may receive supplies (RP3 UC-41 A2).
    private const string RecipientsSql = """
        SELECT u.id, u.full_name, r.code
        FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id
        WHERE u.status = 'Active' AND u.deleted_at IS NULL
          AND (@recipientId IS NULL OR u.id = @recipientId)
          AND ((r.code = 'Manager' AND (
                   NOT EXISTS (SELECT 1 FROM dbo.sessions s2 JOIN dbo.courts c2 ON c2.id = s2.court_id WHERE s2.id = @sessionId AND c2.venue_id IS NOT NULL)
                   OR u.venue_id = (SELECT c2.venue_id FROM dbo.sessions s2 JOIN dbo.courts c2 ON c2.id = s2.court_id WHERE s2.id = @sessionId)))
               OR (r.code = 'Referee'
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
               CAST(CASE WHEN rc.id IS NOT NULL THEN 1 ELSE 0 END AS bit) AS reconciled,
               rep.used AS reported_used,
               ISNULL(bu.unit_name, N'Cái') AS base_unit,
               rep.return_offered,
               TRY_CAST(JSON_VALUE(rc.new_value, '$.difference') AS int) AS difference
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
            SELECT TOP (1) TRY_CAST(JSON_VALUE(a.new_value, '$.used') AS int) AS used,
                   TRY_CAST(JSON_VALUE(a.new_value, '$.returnOffered') AS int) AS return_offered
            FROM dbo.audit_logs a
            WHERE a.entity_type = 'SessionSupply' AND a.entity_id = @sessionId AND a.action = 'supply_use_reported'
              AND TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) = mv.product_id
            ORDER BY a.id DESC
        ) rep
        OUTER APPLY (
            SELECT TOP (1) a.id, a.new_value FROM dbo.audit_logs a
            WHERE a.entity_type = 'SessionSupply' AND a.entity_id = @sessionId AND a.action = 'supply_reconciled'
              AND TRY_CAST(JSON_VALUE(a.new_value, '$.productId') AS bigint) = mv.product_id
            ORDER BY a.id DESC
        ) rc
        LEFT JOIN dbo.users u ON u.id = rcp.recipient_id
        LEFT JOIN dbo.product_units bu ON bu.product_id = mv.product_id AND bu.factor_to_base = 1
        ORDER BY p.name;
        """;
    private const string RequestsSql = """
        SELECT r.id, r.product_id, p.sku, p.name, r.quantity, r.entered_unit, r.entered_quantity, r.note, r.status,
               r.issued_quantity, r.requested_by, ISNULL(ru.full_name, N''), hu.full_name, r.created_at, r.handled_at,
               r.decision_note, ISNULL(bu.unit_name, N'Cái')
        FROM dbo.session_supply_requests r
        JOIN dbo.products p ON p.id = r.product_id
        LEFT JOIN dbo.users ru ON ru.id = r.requested_by
        LEFT JOIN dbo.users hu ON hu.id = r.handled_by
        LEFT JOIN dbo.product_units bu ON bu.product_id = r.product_id AND bu.factor_to_base = 1
        WHERE r.session_id = @sessionId
        ORDER BY CASE r.status WHEN 'Pending' THEN 0 ELSE 1 END, r.id DESC;
        """;
    // Shared guard: session of the caller's center, not cancelled. Sets @sessionVenue, @sessionStatus, @sessionDate.
    private const string SessionGuard = """
        DECLARE @sessionStatus varchar(100), @sessionDate date, @sessionVenue bigint;
        SELECT @sessionStatus = s.status, @sessionDate = s.session_date, @sessionVenue = c.venue_id
        FROM dbo.sessions s JOIN dbo.courts c ON c.id = s.court_id
        WHERE s.id = @sessionId AND (@venue IS NULL OR c.venue_id = @venue);
        """;
    private const string LockSql = """
        DECLARE @lock int;
        EXEC @lock = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
        IF @lock < 0 THROW 51000, 'Session supplies are busy.', 1;
        """;
    // Resolves the unit (empty = base unit) into @factor/@unit and converts @quantity into @baseQuantity.
    private const string ConvertSql = """
        IF NOT EXISTS (SELECT 1 FROM dbo.product_units WHERE product_id = @productId AND factor_to_base = 1)
            INSERT dbo.product_units (product_id, unit_name, factor_to_base) VALUES (@productId, N'Cái', 1);
        DECLARE @factor int, @unit nvarchar(50);
        SELECT @factor = factor_to_base, @unit = unit_name FROM dbo.product_units
        WHERE product_id = @productId AND ((@unitName IS NULL AND factor_to_base = 1) OR unit_name = @unitName);
        IF @factor IS NULL BEGIN SELECT 9; RETURN; END;
        DECLARE @big bigint = CAST(@quantity AS bigint) * @factor;
        IF @big > @maxBase BEGIN SELECT 10; RETURN; END;
        DECLARE @baseQuantity int = CAST(@big AS int);
        """;
    private long? Venue => dataScope?.VenueId;

    public async Task<IReadOnlyList<SupplySession>> GetSessionsAsync(DateOnly date, long? refereeId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand(SessionSelect + """

            WHERE s.session_date = @date AND s.status <> 'Cancelled'
              AND (@venue IS NULL OR c.venue_id = @venue)
              AND (@refereeId IS NULL OR EXISTS (
                  SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id = s.id AND sr.referee_id = @refereeId))
            ORDER BY ts.start_time, c.name;
            """, connection);
        command.Parameters.Add("@date", SqlDbType.Date).Value = date.ToDateTime(TimeOnly.MinValue);
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = (object? )refereeId ?? DBNull.Value;
        AddVenue(command, Venue);
        var sessions = new List<SupplySession>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            sessions.Add(ReadSession(reader));
        return sessions;
    }

    public async Task<SessionSupplyDetails?> GetDetailsAsync(long sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        return await LoadDetailsAsync(connection, null, sessionId, Venue, cancellationToken);
    }

    public Task<SupplyResult> IssueAsync(long sessionId, IssueSupplyRequest request, long staffUserId, CancellationToken cancellationToken) =>
        IssueCoreAsync(sessionId, null, request.ProductId, request.RecipientId, request.Quantity, request.UnitName, staffUserId, cancellationToken);

    public Task<SupplyResult> IssueRequestAsync(long sessionId, long requestId, IssueSupplyFromRequest request, long staffUserId, CancellationToken cancellationToken) =>
        IssueCoreAsync(sessionId, requestId, 0, 0, request.Quantity, request.UnitName, staffUserId, cancellationToken);

    // One issue posts one SessionIssue movement and lowers on-hand stock; goods held for orders cannot be issued.
    // With a request, product and recipient come from the request and the request is closed in the same transaction.
    private async Task<SupplyResult> IssueCoreAsync(long sessionId, long? requestId, long productId, long recipientId, int? quantity, string? unitName, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 posted, 1 session missing, 2 session cancelled, 3 recipient not allowed, 4 allocation already closed,
        // 5 product missing, 6 product inactive, 7 insufficient stock, 8 session already ended, 9 unknown unit,
        // 10 too large to post, 11 request missing, 12 request already handled.
        var sql = LockSql + SessionGuard + """
            IF @sessionStatus IS NULL BEGIN SELECT 1; RETURN; END;
            IF @sessionStatus = 'Cancelled' BEGIN SELECT 2; RETURN; END;
            IF @sessionDate < CONVERT(date, SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')) BEGIN SELECT 8; RETURN; END;
            IF @requestId IS NOT NULL
            BEGIN
                -- No quantity in the body: issue exactly what was requested, in the unit it was requested in.
                DECLARE @requestStatus varchar(20), @useRequested bit = CASE WHEN @quantity IS NULL THEN 1 ELSE 0 END;
                SELECT @requestStatus = status, @productId = product_id, @recipientId = requested_by,
                       @quantity = CASE WHEN @useRequested = 1 THEN ISNULL(entered_quantity, quantity) ELSE @quantity END,
                       @unitName = CASE WHEN @useRequested = 1 THEN CASE WHEN entered_quantity IS NULL THEN NULL ELSE entered_unit END ELSE @unitName END
                FROM dbo.session_supply_requests WITH (UPDLOCK, HOLDLOCK)
                WHERE id = @requestId AND session_id = @sessionId;
                IF @requestStatus IS NULL BEGIN SELECT 11; RETURN; END;
                IF @requestStatus <> 'Pending' BEGIN SELECT 12; RETURN; END;
            END;
            DECLARE @recipients TABLE (id bigint, full_name nvarchar(255), code varchar(100));
            INSERT @recipients EXEC sp_executesql @recipientsSql, N'@sessionId bigint, @recipientId bigint', @sessionId, @recipientId;
            IF NOT EXISTS (SELECT 1 FROM @recipients) BEGIN SELECT 3; RETURN; END;
            IF EXISTS (SELECT 1 FROM dbo.audit_logs WHERE entity_type = 'SessionSupply' AND entity_id = @sessionId
                AND action = 'supply_reconciled' AND TRY_CAST(JSON_VALUE(new_value, '$.productId') AS bigint) = @productId)
                BEGIN SELECT 4; RETURN; END;
            DECLARE @active bit, @productVenue bigint;
            SELECT @active = ISNULL(is_active, 0), @productVenue = venue_id FROM dbo.products WHERE id = @productId;
            IF @@ROWCOUNT = 0 OR (@productVenue IS NOT NULL AND @sessionVenue IS NOT NULL AND @productVenue <> @sessionVenue)
                BEGIN SELECT 5; RETURN; END;
            IF @active = 0 BEGIN SELECT 6; RETURN; END;
            """ + ConvertSql + """
            DECLARE @posted TABLE (id bigint);
            UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand - @baseQuantity
            OUTPUT inserted.id INTO @posted
            WHERE product_id = @productId AND quantity_on_hand - reserved_quantity >= @baseQuantity;
            IF NOT EXISTS (SELECT 1 FROM @posted) BEGIN SELECT 7; RETURN; END;
            INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by, entered_unit, entered_quantity)
            SELECT id, 'SessionIssue', -@baseQuantity, 'Session', @sessionId, @staffUserId, @unit, @quantity FROM @posted;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@staffUserId, 'supply_issued', 'SessionSupply', @sessionId,
                CONCAT('{"productId":', @productId, ',"quantity":', @baseQuantity, ',"recipientId":', @recipientId,
                       CASE WHEN @requestId IS NULL THEN '' ELSE CONCAT(',"requestId":', @requestId) END, '}'));
            IF @requestId IS NOT NULL
                UPDATE dbo.session_supply_requests
                SET status = 'Issued', issued_quantity = @baseQuantity, handled_by = @staffUserId, handled_at = SYSDATETIMEOFFSET()
                WHERE id = @requestId;
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@requestId", SqlDbType.BigInt).Value = (object? )requestId ?? DBNull.Value;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@recipientId", SqlDbType.BigInt).Value = recipientId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = (object? )quantity ?? DBNull.Value;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        command.Parameters.Add("@recipientsSql", SqlDbType.NVarChar, -1).Value = RecipientsSql;
        AddUnit(command, unitName);
        command.Parameters.Add("@maxBase", SqlDbType.BigInt).Value = StockUnitRules.MaxBaseQuantity;
        AddVenue(command, Venue);
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("SESSION_NOT_FOUND", "Không tìm thấy buổi chơi."),
                2 => SupplyResult.Fail("SESSION_CANCELLED", "Buổi chơi đã hủy, không thể cấp vật tư."),
                3 => SupplyResult.Fail("INVALID_RECIPIENT", "Người nhận phải là trọng tài được phân công cho buổi này hoặc Quản lý của cơ sở."),
                4 => SupplyResult.Fail("ALLOCATION_CLOSED", "Vật tư này đã chốt sử dụng cho buổi chơi, không cấp thêm."),
                5 => SupplyResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy vật tư của cơ sở này."),
                6 => SupplyResult.Fail("PRODUCT_INACTIVE", "Vật tư đã ngừng sử dụng."),
                8 => SupplyResult.Fail("SESSION_ENDED", "Buổi chơi đã qua, không thể cấp thêm vật tư."),
                9 => SupplyResult.Fail("UNIT_NOT_FOUND", "Vật tư không có đơn vị này."),
                10 => SupplyResult.Fail("QUANTITY_TOO_LARGE", "Số lượng quy đổi vượt giới hạn cho phép."),
                11 => SupplyResult.Fail("REQUEST_NOT_FOUND", "Không tìm thấy yêu cầu vật tư của buổi này."),
                12 => SupplyResult.Fail("REQUEST_HANDLED", "Yêu cầu này đã được xử lý."),
                _ => SupplyResult.Fail("INSUFFICIENT_STOCK", "Không đủ số còn khả dụng (tồn kho trừ hàng đang giữ cho đơn) để cấp số lượng này.")};
        }

        var details = await LoadDetailsAsync(connection, transaction, sessionId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    public async Task<SupplyResult> CreateRequestAsync(long sessionId, CreateSupplyRequest request, long refereeId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 saved, 1 not assigned / session missing, 2 cancelled, 5 product missing, 6 inactive, 8 ended,
        // 9 unknown unit, 10 too large.
        var sql = LockSql + SessionGuard + """
            IF @sessionStatus IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.session_referees WHERE session_id = @sessionId AND referee_id = @refereeId)
                BEGIN SELECT 1; RETURN; END;
            IF @sessionStatus = 'Cancelled' BEGIN SELECT 2; RETURN; END;
            IF @sessionDate < CONVERT(date, SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00')) BEGIN SELECT 8; RETURN; END;
            DECLARE @active bit, @productVenue bigint;
            SELECT @active = ISNULL(is_active, 0), @productVenue = venue_id FROM dbo.products WHERE id = @productId;
            IF @@ROWCOUNT = 0 OR (@productVenue IS NOT NULL AND @sessionVenue IS NOT NULL AND @productVenue <> @sessionVenue)
                BEGIN SELECT 5; RETURN; END;
            IF @active = 0 BEGIN SELECT 6; RETURN; END;
            """ + ConvertSql + """
            INSERT dbo.session_supply_requests (session_id, product_id, requested_by, quantity, entered_unit, entered_quantity, note)
            VALUES (@sessionId, @productId, @refereeId, @baseQuantity, @unit, @quantity, @note);
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = request.ProductId;
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = refereeId;
        command.Parameters.Add("@quantity", SqlDbType.Int).Value = request.Quantity;
        command.Parameters.Add("@note", SqlDbType.NVarChar, SupplyRules.MaxNoteLength).Value = (object? )SupplyRules.NormalizeNote(request.Note) ?? DBNull.Value;
        AddUnit(command, request.UnitName);
        command.Parameters.Add("@maxBase", SqlDbType.BigInt).Value = StockUnitRules.MaxBaseQuantity;
        AddVenue(command, Venue);
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("NOT_ASSIGNED", "Bạn không được phân công cho buổi chơi này."),
                2 => SupplyResult.Fail("SESSION_CANCELLED", "Buổi chơi đã hủy."),
                5 => SupplyResult.Fail("PRODUCT_NOT_FOUND", "Không tìm thấy vật tư của cơ sở này."),
                6 => SupplyResult.Fail("PRODUCT_INACTIVE", "Vật tư đã ngừng sử dụng."),
                8 => SupplyResult.Fail("SESSION_ENDED", "Buổi chơi đã qua, không thể yêu cầu thêm vật tư."),
                9 => SupplyResult.Fail("UNIT_NOT_FOUND", "Vật tư không có đơn vị này."),
                _ => SupplyResult.Fail("QUANTITY_TOO_LARGE", "Số lượng quy đổi vượt giới hạn cho phép.")};
        }

        var details = await LoadDetailsAsync(connection, transaction, sessionId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    // Staff rejects a pending request; a referee cancels their own pending request.
    public Task<SupplyResult> RejectRequestAsync(long sessionId, long requestId, string note, long staffUserId, CancellationToken cancellationToken) =>
        CloseRequestAsync(sessionId, requestId, "Rejected", note, staffUserId, null, cancellationToken);

    public Task<SupplyResult> CancelRequestAsync(long sessionId, long requestId, long refereeId, CancellationToken cancellationToken) =>
        CloseRequestAsync(sessionId, requestId, "Cancelled", null, refereeId, refereeId, cancellationToken);

    private async Task<SupplyResult> CloseRequestAsync(long sessionId, long requestId, string newStatus, string? note, long actorId, long? ownerId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var sql = LockSql + SessionGuard + """
            IF @sessionStatus IS NULL BEGIN SELECT 11; RETURN; END;
            DECLARE @status varchar(20);
            SELECT @status = status FROM dbo.session_supply_requests WITH (UPDLOCK, HOLDLOCK)
            WHERE id = @requestId AND session_id = @sessionId AND (@ownerId IS NULL OR requested_by = @ownerId);
            IF @status IS NULL BEGIN SELECT 11; RETURN; END;
            IF @status = @newStatus BEGIN SELECT 0; RETURN; END;
            IF @status <> 'Pending' BEGIN SELECT 12; RETURN; END;
            UPDATE dbo.session_supply_requests
            SET status = @newStatus, handled_by = @actorId, handled_at = SYSDATETIMEOFFSET(), decision_note = @note
            WHERE id = @requestId;
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@requestId", SqlDbType.BigInt).Value = requestId;
        command.Parameters.Add("@ownerId", SqlDbType.BigInt).Value = (object? )ownerId ?? DBNull.Value;
        command.Parameters.Add("@actorId", SqlDbType.BigInt).Value = actorId;
        command.Parameters.Add("@newStatus", SqlDbType.VarChar, 20).Value = newStatus;
        command.Parameters.Add("@note", SqlDbType.NVarChar, SupplyRules.MaxNoteLength).Value = (object? )SupplyRules.NormalizeNote(note) ?? DBNull.Value;
        AddVenue(command, Venue);
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status == 11 ? SupplyResult.Fail("REQUEST_NOT_FOUND", "Không tìm thấy yêu cầu vật tư.") : SupplyResult.Fail("REQUEST_HANDLED", "Yêu cầu này đã được xử lý.");
        }

        var details = await LoadDetailsAsync(connection, transaction, sessionId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    public async Task<bool> IsRefereeAssignedAsync(long sessionId, long refereeId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.session_referees sr
            JOIN dbo.sessions s ON s.id = sr.session_id JOIN dbo.courts c ON c.id = s.court_id
            WHERE sr.session_id = @sessionId AND sr.referee_id = @refereeId AND (@venue IS NULL OR c.venue_id = @venue);
            """, connection);
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = refereeId;
        AddVenue(command, Venue);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task<SupplyResult> ReportUseAsync(long sessionId, long productId, ReportSupplyUseRequest request, long refereeId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 recorded, 1 not assigned, 2 nothing issued, 3 already closed, 4 use (+ offered return) exceeds outstanding.
        var sql = LockSql + SessionGuard + """
            IF @sessionStatus IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.session_referees WHERE session_id = @sessionId AND referee_id = @refereeId)
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
            IF @used + ISNULL(@returnOffered, 0) > @issued - @returned BEGIN SELECT 4; RETURN; END;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@refereeId, 'supply_use_reported', 'SessionSupply', @sessionId,
                CONCAT('{"productId":', @productId, ',"used":', @used,
                       CASE WHEN @returnOffered IS NULL THEN '' ELSE CONCAT(',"returnOffered":', @returnOffered) END, '}'));
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@used", SqlDbType.Int).Value = request.UsedQuantity;
        command.Parameters.Add("@returnOffered", SqlDbType.Int).Value = (object? )request.ReturnQuantity ?? DBNull.Value;
        command.Parameters.Add("@refereeId", SqlDbType.BigInt).Value = refereeId;
        AddVenue(command, Venue);
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status != 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("NOT_ASSIGNED", "Bạn không được phân công cho buổi chơi này."),
                2 => SupplyResult.Fail("ALLOCATION_NOT_FOUND", "Vật tư này chưa được cấp cho buổi chơi."),
                3 => SupplyResult.Fail("ALLOCATION_CLOSED", "Nhân viên đã chốt vật tư này, không sửa số đã dùng."),
                _ => SupplyResult.Fail("USE_EXCEEDS_ISSUED", "Số đã dùng cộng số trả lại vượt quá số được cấp.")};
        }

        var details = await LoadDetailsAsync(connection, transaction, sessionId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    // Staff confirms the quantity that physically came back: posts the return once and closes the allocation.
    // The difference against the referee's report is kept for a stock count (RP3 UC-42 step 6).
    public async Task<SupplyResult> ReconcileAsync(long sessionId, long productId, ReturnSupplyRequest request, long staffUserId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        // Status: 0 closed now, 1 session missing, 2 nothing issued, 3 return exceeds outstanding, 9 already closed.
        var sql = LockSql + SessionGuard + """
            IF @sessionStatus IS NULL BEGIN SELECT 1; RETURN; END;
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
            DECLARE @reported int = (
                SELECT TOP (1) TRY_CAST(JSON_VALUE(new_value, '$.used') AS int) FROM dbo.audit_logs
                WHERE entity_type = 'SessionSupply' AND entity_id = @sessionId AND action = 'supply_use_reported'
                  AND TRY_CAST(JSON_VALUE(new_value, '$.productId') AS bigint) = @productId
                ORDER BY id DESC);
            IF @returnQuantity > 0
            BEGIN
                UPDATE dbo.stock_items SET quantity_on_hand = quantity_on_hand + @returnQuantity WHERE id = @stockItemId;
                INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id, created_by)
                VALUES (@stockItemId, 'SessionReturn', @returnQuantity, 'Session', @sessionId, @staffUserId);
            END;
            DECLARE @used int = @issued - @returned - @returnQuantity;
            INSERT dbo.audit_logs (actor_id, action, entity_type, entity_id, new_value)
            VALUES (@staffUserId, 'supply_reconciled', 'SessionSupply', @sessionId,
                CONCAT('{"productId":', @productId, ',"returned":', @returned + @returnQuantity, ',"used":', @used,
                       CASE WHEN @reported IS NULL THEN '' ELSE CONCAT(',"reportedUsed":', @reported, ',"difference":', @used - @reported) END, '}'));
            SELECT 0;
            """;
        await using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.Add("@lockName", SqlDbType.NVarChar, 255).Value = $"TKSC:session-supply:{sessionId}";
        command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
        command.Parameters.Add("@productId", SqlDbType.BigInt).Value = productId;
        command.Parameters.Add("@returnQuantity", SqlDbType.Int).Value = request.ReturnedQuantity;
        command.Parameters.Add("@staffUserId", SqlDbType.BigInt).Value = staffUserId;
        AddVenue(command, Venue);
        var status = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        if (status is not (0 or 9))
        {
            await transaction.RollbackAsync(cancellationToken);
            return status switch
            {
                1 => SupplyResult.Fail("SESSION_NOT_FOUND", "Không tìm thấy buổi chơi."),
                2 => SupplyResult.Fail("ALLOCATION_NOT_FOUND", "Vật tư này chưa được cấp cho buổi chơi."),
                _ => SupplyResult.Fail("RETURN_EXCEEDS_ISSUED", "Số trả lại vượt quá số đã cấp.")};
        }

        var details = await LoadDetailsAsync(connection, transaction, sessionId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return SupplyResult.Ok(details!);
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await be_da.Infrastructure.Persistence.SportCenterConnection.OpenAsync(connection, cancellationToken, dataScope);
        return connection;
    }

    // venue null: no center filter (the caller already checked the session's center in the same transaction).
    private static async Task<SessionSupplyDetails?> LoadDetailsAsync(SqlConnection connection, SqlTransaction? transaction, long sessionId, long? venue, CancellationToken cancellationToken)
    {
        SupplySession? session;
        await using (var command = new SqlCommand(SessionSelect + "\nWHERE s.id = @sessionId AND (@venue IS NULL OR c.venue_id = @venue);", connection, transaction))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
            AddVenue(command, venue);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            session = await reader.ReadAsync(cancellationToken) ? ReadSession(reader) : null;
        }

        if (session is null)
            return null;
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
                allocations.Add(new SupplyAllocation(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.IsDBNull(6) ? null : reader.GetString(6), reader.GetBoolean(7), reader.IsDBNull(8) ? null : reader.GetInt32(8), reader.GetString(9), reader.IsDBNull(10) ? null : reader.GetInt32(10), reader.IsDBNull(11) ? null : reader.GetInt32(11)));
            }
        }

        var requests = new List<SupplyRequestView>();
        await using (var command = new SqlCommand(RequestsSql, connection, transaction))
        {
            command.Parameters.Add("@sessionId", SqlDbType.BigInt).Value = sessionId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                requests.Add(new SupplyRequestView(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetString(3), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetString(7), reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetInt32(9), reader.GetInt64(10), reader.GetString(11), reader.IsDBNull(12) ? null : reader.GetString(12), reader.GetDateTimeOffset(13), reader.IsDBNull(14) ? null : reader.GetDateTimeOffset(14), reader.IsDBNull(15) ? null : reader.GetString(15), reader.GetString(16)));
            }
        }

        return new SessionSupplyDetails(session, recipients, allocations, requests);
    }

    private static void AddVenue(SqlCommand command, long? venue) => command.Parameters.Add("@venue", SqlDbType.BigInt).Value = (object? )venue ?? DBNull.Value;
    private static void AddUnit(SqlCommand command, string? unitName)
    {
        var unit = StockUnitRules.NormalizeUnitName(unitName);
        command.Parameters.Add("@unitName", SqlDbType.NVarChar, 50).Value = unit.Length == 0 ? DBNull.Value : unit;
    }

    private static SupplySession ReadSession(SqlDataReader reader) => new(reader.GetInt64(0), DateOnly.FromDateTime(reader.GetDateTime(1)), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetInt32(6), reader.GetInt32(7), reader.GetInt32(8));
}
