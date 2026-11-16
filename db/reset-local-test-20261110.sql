-- Reset local test transactions, preserve accounts and reference configuration.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() <> 'Sep490' THROW 51000,'Wrong database',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRANSACTION;
DECLARE @sql nvarchar(max)='';
SELECT @sql=@sql+'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+'.'+QUOTENAME(name)+' NOCHECK CONSTRAINT ALL;' FROM sys.tables;
EXEC sys.sp_executesql @sql;
SET @sql='';
SELECT @sql=@sql+'DELETE FROM dbo.'+QUOTENAME(name)+';' FROM sys.tables
WHERE name NOT IN ('users','roles','players','player_sport_profiles','sports','venues','courts','time_slots','rubrics','rubric_dimensions','policies','products','stock_items','product_units');
EXEC sys.sp_executesql @sql;
UPDATE dbo.player_sport_profiles SET current_score=NULL,current_band=NULL,rating_status='Unrated';
UPDATE dbo.stock_items SET quantity_on_hand=100,reorder_level=10;
SET @sql='';
SELECT @sql=@sql+'ALTER TABLE '+QUOTENAME(SCHEMA_NAME(schema_id))+'.'+QUOTENAME(name)+' WITH CHECK CHECK CONSTRAINT ALL;' FROM sys.tables;
EXEC sys.sp_executesql @sql;
GO
-- Upgrade 010: stock unit conversion, per-unit barcode, receipt source reference, order buyer.
-- Task split 05/11/2026 (Dũng, UC-26/27/39/31); user approved 06/11/2026. Additive only; safe to re-run.
-- Run explicitly: sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/upgrades/010-stock-units-barcode.sql
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;
BEGIN TRANSACTION;

