-- Upgrade 017: stock reservations for unpaid goods orders, referee supply requests, stock counts with approval.
-- Task split 13/11/2026 (Dũng: goods and supply lifecycle; RP3 UC-36..UC-38, UC-41..UC-44, JOB-04).
-- User approved 13/11/2026. Additive only (new columns, one new table, indexes); no data is deleted. Safe to re-run.
-- Run explicitly against the target database, e.g.:
--   sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/upgrades/017-stock-reservation-supply-count.sql
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE N'Sep490%' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;
BEGIN TRANSACTION;

-- 1. Available to sell = quantity_on_hand - reserved_quantity. Unpaid and paid-but-not-handed-over orders hold stock here.
IF COL_LENGTH(N'dbo.stock_items', N'reserved_quantity') IS NULL
    ALTER TABLE dbo.stock_items ADD reserved_quantity INT NOT NULL
        CONSTRAINT DF_stock_items_reserved_quantity DEFAULT 0;

-- 2. Goods order hold, cancellation and handover history. Orders created before this upgrade keep
--    reservation_state NULL: they never reserved stock, so nothing is released or consumed for them.
IF COL_LENGTH(N'dbo.orders', N'created_at') IS NULL
    ALTER TABLE dbo.orders ADD created_at DATETIMEOFFSET(0) NULL CONSTRAINT DF_orders_created_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.orders', N'expires_at') IS NULL
    ALTER TABLE dbo.orders ADD expires_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.orders', N'reservation_state') IS NULL
    ALTER TABLE dbo.orders ADD reservation_state VARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.orders', N'cancelled_by') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD cancelled_by BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES dbo.users(id);
END;
IF COL_LENGTH(N'dbo.orders', N'cancelled_at') IS NULL
    ALTER TABLE dbo.orders ADD cancelled_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.orders', N'delivered_by') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD delivered_by BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_delivered_by FOREIGN KEY (delivered_by) REFERENCES dbo.users(id);
END;
IF COL_LENGTH(N'dbo.orders', N'delivered_at') IS NULL
    ALTER TABLE dbo.orders ADD delivered_at DATETIMEOFFSET(0) NULL;

-- 3a. Referee (or Manager) asks for supplies; Staff issues or rejects. Quantities are in the base unit.
IF OBJECT_ID(N'dbo.session_supply_requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.session_supply_requests (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_session_supply_requests PRIMARY KEY,
        session_id BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_session_id REFERENCES dbo.sessions(id),
        product_id BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_product_id REFERENCES dbo.products(id),
        requested_by BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_requested_by REFERENCES dbo.users(id),
        quantity INT NOT NULL CONSTRAINT CK_session_supply_requests_quantity CHECK (quantity > 0),
        entered_unit NVARCHAR(50) NULL,
        entered_quantity INT NULL,
        note NVARCHAR(300) NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_session_supply_requests_status DEFAULT 'Pending'
            CONSTRAINT CK_session_supply_requests_status CHECK (status IN ('Pending', 'Issued', 'Rejected', 'Cancelled')),
        issued_quantity INT NULL,
        handled_by BIGINT NULL CONSTRAINT FK_session_supply_requests_handled_by REFERENCES dbo.users(id),
        handled_at DATETIMEOFFSET(0) NULL,
        decision_note NVARCHAR(300) NULL,
        created_at DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_session_supply_requests_created_at DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE INDEX IX_session_supply_requests_session ON dbo.session_supply_requests(session_id, status);
END;

-- 3b. Stock count: the count never changes stock; a separately approved adjustment is applied once.
IF COL_LENGTH(N'dbo.stock_counts', N'status') IS NULL
    ALTER TABLE dbo.stock_counts ADD status VARCHAR(20) NOT NULL CONSTRAINT DF_stock_counts_status DEFAULT 'Pending';
IF COL_LENGTH(N'dbo.stock_counts', N'reason') IS NULL
    ALTER TABLE dbo.stock_counts ADD reason NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'evidence') IS NULL
    ALTER TABLE dbo.stock_counts ADD evidence NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'entered_unit') IS NULL
    ALTER TABLE dbo.stock_counts ADD entered_unit NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'entered_quantity') IS NULL
    ALTER TABLE dbo.stock_counts ADD entered_quantity INT NULL;
