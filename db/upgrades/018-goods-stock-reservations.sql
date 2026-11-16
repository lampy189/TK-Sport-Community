-- PAY-05. Review and explicitly approve before applying to Sep490.
-- Additive, manual activation. Backfills committed holds only for paid, not-yet-posted goods.
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
-- Hold inventory stable through backfill and guard installation.
DECLARE @inventoryRows bigint;
SELECT @inventoryRows=COUNT_BIG(*) FROM dbo.stock_items WITH (TABLOCKX,HOLDLOCK);
IF OBJECT_ID('dbo.goods_stock_reservations','U') IS NULL
BEGIN
 CREATE TABLE dbo.goods_stock_reservations(
  order_id bigint NOT NULL REFERENCES dbo.orders(id),
  stock_item_id bigint NOT NULL REFERENCES dbo.stock_items(id),
  quantity int NOT NULL CHECK(quantity>0),
  expires_at datetimeoffset(0) NOT NULL,
  state varchar(20) NOT NULL CHECK(state IN ('Held','Committed','Released','Consumed')),
  updated_at datetimeoffset(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT PK_goods_stock_reservations PRIMARY KEY(order_id,stock_item_id)
 );
 CREATE INDEX IX_goods_stock_reservations_stock ON dbo.goods_stock_reservations(stock_item_id,state,expires_at) INCLUDE(quantity);
END;
-- Preserve already-paid goods that have not posted their sale yet.
IF EXISTS (SELECT 1 FROM dbo.order_items i JOIN dbo.orders o ON o.id=i.order_id
 LEFT JOIN dbo.stock_items si ON si.product_id=i.product_id
 WHERE o.payment_state='Paid' AND COALESCE(o.handover_state,'')<>'Received'
 AND NOT EXISTS(SELECT 1 FROM dbo.stock_movements m WHERE m.reference_type='GoodsOrder' AND m.reference_id=o.id AND m.movement_type='SaleOut')
 AND si.id IS NULL) THROW 51010,N'Có đơn đã trả tiền nhưng thiếu stock_item. Đối soát trước khi nâng cấp.',1;
INSERT dbo.goods_stock_reservations(order_id,stock_item_id,quantity,expires_at,state)
 SELECT o.id,si.id,SUM(CONVERT(int,i.quantity)),SYSUTCDATETIME(),'Committed'
 FROM dbo.orders o JOIN dbo.order_items i ON i.order_id=o.id JOIN dbo.stock_items si ON si.product_id=i.product_id
 WHERE o.payment_state='Paid' AND COALESCE(o.handover_state,'')<>'Received'
 AND NOT EXISTS(SELECT 1 FROM dbo.stock_movements m WHERE m.reference_type='GoodsOrder' AND m.reference_id=o.id AND m.movement_type='SaleOut')
 AND NOT EXISTS(SELECT 1 FROM dbo.goods_stock_reservations r WHERE r.order_id=o.id AND r.stock_item_id=si.id)
 GROUP BY o.id,si.id;
IF EXISTS(SELECT 1 FROM dbo.stock_items si WHERE si.quantity_on_hand <
 (SELECT COALESCE(SUM(CONVERT(bigint,r.quantity)),0) FROM dbo.goods_stock_reservations r WHERE r.stock_item_id=si.id AND
  (r.state='Committed' OR (r.state='Held' AND r.expires_at>SYSUTCDATETIME()))))
 THROW 51010,N'Tồn hiện có không đủ cho các đơn đã trả tiền. Migration rollback; cần đối soát.',1;
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_stock_items_preserve_goods_holds ON dbo.stock_items AFTER UPDATE AS
BEGIN
 SET NOCOUNT ON;
 IF NOT UPDATE(quantity_on_hand) RETURN;
 IF EXISTS (
  SELECT 1 FROM inserted i
  WHERE i.quantity_on_hand < (SELECT COALESCE(SUM(CONVERT(bigint,r.quantity)),0)
    FROM dbo.goods_stock_reservations r WITH (UPDLOCK,HOLDLOCK)
    WHERE r.stock_item_id=i.id AND (r.state=''Committed'' OR (r.state=''Held'' AND r.expires_at>SYSUTCDATETIME()))))
  THROW 51010,N''Tồn kho đã được giữ cho đơn hàng. Không thể xuất phần đã giữ.'',1;
END;');
COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;