-- The row with factor_to_base = 1 is the product's base unit; every stock quantity is counted in it.
IF OBJECT_ID(N'dbo.product_units', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_units (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_units PRIMARY KEY,
        product_id BIGINT NOT NULL CONSTRAINT FK_product_units_product_id REFERENCES dbo.products(id),
        unit_name NVARCHAR(50) NOT NULL,
        factor_to_base INT NOT NULL CONSTRAINT CK_product_units_factor CHECK (factor_to_base >= 1),
        barcode VARCHAR(64) NULL
    );
    CREATE UNIQUE INDEX UX_product_units_product_unit ON dbo.product_units(product_id, unit_name);
    CREATE UNIQUE INDEX UX_product_units_one_base ON dbo.product_units(product_id) WHERE factor_to_base = 1;
    CREATE UNIQUE INDEX UX_product_units_barcode ON dbo.product_units(barcode) WHERE barcode IS NOT NULL;
END;

INSERT dbo.product_units (product_id, unit_name, factor_to_base)
SELECT p.id, N'Cái', 1
FROM dbo.products p
WHERE NOT EXISTS (SELECT 1 FROM dbo.product_units u WHERE u.product_id = p.id AND u.factor_to_base = 1);

-- Existing movements keep NULL here: their real posting time and source are unknown.
IF COL_LENGTH(N'dbo.stock_movements', N'created_at') IS NULL
    ALTER TABLE dbo.stock_movements ADD created_at DATETIMEOFFSET(0) NULL
        CONSTRAINT DF_stock_movements_created_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.stock_movements', N'entered_unit') IS NULL
    ALTER TABLE dbo.stock_movements ADD entered_unit NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.stock_movements', N'entered_quantity') IS NULL
    ALTER TABLE dbo.stock_movements ADD entered_quantity INT NULL;
IF COL_LENGTH(N'dbo.stock_movements', N'source_ref') IS NULL
    ALTER TABLE dbo.stock_movements ADD source_ref VARCHAR(100) NULL;

IF COL_LENGTH(N'dbo.orders', N'buyer_id') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD buyer_id BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_buyer_id FOREIGN KEY (buyer_id) REFERENCES dbo.users(id);
END;
COMMIT;
GO
-- Separate batch: the column must exist before the index that references it is compiled.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_source_ref' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_source_ref ON dbo.stock_movements(stock_item_id, source_ref) WHERE source_ref IS NOT NULL;
GO

GO
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
    INSERT dbo.product_units (product_id, unit_name, factor_to_base)
    SELECT p.id, v.unit_name, 1
    FROM (VALUES ('DEMO-WATER', N'Chai'), ('DEMO-SHUTTLE', N'Quả'), ('DEMO-PICKLEBALL', N'Quả')) v(sku, unit_name)
    JOIN dbo.products p ON p.sku = v.sku
    WHERE NOT EXISTS (SELECT 1 FROM dbo.product_units x WHERE x.product_id = p.id AND x.factor_to_base = 1);

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

-- Development fixtures for UC-36/UC-37 payment UI, not real stock reservations.
-- Existing demo products and a Staff account are required.
-- Does not simulate receipt of money, handover, or change inventory.
USE [Sep490];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource = N'TKSC-goods-payment-seed-v1',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @lock < 0 THROW 50001, 'Cannot acquire seed lock.', 1;
    DECLARE @staff bigint = (SELECT TOP (1) u.id FROM dbo.users u
        JOIN dbo.roles r ON r.id = u.role_id WHERE r.code = 'Staff'
        ORDER BY CASE WHEN u.email = N'staff@demo.tksc.invalid' THEN 0 ELSE 1 END, u.id);
    IF @staff IS NULL THROW 50002, 'A Staff account is required.', 1;

    DECLARE @lines TABLE (code varchar(100), customer nvarchar(255), sku varchar(100), qty smallint);
    INSERT @lines VALUES
        ('DEMO-GOODS-001', N'Khách mẫu 01', 'DEMO-WATER', 2),
        ('DEMO-GOODS-002', N'Khách mẫu 02', 'DEMO-SHUTTLE', 2),
        ('DEMO-GOODS-002', N'Khách mẫu 02', 'DEMO-WATER', 2),
        ('DEMO-GOODS-003', N'Khách mẫu 03', 'DEMO-PICKLEBALL', 2),
        ('DEMO-GOODS-003', N'Khách mẫu 03', 'DEMO-WATER', 4),
        ('DEMO-GOODS-004', N'Khách mẫu 04', 'DEMO-SHUTTLE', 4),
        ('DEMO-GOODS-004', N'Khách mẫu 04', 'DEMO-PICKLEBALL', 1),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-WATER', 5),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-SHUTTLE', 2),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-PICKLEBALL', 2);
    IF EXISTS (SELECT 1 FROM @lines l WHERE NOT EXISTS
        (SELECT 1 FROM dbo.products p WHERE p.sku = l.sku AND p.is_active = 1 AND p.unit_price_vnd > 0))
        THROW 50003, 'Active demo products with positive prices are required.', 1;

    DECLARE @added TABLE (id bigint, code varchar(100));
    INSERT dbo.orders (order_code, staff_id, customer_name, total_amount, status, payment_state, handover_state)
    OUTPUT inserted.id, inserted.order_code INTO @added
    SELECT l.code, @staff, l.customer, SUM(l.qty * p.unit_price_vnd), 'Cart', 'Unpaid', 'Pending'
    FROM @lines l JOIN dbo.products p ON p.sku = l.sku
    WHERE NOT EXISTS (SELECT 1 FROM dbo.orders o WHERE o.order_code = l.code)
    GROUP BY l.code, l.customer;

    INSERT dbo.order_items (order_id, product_id, quantity, unit_price_vnd, subtotal_vnd)
    SELECT a.id, p.id, l.qty, p.unit_price_vnd, l.qty * p.unit_price_vnd
    FROM @added a JOIN @lines l ON l.code = a.code JOIN dbo.products p ON p.sku = l.sku;
    COMMIT;
    SELECT COUNT(*) AS inserted_orders FROM @added;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;

