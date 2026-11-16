-- Upgrade 020: referee supply requests and stock counts with an approved adjustment (task split 13/11/2026, Dũng;
-- RP3 UC-41..UC-44). Replaces the unmerged draft 017-stock-reservation-supply-count: goods holds now use
-- dbo.goods_stock_reservations from upgrade 018 (available = on hand - Committed - unexpired Held), so this upgrade
-- adds no reservation column. Additive only (one new table, new columns, one index); no data is deleted. Safe to re-run.
-- Requires upgrade 018. Review and approve before applying, e.g.:
--   sqlcmd -S localhost -d Sep490 -E -C -b -f 65001 -i db/upgrades/020-supply-requests-stock-counts.sql
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() NOT LIKE N'Sep490%' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;
IF OBJECT_ID(N'dbo.goods_stock_reservations', N'U') IS NULL THROW 50000, 'Apply upgrade 018-goods-stock-reservations first.', 1;
BEGIN TRANSACTION;

-- Referee (or Manager) asks for supplies; Staff issues or rejects. Quantities are in the base unit.
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

-- Stock count: the count never changes stock; a separately approved adjustment is applied once.
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
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stock_counts_status')
    ALTER TABLE dbo.stock_counts ADD CONSTRAINT CK_stock_counts_status CHECK (status IN ('NoDifference', 'Pending', 'Applied', 'Rejected'));
-- Database-level "exactly once": one adjustment movement per approved stock count.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_stock_count' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_stock_count ON dbo.stock_movements(reference_id) WHERE reference_type = 'StockCount';
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
