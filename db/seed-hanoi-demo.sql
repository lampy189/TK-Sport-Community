-- Explicitly requested new Hanoi branch fixtures. No payments, paid tickets or revenue are fabricated.
-- Development accounts have no password/Google subject and cannot use real Google sign-in.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @venue bigint=1,@actor bigint=(SELECT MIN(u.id) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Owner' AND u.deleted_at IS NULL);
 IF @actor IS NULL OR OBJECT_ID('dbo.product_units','U') IS NULL THROW 50000,'Owner and stock unit upgrade are required.',1;
 DECLARE @people TABLE(email nvarchar(320),full_name nvarchar(255),role_code varchar(30));
 INSERT @people VALUES
 (N'demo-manager-hn@example.invalid',N'[DEMO HN] Quản lý Hà Nội','Manager'),
 (N'demo-staff-hn@example.invalid',N'[DEMO HN] Nhân viên Hà Nội','Staff'),
 (N'demo-referee-hn@example.invalid',N'[DEMO HN] Trọng tài Hà Nội','Referee');
 INSERT dbo.users(role_id,email,full_name,status,venue_id)
 SELECT r.id,p.email,p.full_name,'Active',@venue FROM @people p JOIN dbo.roles r ON r.code=p.role_code
 WHERE NOT EXISTS(SELECT 1 FROM dbo.users u WHERE u.email=p.email);
 IF EXISTS(SELECT 1 FROM @people p JOIN dbo.users u ON u.email=p.email JOIN dbo.roles r ON r.id=u.role_id WHERE u.venue_id<>@venue OR r.code<>p.role_code)
  THROW 50000,'A Hanoi demo account already exists with different permissions; refusing to overwrite.',1;
 DECLARE @manager bigint=(SELECT id FROM dbo.users WHERE email=N'demo-manager-hn@example.invalid'),
 @staff bigint=(SELECT id FROM dbo.users WHERE email=N'demo-staff-hn@example.invalid'),
 @referee bigint=(SELECT id FROM dbo.users WHERE email=N'demo-referee-hn@example.invalid');
 INSERT dbo.courts(venue_id,name,sport_id,is_active)
 SELECT @venue,N'[DEMO HN] Sân '+s.name+N' 1',s.id,1 FROM dbo.sports s WHERE s.code IN ('badminton','pickleball')
 AND NOT EXISTS(SELECT 1 FROM dbo.courts c WHERE c.venue_id=@venue AND c.sport_id=s.id AND c.name=N'[DEMO HN] Sân '+s.name+N' 1');

 DECLARE @catalog TABLE(sku varchar(100),name nvarchar(255),category varchar(100),price int,qty int,unit nvarchar(50));
 INSERT @catalog VALUES
 ('HN-DEMO-WATER',N'[DEMO HN] Nước suối 500ml','Drink',10000,120,N'Chai'),
 ('HN-DEMO-SHUTTLE',N'[DEMO HN] Quả cầu lông','Equipment',25000,96,N'Quả'),
 ('HN-DEMO-PICKLEBALL',N'[DEMO HN] Bóng Pickleball','Equipment',50000,48,N'Quả'),
 ('HN-DEMO-TOWEL',N'[DEMO HN] Khăn thể thao','Accessory',35000,40,N'Cái');
 INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id)
 SELECT x.sku,x.name,x.category,x.price,1,@venue FROM @catalog x WHERE NOT EXISTS(SELECT 1 FROM dbo.products p WHERE p.sku=x.sku);
 INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id)
 SELECT p.id,x.qty,10,@venue FROM @catalog x JOIN dbo.products p ON p.sku=x.sku
 WHERE NOT EXISTS(SELECT 1 FROM dbo.stock_items si WHERE si.product_id=p.id);
 INSERT dbo.product_units(product_id,unit_name,factor_to_base,barcode)
 SELECT p.id,x.unit,1,x.sku FROM @catalog x JOIN dbo.products p ON p.sku=x.sku
 WHERE NOT EXISTS(SELECT 1 FROM dbo.product_units pu WHERE pu.product_id=p.id AND pu.factor_to_base=1);
 INSERT dbo.stock_movements(stock_item_id,movement_type,quantity,reference_type,reference_id,created_by,entered_unit,entered_quantity,source_ref)
 SELECT si.id,'OpeningBalance',x.qty,'Product',p.id,@actor,x.unit,x.qty,'HN-DEMO-OPENING-'+x.sku
 FROM @catalog x JOIN dbo.products p ON p.sku=x.sku JOIN dbo.stock_items si ON si.product_id=p.id
 WHERE NOT EXISTS(SELECT 1 FROM dbo.stock_movements m WHERE m.stock_item_id=si.id AND m.source_ref='HN-DEMO-OPENING-'+x.sku);

 DECLARE @now datetimeoffset=SWITCHOFFSET(SYSDATETIMEOFFSET(),'+07:00'),@today date,@running bigint,@future bigint;
 SET @today=CONVERT(date,@now);
 SELECT TOP(1) @running=id FROM dbo.time_slots WHERE start_time<=CONVERT(time,@now) AND end_time>CONVERT(time,@now) ORDER BY start_time;
 IF @running IS NULL
 BEGIN
  DECLARE @start time=TIMEFROMPARTS(DATEPART(hour,@now),0,0,0,0),@end time=CONVERT(time,DATEADD(hour,2,@now));
  IF @end<=@start BEGIN SET @start='21:00';SET @end='23:59'; END;
  IF NOT EXISTS(SELECT 1 FROM dbo.time_slots WHERE start_time=@start AND end_time=@end)
   INSERT dbo.time_slots(start_time,end_time,label) VALUES(@start,@end,CONVERT(nvarchar(5),@start,108)+N' - '+CONVERT(nvarchar(5),@end,108));
  SELECT TOP(1) @running=id FROM dbo.time_slots WHERE start_time=@start AND end_time=@end;
 END;
 SELECT TOP(1) @future=id FROM dbo.time_slots WHERE start_time>CONVERT(time,@now) ORDER BY start_time;
 DECLARE @slots TABLE(day date,slot bigint);
 INSERT @slots VALUES(@today,@running),(DATEADD(day,1,@today),3);
 IF @future IS NOT NULL INSERT @slots VALUES(@today,@future);
 INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
 SELECT c.sport_id,c.id,x.slot,x.day,NULL,8,80000,'Published',@manager FROM dbo.courts c CROSS JOIN @slots x
 WHERE c.venue_id=@venue AND c.name LIKE N'[[]DEMO HN]%' AND NOT EXISTS(
 SELECT 1 FROM dbo.sessions s WHERE s.court_id=c.id AND s.time_slot_id=x.slot AND s.session_date=x.day);
 INSERT dbo.session_referees(session_id,referee_id,assigned_by)
 SELECT s.id,@referee,@manager FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id
 WHERE c.venue_id=@venue AND c.name LIKE N'[[]DEMO HN]%' AND NOT EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id AND sr.referee_id=@referee);
 INSERT dbo.session_staff(session_id,staff_id,role_in_session)
 SELECT s.id,@staff,N'Vận hành mẫu Hà Nội' FROM dbo.sessions s JOIN dbo.courts c ON c.id=s.court_id
 WHERE c.venue_id=@venue AND c.name LIKE N'[[]DEMO HN]%' AND NOT EXISTS(SELECT 1 FROM dbo.session_staff ss WHERE ss.session_id=s.id AND ss.staff_id=@staff);
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
 THROW;
END CATCH;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
