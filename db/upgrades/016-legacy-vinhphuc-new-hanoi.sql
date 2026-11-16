-- User correction 10 Nov 2026: ALL pre-rollout business data belongs to Vinh Phuc.
-- Snapshot boundaries verified before this migration: legacy user IDs <=10041, courts 1..9,
-- products 1..4, goods orders 1..5, payments 1..21. Do not guess newer records.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=1 AND name=N'TKSC Hà Nội')
  OR NOT EXISTS(SELECT 1 FROM dbo.venues WHERE id=2 AND name=N'TKSC Vĩnh Phúc')
  THROW 50000,'Review venue mapping before moving legacy data.',1;
 UPDATE dbo.courts SET venue_id=2 WHERE id BETWEEN 1 AND 9;
 UPDATE dbo.products SET venue_id=2 WHERE id BETWEEN 1 AND 4;
 UPDATE dbo.stock_items SET venue_id=2 WHERE product_id BETWEEN 1 AND 4;
 UPDATE dbo.orders SET venue_id=2 WHERE id BETWEEN 1 AND 5;
 -- No legacy shift reconciliation rows existed in the verified snapshot.
 UPDATE dbo.revenue_entries SET venue_id=2 WHERE payment_id BETWEEN 1 AND 21;
 UPDATE u SET venue_id=2 FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
 WHERE u.id BETWEEN 1 AND 10041 AND r.code IN ('Staff','Manager','Referee');
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
 THROW;
END CATCH;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
