-- Additive upgrade. Review and explicitly approve before applying to Sep490.
-- No seed accounts, orders, payments or tickets are created by this script.
SET XACT_ABORT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490 before applying this upgrade.',1;
-- Legacy single-operator databases need only these two additive tables.
-- Existing multi-center databases must keep their original security/accounting requirements.
IF (COL_LENGTH('dbo.users','venue_id') IS NOT NULL
 OR EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation'))
 AND (COL_LENGTH('dbo.users','venue_id') IS NULL OR OBJECT_ID('dbo.revenue_entries','U') IS NULL
 OR NOT EXISTS(SELECT 1 FROM sys.triggers WHERE object_id=OBJECT_ID('dbo.TR_payments_revenue') AND is_disabled=0)
 OR NOT EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation' AND is_enabled=1))
 THROW 50000,'Apply reviewed Sport Center accounting/security/revenue upgrades 010-012 first.',1;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.late_entry_offers','U') IS NULL
BEGIN
 CREATE TABLE dbo.late_entry_offers(
  session_id BIGINT NOT NULL CONSTRAINT PK_late_entry_offers PRIMARY KEY,
  max_tickets INT NOT NULL, price_vnd INT NOT NULL, is_open BIT NOT NULL,
  revision BIGINT NOT NULL, updated_by BIGINT NOT NULL, updated_at DATETIMEOFFSET NOT NULL,
  CONSTRAINT FK_late_offer_session FOREIGN KEY(session_id) REFERENCES dbo.sessions(id),
  CONSTRAINT FK_late_offer_actor FOREIGN KEY(updated_by) REFERENCES dbo.users(id),
  CONSTRAINT CK_late_offer_quantity CHECK(max_tickets BETWEEN 1 AND 8),
  CONSTRAINT CK_late_offer_price CHECK(price_vnd BETWEEN 1000 AND 100000000),
  CONSTRAINT CK_late_offer_revision CHECK(revision>0)
 );
END;
IF OBJECT_ID('dbo.late_entry_tickets','U') IS NULL
BEGIN
 CREATE TABLE dbo.late_entry_tickets(
  ticket_id BIGINT NOT NULL CONSTRAINT PK_late_entry_tickets PRIMARY KEY,
  session_id BIGINT NOT NULL, offer_revision BIGINT NOT NULL,
  starts_at DATETIMEOFFSET NOT NULL, ends_at DATETIMEOFFSET NOT NULL,
  purchased_at DATETIMEOFFSET NOT NULL,
  CONSTRAINT FK_late_ticket_ticket FOREIGN KEY(ticket_id) REFERENCES dbo.tickets(id),
  CONSTRAINT FK_late_ticket_offer FOREIGN KEY(session_id) REFERENCES dbo.late_entry_offers(session_id),
  CONSTRAINT CK_late_ticket_interval CHECK(ends_at>starts_at AND purchased_at>=starts_at AND purchased_at<ends_at)
 );
 CREATE INDEX IX_late_ticket_session ON dbo.late_entry_tickets(session_id,ticket_id);
END;
COMMIT;
