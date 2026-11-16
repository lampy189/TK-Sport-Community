-- REVIEW BEFORE RUNNING: concrete legacy-data mapping for the Hanoi/Vinh Phuc rollout.
-- Requires reviewed 010-sport-center-accounting, 011-security, 012-revenue, 013-catalog.
-- Final user decision: all legacy business data belongs to Vinh Phuc; Hanoi is new.
-- Does not create accounts, tickets, payments, sessions, stock quantities or fake revenue.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
IF COL_LENGTH('dbo.products','venue_id') IS NULL OR COL_LENGTH('dbo.users','venue_id') IS NULL
 THROW 50000,'Apply the reviewed 010-013 scripts first.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @hanoi bigint=1, @vinhphuc bigint=2;
 IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=@hanoi AND name IN (N'TK Sport Community Center',N'TKSC Hà Nội'))
  OR NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=@vinhphuc AND name IN (N'TKSC Demo',N'TKSC Vĩnh Phúc'))
  THROW 50000,'Legacy venue mapping changed; review IDs before applying.',1;
 UPDATE dbo.venues SET name=N'TKSC Hà Nội',address=N'Hà Nội' WHERE id=@hanoi;
 UPDATE dbo.venues SET name=N'TKSC Vĩnh Phúc',address=N'Vĩnh Phúc' WHERE id=@vinhphuc;
 -- All nine legacy courts (and their sessions/tickets by relationship) belong to Vinh Phuc.
 UPDATE dbo.courts SET venue_id=@vinhphuc WHERE id BETWEEN 1 AND 9;
 UPDATE dbo.products SET venue_id=@vinhphuc WHERE venue_id IS NULL;
 UPDATE s SET venue_id=p.venue_id FROM dbo.stock_items s JOIN dbo.products p ON p.id=s.product_id WHERE s.venue_id IS NULL;
 UPDATE dbo.orders SET venue_id=@vinhphuc WHERE venue_id IS NULL;
 UPDATE dbo.shift_reconciliations SET venue_id=@vinhphuc WHERE venue_id IS NULL;
 UPDATE u SET venue_id=@vinhphuc FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
 WHERE u.id<=10041 AND u.venue_id IS NULL AND r.code IN ('Staff','Manager','Referee');
 -- Existing personnel belong to Vinh Phuc per user instruction; Owner can reassign later.
 -- Player account/profile/history remains global; selected browsing center lives in the signed session.
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
 THROW;
END CATCH;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