GO
-- Synthetic business documents in the real local database. No gateway/email calls.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRANSACTION;
DECLARE @venue bigint,@staff bigint,@payer bigint,@player bigint,@court bigint,@session bigint,@order bigint,@payment bigint,@ticket bigint,@product bigint,@refund bigint;
DECLARE @sport bigint=(SELECT TOP(1) id FROM dbo.sports WHERE code='badminton');
DECLARE @slot bigint=(SELECT MIN(id) FROM dbo.time_slots);
IF @sport IS NULL OR @slot IS NULL THROW 50000,'Sports and time slots are required.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Thủ Đức') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Thủ Đức',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Thủ Đức';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc01@example.invalid',N'[DEMO] Staff Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc01@example.invalid',N'[DEMO] Manager Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc01@example.invalid',N'[DEMO] Referee Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc01@example.invalid',N'[DEMO] Player Thủ Đức','Active',NULL FROM dbo.roles WHERE code=N'Player';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-owner-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-owner-sc01@example.invalid',N'[DEMO] Owner Thủ Đức','Active',NULL FROM dbo.roles WHERE code=N'Owner';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc01@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc01@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC01') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC01',N'[DEMO] Combo cầu và nước Thủ Đức','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC01';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,600000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-0',@payer,600000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-0','confirmed',600000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,600000,'cash','paid',N'DEMO-SC-SOC-0-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-0',@staff,N'[DEMO] Khách mẫu',150000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,150000,150000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,150000,'cash','paid',N'DEMO-SC-GOODS-0-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,680000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-0',@payer,680000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-0','confirmed',680000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,680000,'cash','paid',N'DEMO-SC-SOC-1-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-0',@staff,N'[DEMO] Khách mẫu',170000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,170000,170000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,170000,'cash','paid',N'DEMO-SC-GOODS-1-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,760000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-0',@payer,760000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-0','confirmed',760000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,760000,'cash','paid',N'DEMO-SC-SOC-2-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-0',@staff,N'[DEMO] Khách mẫu',190000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,190000,190000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,190000,'cash','paid',N'DEMO-SC-GOODS-2-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,840000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-0',@payer,840000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-0','confirmed',840000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,840000,'cash','paid',N'DEMO-SC-SOC-3-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-0',@staff,N'[DEMO] Khách mẫu',210000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,210000,210000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,210000,'cash','paid',N'DEMO-SC-GOODS-3-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,920000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-0',@payer,920000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-0','confirmed',920000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,920000,'cash','paid',N'DEMO-SC-SOC-4-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-0',@staff,N'[DEMO] Khách mẫu',230000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,230000,230000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,230000,'cash','paid',N'DEMO-SC-GOODS-4-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1000000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-0',@payer,1000000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-0','confirmed',1000000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1000000,'cash','paid',N'DEMO-SC-SOC-5-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-0',@staff,N'[DEMO] Khách mẫu',250000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,250000,250000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,250000,'cash','paid',N'DEMO-SC-GOODS-5-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1080000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-0',@payer,1080000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-0','confirmed',1080000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1080000,'cash','paid',N'DEMO-SC-SOC-6-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-0',@staff,N'[DEMO] Khách mẫu',270000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,270000,270000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,270000,'cash','paid',N'DEMO-SC-GOODS-6-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-CUP-01') BEGIN
INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,N'DEMO-SC-CUP-01',@sport,1); SET @court=SCOPE_IDENTITY();
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,2400000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-CUP-01',@payer,2400000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-CUP-01','confirmed',2400000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,2400000,'cash','paid',N'DEMO-SC-CUP-01',@staff); SET @payment=SCOPE_IDENTITY();
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T08:00:00+07:00',note=N'[DEMO] Giải giao hữu liên cơ sở · phần thu Thủ Đức',event_code=N'CUP-DEMO' WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-0-0';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,80000,'completed',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T10:00:00+07:00',note=N'[DEMO] Hoàn vé đã thu ngày 30/10' WHERE refund_id=@refund;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Bình Thạnh') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Bình Thạnh',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Bình Thạnh';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc02@example.invalid',N'[DEMO] Staff Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc02@example.invalid',N'[DEMO] Manager Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc02@example.invalid',N'[DEMO] Referee Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc02@example.invalid',N'[DEMO] Player Bình Thạnh','Active',NULL FROM dbo.roles WHERE code=N'Player';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc02@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc02@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC02') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC02',N'[DEMO] Combo cầu và nước Bình Thạnh','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC02';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,720000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-1',@payer,720000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-1','confirmed',720000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,720000,'cash','paid',N'DEMO-SC-SOC-0-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-1',@staff,N'[DEMO] Khách mẫu',185000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,185000,185000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,185000,'cash','paid',N'DEMO-SC-GOODS-0-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,800000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-1',@payer,800000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-1','confirmed',800000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,800000,'cash','paid',N'DEMO-SC-SOC-1-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-1',@staff,N'[DEMO] Khách mẫu',205000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,205000,205000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,205000,'cash','paid',N'DEMO-SC-GOODS-1-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,880000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-1',@payer,880000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-1','confirmed',880000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,880000,'cash','paid',N'DEMO-SC-SOC-2-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-1',@staff,N'[DEMO] Khách mẫu',225000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,225000,225000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,225000,'cash','paid',N'DEMO-SC-GOODS-2-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,960000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-1',@payer,960000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-1','confirmed',960000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,960000,'cash','paid',N'DEMO-SC-SOC-3-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-1',@staff,N'[DEMO] Khách mẫu',245000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,245000,245000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,245000,'cash','paid',N'DEMO-SC-GOODS-3-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1040000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-1',@payer,1040000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-1','confirmed',1040000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1040000,'cash','paid',N'DEMO-SC-SOC-4-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-1',@staff,N'[DEMO] Khách mẫu',265000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,265000,265000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,265000,'cash','paid',N'DEMO-SC-GOODS-4-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1120000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-1',@payer,1120000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-1','confirmed',1120000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1120000,'cash','paid',N'DEMO-SC-SOC-5-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-1',@staff,N'[DEMO] Khách mẫu',285000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,285000,285000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,285000,'cash','paid',N'DEMO-SC-GOODS-5-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1200000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-1',@payer,1200000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-1','confirmed',1200000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1200000,'cash','paid',N'DEMO-SC-SOC-6-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-1',@staff,N'[DEMO] Khách mẫu',305000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,305000,305000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,305000,'cash','paid',N'DEMO-SC-GOODS-6-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-PENDING-01') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,320000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-PENDING-01',@payer,320000,'pending'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-PENDING-01','reserved',320000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,320000,'cash','pending',N'DEMO-SC-PENDING-01',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T10:00:00+07:00',note=N'[DEMO] Chưa xác nhận chuyển khoản',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-3-1';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,160000,'completed',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T09:00:00+07:00',note=N'[DEMO] Hoàn hai vé' WHERE refund_id=@refund;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Quận 7') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Quận 7',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Quận 7';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc03@example.invalid',N'[DEMO] Staff Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc03@example.invalid',N'[DEMO] Manager Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc03@example.invalid',N'[DEMO] Referee Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc03@example.invalid',N'[DEMO] Player Quận 7','Active',NULL FROM dbo.roles WHERE code=N'Player';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc03@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc03@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC03') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC03',N'[DEMO] Combo cầu và nước Quận 7','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC03';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,840000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-2',@payer,840000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-2','confirmed',840000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,840000,'cash','paid',N'DEMO-SC-SOC-0-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-2',@staff,N'[DEMO] Khách mẫu',220000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,220000,220000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,220000,'cash','paid',N'DEMO-SC-GOODS-0-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,920000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-2',@payer,920000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-2','confirmed',920000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,920000,'cash','paid',N'DEMO-SC-SOC-1-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-2',@staff,N'[DEMO] Khách mẫu',240000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,240000,240000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,240000,'cash','paid',N'DEMO-SC-GOODS-1-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,1000000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-2',@payer,1000000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-2','confirmed',1000000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1000000,'cash','paid',N'DEMO-SC-SOC-2-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-2',@staff,N'[DEMO] Khách mẫu',260000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,260000,260000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,260000,'cash','paid',N'DEMO-SC-GOODS-2-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,1080000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-2',@payer,1080000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-2','confirmed',1080000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1080000,'cash','paid',N'DEMO-SC-SOC-3-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-2',@staff,N'[DEMO] Khách mẫu',280000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,280000,280000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,280000,'cash','paid',N'DEMO-SC-GOODS-3-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1160000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-2',@payer,1160000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-2','confirmed',1160000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1160000,'cash','paid',N'DEMO-SC-SOC-4-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-2',@staff,N'[DEMO] Khách mẫu',300000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,300000,300000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,300000,'cash','paid',N'DEMO-SC-GOODS-4-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1240000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-2',@payer,1240000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-2','confirmed',1240000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1240000,'cash','paid',N'DEMO-SC-SOC-5-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-2',@staff,N'[DEMO] Khách mẫu',320000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,320000,320000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,320000,'cash','paid',N'DEMO-SC-GOODS-5-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1320000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-2',@payer,1320000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-2','confirmed',1320000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1320000,'cash','paid',N'DEMO-SC-SOC-6-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-2',@staff,N'[DEMO] Khách mẫu',340000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,340000,340000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,340000,'cash','paid',N'DEMO-SC-GOODS-6-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-CUP-02') BEGIN
INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,N'DEMO-SC-CUP-02',@sport,1); SET @court=SCOPE_IDENTITY();
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1600000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-CUP-02',@payer,1600000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-CUP-02','confirmed',1600000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1600000,'cash','paid',N'DEMO-SC-CUP-02',@staff); SET @payment=SCOPE_IDENTITY();
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T08:05:00+07:00',note=N'[DEMO] Giải giao hữu liên cơ sở · phần thu Quận 7',event_code=N'CUP-DEMO' WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-5-2';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,80000,'requested',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T11:00:00+07:00',note=N'[DEMO] Yêu cầu hoàn tiền, chưa chi' WHERE refund_id=@refund;
END;
COMMIT;
SELECT v.name,e.entry_type,SUM(e.amount_vnd) total_vnd FROM dbo.revenue_entries e JOIN dbo.venues v ON v.id=e.venue_id WHERE v.name LIKE '[[]DEMO-SC]%' GROUP BY v.name,e.entry_type ORDER BY v.name,e.entry_type;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';

