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
