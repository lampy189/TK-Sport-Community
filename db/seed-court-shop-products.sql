-- Chạy trên DB demo đã có các bảng/phiên bản giá hiện hành.
-- @Apply = 0 để xem trước. Đặt @Apply = 1 để thêm những SKU còn thiếu.
-- Sau khi tạo, gửi giá hàng hóa để Owner duyệt và mở bán khi giá có hiệu lực.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

DECLARE @Apply bit = 0;
DECLARE @venues table (id bigint PRIMARY KEY, prefix varchar(2), name nvarchar(255));
INSERT @venues VALUES (1, 'VP', N'TKSC Vĩnh Phúc'), (11, 'HN', N'TKSC Hà Nội');

IF EXISTS (
    SELECT 1 FROM @venues expected
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.venues v
        WHERE v.id = expected.id AND v.name = expected.name AND v.is_active = 1
    )
)
    THROW 51001, N'Sai cơ sở đích. Kiểm tra DB và bảng @venues trước khi chạy.', 1;

DECLARE @items table (
    sku varchar(100) PRIMARY KEY,
    name nvarchar(255),
    category varchar(100),
    price_vnd int,
    quantity int,
    reorder_level int,
    unit_name nvarchar(50),
    venue_id bigint,
    actor_id bigint NULL
);
INSERT @items
SELECT CONCAT(v.prefix, '-', p.code), p.name, p.category, p.price_vnd, p.quantity, p.reorder_level, p.unit_name, v.id, staff.id
FROM @venues v
CROSS JOIN (VALUES
    ('BADMINTON-RACKET', N'Vợt cầu lông tập luyện', 'Equipment', 250000, 8, 2, N'Cây'),
    ('SPORT-SOCKS', N'Tất thể thao cổ ngắn', 'Apparel', 35000, 20, 5, N'Đôi'),
    ('GRANOLA-BAR', N'Thanh ngũ cốc', 'Food', 20000, 30, 8, N'Thanh')
) p(code, name, category, price_vnd, quantity, reorder_level, unit_name)
OUTER APPLY (
    SELECT TOP (1) u.id FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id
    WHERE u.venue_id = v.id AND r.code = 'Staff' AND u.status = 'Active' AND u.deleted_at IS NULL
    ORDER BY u.id
) staff;

IF EXISTS (SELECT 1 FROM @items WHERE actor_id IS NULL)
    THROW 51002, N'Cần một tài khoản Staff đang hoạt động tại mỗi cơ sở.', 1;

IF @Apply = 0
BEGIN
    SELECT i.venue_id, i.sku, i.name, i.price_vnd, i.quantity, i.unit_name,
           CASE WHEN p.id IS NULL THEN N'Sẽ thêm' ELSE N'SKU đã có' END AS ket_qua
    FROM @items i LEFT JOIN dbo.products p ON p.sku = i.sku
    ORDER BY i.venue_id, i.sku;
    RETURN;
END;

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (
        SELECT 1 FROM @items i JOIN dbo.products p WITH (UPDLOCK, HOLDLOCK) ON p.sku = i.sku
        WHERE p.venue_id IS NULL OR p.venue_id <> i.venue_id
    )
        THROW 51003, N'Một SKU đã thuộc cơ sở khác. Kiểm tra trước khi chạy lại.', 1;

    DECLARE @newProducts table (id bigint PRIMARY KEY, sku varchar(100));
    INSERT dbo.products (sku, name, category, unit_price_vnd, is_active, venue_id, requires_price_approval)
    OUTPUT inserted.id, inserted.sku INTO @newProducts
    SELECT i.sku, i.name, i.category, i.price_vnd, 0, i.venue_id, 1
    FROM @items i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.products p WITH (UPDLOCK, HOLDLOCK) WHERE p.sku = i.sku);

    INSERT dbo.product_units (product_id, unit_name, factor_to_base)
    SELECT p.id, i.unit_name, 1 FROM @newProducts p JOIN @items i ON i.sku = p.sku;

    DECLARE @newStock table (id bigint PRIMARY KEY, product_id bigint);
    INSERT dbo.stock_items (product_id, quantity_on_hand, reorder_level, venue_id)
    OUTPUT inserted.id, inserted.product_id INTO @newStock
    SELECT p.id, i.quantity, i.reorder_level, i.venue_id
    FROM @newProducts p JOIN @items i ON i.sku = p.sku;

    INSERT dbo.stock_movements (stock_item_id, movement_type, quantity, reference_type, reference_id,
                                created_by, entered_unit, entered_quantity)
    SELECT s.id, 'OpeningBalance', i.quantity, 'Product', p.id, i.actor_id, i.unit_name, i.quantity
    FROM @newStock s JOIN @newProducts p ON p.id = s.product_id JOIN @items i ON i.sku = p.sku;

    COMMIT TRANSACTION;
    SELECT p.id, i.venue_id, p.sku, i.name
    FROM @newProducts p JOIN @items i ON i.sku = p.sku
    ORDER BY i.venue_id, p.sku;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