GO
DECLARE @today date='20261110';
DECLARE @manager bigint=(SELECT MIN(u.id) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Manager' AND u.google_subject IS NOT NULL);
DECLARE @ref bigint=(SELECT MIN(u.id) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Referee' AND u.venue_id=1);
DECLARE @staff bigint=(SELECT MIN(u.id) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Staff' AND u.venue_id=1);
UPDATE u SET venue_id=1 FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code IN ('Manager','Staff','Referee') AND u.venue_id IS NULL;
UPDATE dbo.products SET venue_id=1 WHERE venue_id IS NULL;
UPDATE s SET venue_id=p.venue_id FROM dbo.stock_items s JOIN dbo.products p ON p.id=s.product_id;
UPDATE dbo.orders SET venue_id=1 WHERE venue_id IS NULL;
INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id)
SELECT id,100,10,venue_id FROM dbo.products p WHERE NOT EXISTS(SELECT 1 FROM dbo.stock_items s WHERE s.product_id=p.id);
INSERT dbo.stock_movements(stock_item_id,movement_type,quantity,reference_type,created_by)
SELECT id,'in',quantity_on_hand,'test_reset',@staff FROM dbo.stock_items;
INSERT dbo.player_sport_profiles(player_id,sport_id,rating_status)
SELECT p.id,s.id,'Unrated' FROM dbo.players p CROSS JOIN dbo.sports s WHERE NOT EXISTS(SELECT 1 FROM dbo.player_sport_profiles sp WHERE sp.player_id=p.id AND sp.sport_id=s.id);
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
SELECT c.sport_id,c.id,t.id,DATEADD(day,d.n,@today),NULL,8,CASE WHEN c.sport_id=1 THEN 80000 ELSE 100000 END,'Published',@manager
FROM dbo.courts c CROSS JOIN dbo.time_slots t CROSS JOIN (VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14)) d(n)
WHERE c.venue_id=1 AND c.is_active=1;
INSERT dbo.session_referees(session_id,referee_id,assigned_by)
SELECT s.id,@ref,@manager FROM dbo.sessions s WHERE (s.court_id=1 AND s.time_slot_id IN(1,3,5)) OR (s.court_id=2 AND s.time_slot_id IN(2,4,6));
INSERT dbo.session_staff(session_id,staff_id)
SELECT s.id,@staff FROM dbo.sessions s WHERE s.court_id IN(1,2);
-- Checked-in fixtures today; confirmed and reserved tickets tomorrow for each player.
DECLARE @player bigint,@user bigint,@session bigint,@order bigint,@ticket bigint,@i int,@price int;
DECLARE pc CURSOR LOCAL FAST_FORWARD FOR SELECT p.id,p.user_id FROM dbo.players p JOIN dbo.users u ON u.id=p.user_id WHERE u.status='Active' AND u.deleted_at IS NULL AND (u.google_subject IS NOT NULL OR u.email LIKE 'player0%@demo.tksc.invalid');
OPEN pc; FETCH NEXT FROM pc INTO @player,@user;
DECLARE @n int=0;
WHILE @@FETCH_STATUS=0
BEGIN
 SET @i=0;
 WHILE @i<3
 BEGIN
  SELECT @session=s.id,@price=s.price_vnd FROM dbo.sessions s WHERE s.court_id=CASE WHEN @n<7 THEN 1 ELSE 2 END AND s.session_date=DATEADD(day,CASE WHEN @i=0 THEN 0 ELSE 1 END,@today) AND s.time_slot_id=CASE WHEN @n<7 THEN 1+@i*2 ELSE 2+@i*2 END;
  INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(CONCAT('RESET-',@player,'-',@i),@user,@price,CASE WHEN @i=2 THEN 'pending' ELSE 'paid' END); SET @order=SCOPE_IDENTITY();
  INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd,checked_in_by) VALUES(@order,@session,@player,CONCAT('RESET-T-',@player,'-',@i),CASE WHEN @i=0 THEN 'checkedin' WHEN @i=1 THEN 'confirmed' ELSE 'reserved' END,@price,CASE WHEN @i=0 THEN @ref END);
  INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@user,@price,'cash',CASE WHEN @i=2 THEN 'pending' ELSE 'paid' END,CONCAT('RESET-P-',@player,'-',@i),CASE WHEN @i<>2 THEN @staff END);
  SET @i+=1;
 END;
 SET @n+=1;
 FETCH NEXT FROM pc INTO @player,@user;
