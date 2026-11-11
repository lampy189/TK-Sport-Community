-- SQLCMD mode. Run from repository root ONLY AFTER explicit rollout approval.
-- All schema/data changes share one outer transaction across the included batches.
-- On any error sqlcmd -b closes the connection and rolls back the outer transaction.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=1 AND name IN (N'TK Sport Community Center',N'TKSC Hà Nội'))
 OR NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=2 AND name IN (N'TKSC Demo',N'TKSC Vĩnh Phúc'))
 THROW 50000,'Venue mapping differs from the reviewed Hanoi/Vinh Phuc plan.',1;
BEGIN TRANSACTION;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
CREATE TABLE #rollout_counts(users_count bigint,sessions_count bigint,tickets_count bigint);
INSERT #rollout_counts SELECT (SELECT COUNT_BIG(*) FROM dbo.users),(SELECT COUNT_BIG(*) FROM dbo.sessions),(SELECT COUNT_BIG(*) FROM dbo.tickets);
GO
:r db/upgrades/010-sport-center-accounting.sql
GO
:r db/upgrades/011-sport-center-security.sql
GO
:r db/upgrades/012-revenue-posting.sql
GO
:r db/upgrades/010-stock-units-barcode.sql
GO
:r db/upgrades/013-sport-center-catalog.sql
GO
:r db/upgrades/015-hanoi-vinh-phuc-rollout.sql
GO
:r db/upgrades/014-late-entry-sales.sql
GO
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
IF EXISTS(SELECT 1 FROM #rollout_counts WHERE users_count<>(SELECT COUNT_BIG(*) FROM dbo.users)
 OR sessions_count<>(SELECT COUNT_BIG(*) FROM dbo.sessions) OR tickets_count<>(SELECT COUNT_BIG(*) FROM dbo.tickets))
 THROW 50000,'Unexpected account/session/ticket count change.',1;
IF NOT EXISTS(SELECT 1 FROM sys.security_policies WHERE name='SportCenterIsolation' AND is_enabled=1)
 THROW 50000,'Center isolation policy is not active.',1;
IF OBJECT_ID('dbo.late_entry_tickets','U') IS NULL OR OBJECT_ID('dbo.late_entry_offers','U') IS NULL
 THROW 50000,'Late-entry tables missing.',1;
COMMIT;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
PRINT 'Applied Hanoi/Vinh Phuc schema and reviewed legacy mapping. Accounts/sessions/tickets preserved.';
