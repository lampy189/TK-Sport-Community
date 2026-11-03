-- Development fixtures for UC-36/UC-37 payment UI, not real stock reservations.
-- Existing demo products and a Staff account are required.
-- Does not simulate receipt of money, handover, or change inventory.
USE [Sep490];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource = N'TKSC-goods-payment-seed-v1',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @lock < 0 THROW 50001, 'Cannot acquire seed lock.', 1;
    DECLARE @staff bigint = (SELECT TOP (1) u.id FROM dbo.users u
        JOIN dbo.roles r ON r.id = u.role_id WHERE r.code = 'Staff'
        ORDER BY CASE WHEN u.email = N'staff@demo.tksc.invalid' THEN 0 ELSE 1 END, u.id);
    IF @staff IS NULL THROW 50002, 'A Staff account is required.', 1;

    DECLARE @lines TABLE (code varchar(100), customer nvarchar(255), sku varchar(100), qty smallint);
    INSERT @lines VALUES
        ('DEMO-GOODS-001', N'Khách mẫu 01', 'DEMO-WATER', 2),
        ('DEMO-GOODS-002', N'Khách mẫu 02', 'DEMO-SHUTTLE', 2),
        ('DEMO-GOODS-002', N'Khách mẫu 02', 'DEMO-WATER', 2),
        ('DEMO-GOODS-003', N'Khách mẫu 03', 'DEMO-PICKLEBALL', 2),
        ('DEMO-GOODS-003', N'Khách mẫu 03', 'DEMO-WATER', 4),
        ('DEMO-GOODS-004', N'Khách mẫu 04', 'DEMO-SHUTTLE', 4),
        ('DEMO-GOODS-004', N'Khách mẫu 04', 'DEMO-PICKLEBALL', 1),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-WATER', 5),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-SHUTTLE', 2),
        ('DEMO-GOODS-005', N'Khách mẫu 05', 'DEMO-PICKLEBALL', 2);
    IF EXISTS (SELECT 1 FROM @lines l WHERE NOT EXISTS
        (SELECT 1 FROM dbo.products p WHERE p.sku = l.sku AND p.is_active = 1 AND p.unit_price_vnd > 0))
        THROW 50003, 'Active demo products with positive prices are required.', 1;

    DECLARE @added TABLE (id bigint, code varchar(100));
    INSERT dbo.orders (order_code, staff_id, customer_name, total_amount, status, payment_state, handover_state)
    OUTPUT inserted.id, inserted.order_code INTO @added
    SELECT l.code, @staff, l.customer, SUM(l.qty * p.unit_price_vnd), 'Cart', 'Unpaid', 'Pending'
    FROM @lines l JOIN dbo.products p ON p.sku = l.sku
    WHERE NOT EXISTS (SELECT 1 FROM dbo.orders o WHERE o.order_code = l.code)
    GROUP BY l.code, l.customer;

    INSERT dbo.order_items (order_id, product_id, quantity, unit_price_vnd, subtotal_vnd)
    SELECT a.id, p.id, l.qty, p.unit_price_vnd, l.qty * p.unit_price_vnd
    FROM @added a JOIN @lines l ON l.code = a.code JOIN dbo.products p ON p.sku = l.sku;
    COMMIT;
    SELECT COUNT(*) AS inserted_orders FROM @added;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;