END;
CLOSE pc; DEALLOCATE pc;
-- Published self-surveys for two synthetic players; real players can test surveys from Unrated.
INSERT dbo.assessments(player_sport_profile_id,rubric_id,assessment_type,assessor_id,status,total_score,band_result)
SELECT sp.id,r.id,'SelfSurvey',p.user_id,'Published',50,'B3' FROM dbo.player_sport_profiles sp JOIN dbo.players p ON p.id=sp.player_id JOIN dbo.users u ON u.id=p.user_id JOIN dbo.rubrics r ON r.sport_id=sp.sport_id AND r.is_active=1 WHERE u.email IN ('player01@demo.tksc.invalid','player02@demo.tksc.invalid');
INSERT dbo.assessment_scores(assessment_id,dimension_id,score) SELECT a.id,d.id,2 FROM dbo.assessments a JOIN dbo.rubric_dimensions d ON d.rubric_id=a.rubric_id;
INSERT dbo.player_rating_history(player_sport_profile_id,new_score,new_band,changed_by,assessment_id) SELECT player_sport_profile_id,50,'B3',assessor_id,id FROM dbo.assessments;
UPDATE sp SET current_score=50,current_band='B3',rating_status='Provisional' FROM dbo.player_sport_profiles sp JOIN dbo.assessments a ON a.player_sport_profile_id=sp.id;
IF EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id WHERE t.status IN('confirmed','checkedin','reserved') GROUP BY s.id,s.capacity HAVING COUNT(*)>s.capacity) THROW 51000,'Fixture exceeds capacity',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51000,'Disabled or untrusted FK',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
COMMIT;
SELECT 'RESET SUCCESS' result;




