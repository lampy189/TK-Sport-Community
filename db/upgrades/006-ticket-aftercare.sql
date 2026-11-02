SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
-- UC-14..17 per user scope 2026-10-30. Additive only; apply only after approval.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.ticket_changes','U') IS NULL
BEGIN
 CREATE TABLE dbo.ticket_changes (
  id BIGINT IDENTITY PRIMARY KEY,
  ticket_id BIGINT NOT NULL REFERENCES dbo.tickets(id),
  target_session_id BIGINT NOT NULL REFERENCES dbo.sessions(id),
  old_price_vnd INT NOT NULL CHECK(old_price_vnd>=0),
  new_price_vnd INT NOT NULL CHECK(new_price_vnd>=0),
  status VARCHAR(30) NOT NULL,
  requested_by BIGINT NOT NULL REFERENCES dbo.users(id),
  processed_by BIGINT NULL REFERENCES dbo.users(id),
  new_ticket_id BIGINT NULL REFERENCES dbo.tickets(id),
  created_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
  completed_at DATETIMEOFFSET NULL,
  CONSTRAINT CK_ticket_changes_status CHECK(status IN ('requested','completed','cancelled'))
 );
 CREATE UNIQUE INDEX UX_ticket_changes_pending ON dbo.ticket_changes(ticket_id) WHERE status='requested';
END;
IF OBJECT_ID('dbo.ticket_funding','U') IS NULL
BEGIN
 CREATE TABLE dbo.ticket_funding (
  id BIGINT IDENTITY PRIMARY KEY,
  ticket_id BIGINT NOT NULL REFERENCES dbo.tickets(id),
  payment_id BIGINT NOT NULL REFERENCES dbo.payments(id),
  amount_vnd INT NOT NULL CHECK(amount_vnd>0),
  retired BIT NOT NULL DEFAULT 0,
  CONSTRAINT UX_ticket_funding UNIQUE(ticket_id,payment_id)
 );
END;
COMMIT;
