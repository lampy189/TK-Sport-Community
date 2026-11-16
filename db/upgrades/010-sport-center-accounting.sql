SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
-- Approved by the user on 2026-11-05: implement the requested real database feature.
-- Additive foundation only; deploy scoped API/SQL-store changes before enabling multi-center access.
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select Sep490 before applying this upgrade.', 1;

BEGIN TRY
 BEGIN TRANSACTION;
 IF COL_LENGTH('dbo.users', 'venue_id') IS NULL
 BEGIN
  ALTER TABLE dbo.users ADD venue_id bigint NULL;
  ALTER TABLE dbo.users ADD CONSTRAINT FK_users_venue FOREIGN KEY (venue_id) REFERENCES dbo.venues(id);
 END;
 IF COL_LENGTH('dbo.orders', 'venue_id') IS NULL
 BEGIN
  ALTER TABLE dbo.orders ADD venue_id bigint NULL;
  ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_venue FOREIGN KEY (venue_id) REFERENCES dbo.venues(id);
 END;
 IF COL_LENGTH('dbo.stock_items', 'venue_id') IS NULL
 BEGIN
  ALTER TABLE dbo.stock_items ADD venue_id bigint NULL;
  ALTER TABLE dbo.stock_items ADD CONSTRAINT FK_stock_items_venue FOREIGN KEY (venue_id) REFERENCES dbo.venues(id);
 END;
 IF COL_LENGTH('dbo.shift_reconciliations', 'venue_id') IS NULL
 BEGIN
  ALTER TABLE dbo.shift_reconciliations ADD venue_id bigint NULL;
  ALTER TABLE dbo.shift_reconciliations ADD CONSTRAINT FK_shift_reconciliations_venue FOREIGN KEY (venue_id) REFERENCES dbo.venues(id);
 END;

 -- Keep the existing product-only stock unique index during this additive stage.
 -- Its replacement and backfill require the scoped stock implementation and a separate migration.
 -- Unassigned historical rows are not silently attributed to any demo center.
 IF OBJECT_ID(N'dbo.revenue_entries', N'U') IS NULL
 BEGIN
  CREATE TABLE dbo.revenue_entries (
   id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_revenue_entries PRIMARY KEY,
   venue_id bigint NOT NULL CONSTRAINT FK_revenue_entries_venue REFERENCES dbo.venues(id),
   payment_id bigint NOT NULL CONSTRAINT FK_revenue_entries_payment REFERENCES dbo.payments(id),
   refund_id bigint NULL CONSTRAINT FK_revenue_entries_refund REFERENCES dbo.refunds(id),
   entry_type varchar(30) NOT NULL,
   amount_vnd bigint NOT NULL,
   recognized_at datetimeoffset(0) NOT NULL,
   recorded_at datetimeoffset(0) NOT NULL CONSTRAINT DF_revenue_entries_recorded DEFAULT SYSDATETIMEOFFSET(),
   recorded_by bigint NULL CONSTRAINT FK_revenue_entries_actor REFERENCES dbo.users(id),
   event_code varchar(100) NULL,
   note nvarchar(500) NULL,
   CONSTRAINT CK_revenue_entries_amount CHECK (amount_vnd > 0),
   CONSTRAINT CK_revenue_entries_type CHECK (
    (entry_type IN ('social_receipt','goods_receipt') AND refund_id IS NULL)
    OR (entry_type = 'refund' AND refund_id IS NOT NULL))
  );
  CREATE UNIQUE INDEX UX_revenue_entries_receipt ON dbo.revenue_entries(payment_id,venue_id)
   WHERE refund_id IS NULL;
  CREATE UNIQUE INDEX UX_revenue_entries_refund ON dbo.revenue_entries(refund_id)
   WHERE refund_id IS NOT NULL;
  CREATE INDEX IX_revenue_entries_venue_date ON dbo.revenue_entries(venue_id,recognized_at)
   INCLUDE (entry_type,amount_vnd,payment_id);
 END;
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK;
 THROW;
END CATCH;
