-- Integration checks against the real SQL policy. All test writes roll back.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
DECLARE @a bigint=(SELECT venue_id FROM dbo.users WHERE email='demo-staff-sc01@example.invalid');
DECLARE @b bigint=(SELECT venue_id FROM dbo.users WHERE email='demo-staff-sc02@example.invalid');
DECLARE @actor bigint=(SELECT id FROM dbo.users WHERE email='demo-staff-sc01@example.invalid');
DECLARE @foreignOrder bigint=(SELECT MIN(id) FROM dbo.orders WHERE venue_id=@b);
DECLARE @foreignProduct bigint=(SELECT MIN(id) FROM dbo.products WHERE venue_id=@b);
DECLARE @before bigint=(SELECT COUNT_BIG(*) FROM dbo.revenue_entries);
IF @a IS NULL OR @b IS NULL THROW 51000,'Seed fixtures first.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Staff';
EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=@a;
IF EXISTS(SELECT 1 FROM dbo.orders WHERE venue_id<>@a OR venue_id IS NULL) THROW 51000,'Order isolation failed.',1;
IF EXISTS(SELECT 1 FROM dbo.products WHERE id=@foreignProduct) THROW 51000,'Catalog isolation failed.',1;
IF EXISTS(SELECT 1 FROM dbo.revenue_entries WHERE venue_id<>@a) THROW 51000,'Revenue isolation failed.',1;
IF EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code LIKE 'DEMO-SC-GOODS-%-1') THROW 51000,'Payment isolation failed.',1;
BEGIN TRANSACTION;
UPDATE dbo.orders SET customer_name=N'FORBIDDEN' WHERE id=@foreignOrder;
IF @@ROWCOUNT<>0 THROW 51000,'Cross-center update succeeded.',1;
ROLLBACK;
BEGIN TRY
 BEGIN TRANSACTION;
 INSERT dbo.orders(order_code,staff_id,total_amount,status,venue_id) VALUES('SC-SECURITY-ROLLBACK',@actor,100,'Cart',@b);
 ROLLBACK;
 THROW 51000,'Cross-center insert succeeded.',1;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 IF ERROR_NUMBER()<>33504 THROW;
END CATCH;
-- A new confirmed receipt is posted once, and its source update is atomic.
BEGIN TRANSACTION;
INSERT dbo.orders(order_code,staff_id,total_amount,status,venue_id) VALUES('SC-POSTING-ROLLBACK',@actor,100,'Cart',@a);
DECLARE @order bigint=SCOPE_IDENTITY();
INSERT dbo.payments(goods_order_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,100,'cash','pending','SC-POSTING-ROLLBACK',@actor);
DECLARE @payment bigint=SCOPE_IDENTITY();
IF EXISTS(SELECT 1 FROM dbo.revenue_entries WHERE payment_id=@payment) THROW 51000,'Pending payment posted.',1;
UPDATE dbo.payments SET status='paid' WHERE id=@payment;
UPDATE dbo.payments SET status='paid' WHERE id=@payment;
IF (SELECT COUNT(*) FROM dbo.revenue_entries WHERE payment_id=@payment)<>1 THROW 51000,'Receipt is missing or duplicated.',1;
IF (SELECT SUM(amount_vnd) FROM dbo.revenue_entries WHERE payment_id=@payment)<>100 THROW 51000,'Wrong posted amount.',1;
ROLLBACK;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'SystemAdmin';
IF EXISTS(SELECT 1 FROM dbo.revenue_entries) THROW 51000,'System Admin leaked business revenue.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'Owner';
IF (SELECT COUNT_BIG(*) FROM dbo.revenue_entries)<>@before THROW 51000,'Owner scope or rollback failed.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL;
PRINT 'PASS: SQL read isolation, update/insert guards, atomic receipt, retry deduplication, Owner and SystemAdmin scopes.';
