-- OWN-01 / RP3 policy and price versions. Pending explicit database approval.
-- Additive: existing prices remain the baseline; no sold amounts are rewritten.
SET XACT_ABORT ON;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select Sep490.', 1;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.owner_policy_versions','U') IS NULL
BEGIN
 CREATE TABLE dbo.owner_policy_versions(
  id bigint IDENTITY PRIMARY KEY, code varchar(100) NOT NULL, version int NOT NULL,
  name nvarchar(255) NOT NULL, value nvarchar(max) NOT NULL,
  kind varchar(20) NOT NULL, target_id bigint NULL, price_vnd int NULL,
  venue_id bigint NOT NULL REFERENCES dbo.venues(id),
  status varchar(20) NOT NULL DEFAULT 'submitted',
  effective_from datetimeoffset NOT NULL, effective_to datetimeoffset NULL,
  submitted_by bigint NOT NULL REFERENCES dbo.users(id), submitted_at datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET(),
  submit_reason nvarchar(1000) NOT NULL,
  decided_by bigint NULL REFERENCES dbo.users(id), decided_at datetimeoffset NULL, decision_reason nvarchar(1000) NULL,
  CONSTRAINT UX_owner_policy_version UNIQUE(venue_id,code,version),
  CONSTRAINT CK_owner_policy_dates CHECK(effective_to IS NULL OR effective_to>effective_from),
  CONSTRAINT CK_owner_policy_target CHECK((kind='policy' AND target_id IS NULL AND price_vnd IS NULL) OR (kind IN ('product','session') AND target_id>0 AND price_vnd BETWEEN 0 AND 100000000)),
  CONSTRAINT CK_owner_policy_decision CHECK((status='submitted' AND decided_by IS NULL AND decided_at IS NULL) OR (status IN ('approved','rejected') AND decided_by IS NOT NULL AND decided_by<>submitted_by AND decided_at IS NOT NULL AND decision_reason IS NOT NULL))
 );
 CREATE INDEX IX_owner_policy_price ON dbo.owner_policy_versions(kind,target_id,status,effective_from) INCLUDE(price_vnd,effective_to);
END;
IF COL_LENGTH('dbo.tickets','price_version_id') IS NULL
 ALTER TABLE dbo.tickets ADD price_version_id bigint NULL CONSTRAINT FK_tickets_price_version REFERENCES dbo.owner_policy_versions(id);
IF COL_LENGTH('dbo.order_items','price_version_id') IS NULL
 ALTER TABLE dbo.order_items ADD price_version_id bigint NULL CONSTRAINT FK_order_items_price_version REFERENCES dbo.owner_policy_versions(id);
IF COL_LENGTH('dbo.products','requires_price_approval') IS NULL
 ALTER TABLE dbo.products ADD requires_price_approval bit NOT NULL CONSTRAINT DF_products_price_approval DEFAULT 0;
COMMIT;
GO
CREATE OR ALTER FUNCTION dbo.OwnerEffectivePrice(@kind varchar(20), @target bigint, @baseline int, @at datetimeoffset)
RETURNS int AS
BEGIN
 DECLARE @price int;
 SELECT TOP(1) @price=price_vnd FROM dbo.owner_policy_versions
 WHERE kind=@kind AND target_id=@target AND status='approved'
 AND effective_from<=@at AND (effective_to IS NULL OR effective_to>@at)
 ORDER BY effective_from DESC,id DESC;
 RETURN COALESCE(@price,@baseline);
END;
GO
CREATE OR ALTER FUNCTION dbo.OwnerPriceVersion(@kind varchar(20), @target bigint, @at datetimeoffset)
RETURNS bigint AS
BEGIN
 DECLARE @id bigint;
 SELECT TOP(1) @id=id FROM dbo.owner_policy_versions
 WHERE kind=@kind AND target_id=@target AND status='approved'
 AND effective_from<=@at AND (effective_to IS NULL OR effective_to>@at)
 ORDER BY effective_from DESC,id DESC;
 RETURN @id;
END;
GO
