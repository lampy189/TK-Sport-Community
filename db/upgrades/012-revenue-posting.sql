SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
-- Post receipts/refunds atomically with their source state change, including verified webhook retries.
SET XACT_ABORT ON;
GO
CREATE OR ALTER TRIGGER dbo.TR_payments_revenue ON dbo.payments AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 -- Only transitions to paid; old payments are not silently backdated.
 SELECT i.id,i.order_id,i.goods_order_id,i.amount_vnd,i.confirmed_by
 INTO #paid FROM inserted i LEFT JOIN deleted d ON d.id=i.id
 WHERE LOWER(i.status)='paid' AND (d.id IS NULL OR LOWER(d.status)<>'paid');
 IF NOT EXISTS(SELECT 1 FROM #paid) RETURN;
 -- Each social order belongs to one accounting center, including tournament orders.
 -- Separate orders are required for separate centers; no guessed proportional allocation.
 IF EXISTS(SELECT 1 FROM #paid p WHERE p.order_id IS NOT NULL AND
   (SELECT COUNT(DISTINCT c.venue_id) FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.courts c ON c.id=s.court_id WHERE t.order_id=p.order_id)<>1)
  THROW 51031,'Social payment must have tickets in exactly one Sport Center.',1;
 IF EXISTS(SELECT 1 FROM #paid p JOIN dbo.orders o ON o.id=p.goods_order_id WHERE o.venue_id IS NULL)
  THROW 51032,'Assign the goods order to a Sport Center before confirming payment.',1;
 INSERT dbo.revenue_entries(venue_id,payment_id,entry_type,amount_vnd,recognized_at,recorded_by)
 SELECT COALESCE(o.venue_id,v.venue_id),p.id,CASE WHEN p.goods_order_id IS NOT NULL THEN 'goods_receipt' ELSE 'social_receipt' END,
 p.amount_vnd,SYSDATETIMEOFFSET(),p.confirmed_by
 FROM #paid p LEFT JOIN dbo.orders o ON o.id=p.goods_order_id
 OUTER APPLY(SELECT MIN(c.venue_id) venue_id FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.courts c ON c.id=s.court_id WHERE t.order_id=p.order_id) v
 WHERE p.amount_vnd>0 AND NOT EXISTS(SELECT 1 FROM dbo.revenue_entries e WHERE e.payment_id=p.id AND e.refund_id IS NULL);
END;
GO
CREATE OR ALTER TRIGGER dbo.TR_refunds_revenue ON dbo.refunds AFTER INSERT, UPDATE AS
BEGIN
 SET NOCOUNT ON;
 SELECT i.* INTO #completed FROM inserted i LEFT JOIN deleted d ON d.id=i.id
 WHERE LOWER(i.status)='completed' AND (d.id IS NULL OR LOWER(d.status)<>'completed');
 IF NOT EXISTS(SELECT 1 FROM #completed) RETURN;
 IF EXISTS(SELECT 1 FROM #completed r WHERE r.amount_vnd IS NULL OR r.amount_vnd<=0 OR NOT EXISTS(
  SELECT 1 FROM dbo.revenue_entries e WHERE e.payment_id=r.payment_id AND e.refund_id IS NULL))
  THROW 51033,'Reconcile the original receipt before posting this refund.',1;
 IF EXISTS(SELECT 1 FROM #completed r JOIN dbo.payments p WITH(UPDLOCK,HOLDLOCK) ON p.id=r.payment_id
  WHERE (SELECT SUM(CONVERT(bigint,x.amount_vnd)) FROM dbo.refunds x WHERE x.payment_id=p.id AND LOWER(x.status)='completed')>p.amount_vnd)
  THROW 51034,'Completed refunds exceed the original payment.',1;
 INSERT dbo.revenue_entries(venue_id,payment_id,refund_id,entry_type,amount_vnd,recognized_at,recorded_by)
 SELECT e.venue_id,r.payment_id,r.id,'refund',r.amount_vnd,SYSDATETIMEOFFSET(),r.initiated_by
 FROM #completed r JOIN dbo.revenue_entries e ON e.payment_id=r.payment_id AND e.refund_id IS NULL
 WHERE NOT EXISTS(SELECT 1 FROM dbo.revenue_entries old WHERE old.refund_id=r.id);
END;
GO