-- Last stock movement id seen when counting: the "data version" of RP3 UC-43.
IF COL_LENGTH(N'dbo.stock_counts', N'stock_version') IS NULL
    ALTER TABLE dbo.stock_counts ADD stock_version BIGINT NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'counted_at') IS NULL
    ALTER TABLE dbo.stock_counts ADD counted_at DATETIMEOFFSET(0) NULL CONSTRAINT DF_stock_counts_counted_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.stock_counts', N'decided_at') IS NULL
    ALTER TABLE dbo.stock_counts ADD decided_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'decision_note') IS NULL
    ALTER TABLE dbo.stock_counts ADD decision_note NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'applied_movement_id') IS NULL
BEGIN
    ALTER TABLE dbo.stock_counts ADD applied_movement_id BIGINT NULL;
    ALTER TABLE dbo.stock_counts ADD CONSTRAINT FK_stock_counts_applied_movement_id FOREIGN KEY (applied_movement_id) REFERENCES dbo.stock_movements(id);
END;
COMMIT;
GO
-- Separate batch: constraints and filtered indexes compile after the new columns exist.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stock_items_reserved')
    ALTER TABLE dbo.stock_items ADD CONSTRAINT CK_stock_items_reserved CHECK (reserved_quantity >= 0 AND reserved_quantity <= quantity_on_hand);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_reservation_state')
    ALTER TABLE dbo.orders ADD CONSTRAINT CK_orders_reservation_state CHECK (reservation_state IS NULL OR reservation_state IN ('Held', 'Released', 'Consumed'));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stock_counts_status')
    ALTER TABLE dbo.stock_counts ADD CONSTRAINT CK_stock_counts_status CHECK (status IN ('NoDifference', 'Pending', 'Applied', 'Rejected'));
-- The expiry job looks for held, unpaid orders past their expiry.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_orders_reservation_expiry' AND object_id = OBJECT_ID(N'dbo.orders'))
    CREATE INDEX IX_orders_reservation_expiry ON dbo.orders(expires_at) WHERE reservation_state = 'Held';
-- Database-level "exactly once": one adjustment movement per stock count, one sale-out per order and stock item.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_stock_count' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_stock_count ON dbo.stock_movements(reference_id) WHERE reference_type = 'StockCount';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_sale_out' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_sale_out ON dbo.stock_movements(stock_item_id, reference_id) WHERE reference_type = 'GoodsOrder' AND movement_type = 'SaleOut';
COMMIT;
GO
-- Paid orders still waiting for handover must keep their goods: hold them now when stock allows.
-- Unpaid legacy carts are left without a hold; they reserve when payment starts.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @orderId BIGINT = 0;
WHILE 1 = 1
BEGIN
    SELECT TOP (1) @orderId = o.id FROM dbo.orders o
    WHERE o.id > @orderId AND o.reservation_state IS NULL AND o.payment_state = 'Paid'
      AND ISNULL(o.handover_state, 'Pending') <> 'Received'
    ORDER BY o.id;
    IF @@ROWCOUNT = 0 BREAK;
    -- One order at a time, so earlier holds count against later ones.
    IF NOT EXISTS (
        SELECT 1 FROM (SELECT product_id, SUM(quantity) AS qty FROM dbo.order_items WHERE order_id = @orderId GROUP BY product_id) need
        LEFT JOIN dbo.stock_items si ON si.product_id = need.product_id
        WHERE si.id IS NULL OR si.quantity_on_hand - si.reserved_quantity < need.qty)
    BEGIN
        UPDATE si SET reserved_quantity = si.reserved_quantity + need.qty
        FROM dbo.stock_items si
        JOIN (SELECT product_id, SUM(quantity) AS qty FROM dbo.order_items WHERE order_id = @orderId GROUP BY product_id) need
          ON need.product_id = si.product_id;
        UPDATE dbo.orders SET reservation_state = 'Held' WHERE id = @orderId;
    END;
END;
COMMIT;
GO
-- Multi-center databases (upgrade 011): supply requests follow their session's center.
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'SportCenterIsolation')
   AND NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE target_object_id = OBJECT_ID(N'dbo.session_supply_requests'))
    EXEC(N'ALTER SECURITY POLICY dbo.SportCenterIsolation
        ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests,
        ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests AFTER INSERT,
        ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests AFTER UPDATE;');
GO
