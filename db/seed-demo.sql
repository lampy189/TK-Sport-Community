-- TKSC development seed. Run db/schema.sql FIRST for a NEW database.
-- Existing databases must already match db/schema.sql. No schema changes here.
-- Execute in SSMS against Sep490. Demo identities cannot sign in with Google.
-- Re-running on the same day does not duplicate rows; later runs add future sessions.
USE [Sep490];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;
    -- Serialize concurrent executions of this seed.
    DECLARE @lockResult int;
    EXEC @lockResult = sys.sp_getapplock @Resource = N'TKSC-demo-seed-v1',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @lockResult < 0 THROW 50001, 'Cannot acquire demo seed lock.', 1;

    INSERT dbo.roles (code, name)
    SELECT v.code, v.name FROM (VALUES
        ('Player', N'Player'), ('Referee', N'Referee'), ('Staff', N'Staff'),
        ('Manager', N'Manager'), ('Owner', N'Owner'), ('SystemAdmin', N'System Admin')
    ) v(code, name)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.roles r WHERE r.code = v.code);

    INSERT dbo.sports (code, name)
    SELECT v.code, v.name FROM (VALUES
        ('badminton', N'Cầu lông'), ('pickleball', N'Pickleball')
    ) v(code, name)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.sports s WHERE s.code = v.code);

    DECLARE @accounts TABLE (email nvarchar(320), full_name nvarchar(255), role_code varchar(100));
    INSERT @accounts VALUES
        (N'manager@demo.tksc.invalid', N'Quản lý Demo', 'Manager'),
        (N'staff@demo.tksc.invalid', N'Nhân viên Demo', 'Staff'),
        (N'referee@demo.tksc.invalid', N'Trọng tài Demo', 'Referee'),
        (N'owner@demo.tksc.invalid', N'Chủ sân Demo', 'Owner'),
        (N'admin@demo.tksc.invalid', N'Quản trị Demo', 'SystemAdmin'),
        (N'player01@demo.tksc.invalid', N'Nguyễn Minh An', 'Player'),
        (N'player02@demo.tksc.invalid', N'Trần Thanh Bình', 'Player'),
        (N'player03@demo.tksc.invalid', N'Lê Lan Anh', 'Player'),
        (N'player04@demo.tksc.invalid', N'Phạm Quốc Huy', 'Player');

    INSERT dbo.users (role_id, email, full_name, status)
    SELECT r.id, a.email, a.full_name, 'Active'
    FROM @accounts a JOIN dbo.roles r ON r.code = a.role_code
    WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.email = a.email);

    INSERT dbo.players (user_id)
    SELECT u.id FROM @accounts a JOIN dbo.users u ON u.email = a.email
    JOIN dbo.roles r ON r.id = u.role_id AND r.code = 'Player'
    WHERE a.role_code = 'Player'
      AND NOT EXISTS (SELECT 1 FROM dbo.players p WHERE p.user_id = u.id);

    -- Independent, unrated sport profiles; do not fabricate assessment results.
    INSERT dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
    SELECT p.id, s.id, NULL, NULL, 'Unrated'
    FROM @accounts a JOIN dbo.users u ON u.email = a.email
    JOIN dbo.players p ON p.user_id = u.id
    CROSS JOIN dbo.sports s
    WHERE a.role_code = 'Player' AND s.code IN ('badminton', 'pickleball')
      AND NOT EXISTS (SELECT 1 FROM dbo.player_sport_profiles x WHERE x.player_id = p.id AND x.sport_id = s.id);

    IF NOT EXISTS (SELECT 1 FROM dbo.venues WHERE name = N'TKSC Demo')
        INSERT dbo.venues (name, address, is_active)
        VALUES (N'TKSC Demo', N'Địa điểm minh họa tại TP. Hồ Chí Minh', 1);
    DECLARE @venueId bigint = (SELECT MIN(id) FROM dbo.venues WHERE name = N'TKSC Demo');
    DECLARE @managerId bigint = (
        SELECT u.id FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id
        WHERE u.email = N'manager@demo.tksc.invalid' AND r.code = 'Manager' AND u.status = 'Active' AND u.deleted_at IS NULL);
    IF @managerId IS NULL THROW 50002, 'Demo manager is missing or inactive.', 1;

    INSERT dbo.courts (venue_id, name, sport_id, is_active)
    SELECT @venueId, v.name, s.id, 1
    FROM (VALUES ('badminton', N'Sân Demo Cầu lông'), ('pickleball', N'Sân Demo Pickleball')) v(code, name)
    JOIN dbo.sports s ON s.code = v.code
    WHERE NOT EXISTS (SELECT 1 FROM dbo.courts c WHERE c.venue_id = @venueId AND c.name = v.name);

    INSERT dbo.time_slots (start_time, end_time, label)
    SELECT CAST(v.start_time AS time), CAST(v.end_time AS time), v.label
    FROM (VALUES ('16:00', '18:00', N'16:00 - 18:00'),
                 ('18:00', '20:00', N'18:00 - 20:00'),
                 ('20:00', '22:00', N'20:00 - 22:00')) v(start_time, end_time, label)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.time_slots t
        WHERE t.start_time = CAST(v.start_time AS time) AND t.end_time = CAST(v.end_time AS time));

    -- Vietnam date, independent of SQL Server host timezone.
    DECLARE @today date = CONVERT(date, SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00'));
    INSERT dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    SELECT c.sport_id, c.id, t.id, DATEADD(day, d.n, @today), NULL, 8,
        CASE s.code WHEN 'badminton' THEN 80000 ELSE 100000 END, 'Published', @managerId
    FROM dbo.courts c JOIN dbo.sports s ON s.id = c.sport_id
    CROSS JOIN (SELECT MIN(id) AS id FROM dbo.time_slots
                WHERE (start_time = '16:00' AND end_time = '18:00')
                   OR (start_time = '18:00' AND end_time = '20:00')
                   OR (start_time = '20:00' AND end_time = '22:00')
                GROUP BY start_time, end_time) t
    CROSS JOIN (VALUES (1), (2), (3), (4), (5), (6), (7)) d(n)
    WHERE c.venue_id = @venueId AND c.is_active = 1
      AND c.name IN (N'Sân Demo Cầu lông', N'Sân Demo Pickleball')
      AND NOT EXISTS (SELECT 1 FROM dbo.sessions x
          WHERE x.court_id = c.id AND x.time_slot_id = t.id AND x.session_date = DATEADD(day, d.n, @today));

    INSERT dbo.products (sku, name, category, unit_price_vnd, is_active)
    SELECT v.sku, v.name, v.category, v.price, 1
    FROM (VALUES ('DEMO-WATER', N'Nước suối 500ml', 'Drink', 10000),
                 ('DEMO-SHUTTLE', N'Quả cầu lông', 'Equipment', 25000),
                 ('DEMO-PICKLEBALL', N'Bóng Pickleball', 'Equipment', 50000)) v(sku, name, category, price)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.products p WHERE p.sku = v.sku);
    -- Start stock at zero: no fabricated inventory receipts or sales.
    INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level)
    SELECT p.id, 0, 10 FROM dbo.products p
    WHERE p.sku IN ('DEMO-WATER', 'DEMO-SHUTTLE', 'DEMO-PICKLEBALL')
      AND NOT EXISTS (SELECT 1 FROM dbo.stock_items x WHERE x.product_id = p.id);

    COMMIT TRANSACTION;
    SELECT N'Seed completed' AS result, @today AS vietnam_date;
    SELECT s.id, sp.name AS sport, c.name AS court, s.session_date,
           t.start_time, t.end_time, s.capacity, s.price_vnd, s.status
    FROM dbo.sessions s JOIN dbo.courts c ON c.id = s.court_id
    JOIN dbo.sports sp ON sp.id = s.sport_id JOIN dbo.time_slots t ON t.id = s.time_slot_id
    WHERE c.venue_id = @venueId AND s.session_date BETWEEN DATEADD(day, 1, @today) AND DATEADD(day, 7, @today)
    ORDER BY s.session_date, t.start_time, c.name;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
