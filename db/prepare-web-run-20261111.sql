-- Local test refresh requested for 11 November 2026, 10:00 Asia/Ho_Chi_Minh.
-- Preserve real login accounts and rubrics; replace the old test business dataset.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME()<>'Sep490' THROW 51000,'Wrong database',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRANSACTION;
DECLARE @vp bigint=(SELECT id FROM dbo.venues WHERE name=N'TKSC Vĩnh Phúc');
DECLARE @hn bigint=(SELECT id FROM dbo.venues WHERE name=N'TKSC Hà Nội');
IF @vp IS NULL OR @hn IS NULL THROW 51000,'Expected Hanoi and Vinh Phuc',1;
DECLARE @q nvarchar(max)='';
SELECT @q=@q+'ALTER TABLE dbo.'+QUOTENAME(name)+' NOCHECK CONSTRAINT ALL;' FROM sys.tables;
EXEC sys.sp_executesql @q;
SET @q='';
SELECT @q=@q+'DELETE FROM dbo.'+QUOTENAME(name)+';' FROM sys.tables
WHERE name NOT IN('users','players','player_sport_profiles','roles','sports','rubrics','rubric_dimensions','venues','courts','time_slots');
EXEC sys.sp_executesql @q;
SELECT id INTO #remove_users FROM dbo.users WHERE email LIKE '%@demo.tksc.invalid' OR email LIKE 'demo-%@example.invalid' OR email='demo@tksc.local';
DELETE sp FROM dbo.player_sport_profiles sp JOIN dbo.players p ON p.id=sp.player_id JOIN #remove_users x ON x.id=p.user_id;
DELETE p FROM dbo.players p JOIN #remove_users x ON x.id=p.user_id;
DELETE u FROM dbo.users u JOIN #remove_users x ON x.id=u.id;
DELETE dbo.courts WHERE venue_id NOT IN(@hn,@vp) OR name IN(N'Sân Demo Cầu lông',N'Sân Demo Pickleball');
DELETE dbo.venues WHERE id NOT IN(@hn,@vp);
UPDATE dbo.venues SET is_active=1,address=CASE WHEN id=@hn THEN N'Hà Nội' ELSE N'Vĩnh Phúc' END;
UPDATE c SET name=CONCAT(N'Sân ',CASE WHEN s.code='badminton' THEN N'Cầu lông ' ELSE N'Pickleball ' END,ROW_NUM),is_active=1
FROM dbo.courts c JOIN dbo.sports s ON s.id=c.sport_id
JOIN (SELECT id,ROW_NUMBER() OVER(PARTITION BY venue_id,sport_id ORDER BY id) ROW_NUM FROM dbo.courts) n ON n.id=c.id;
UPDATE u SET venue_id=@vp FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code IN('Manager','Staff','Referee');
UPDATE dbo.player_sport_profiles SET current_score=NULL,current_band=NULL,rating_status='Unrated';
SELECT c.id,c.venue_id,c.sport_id,CONVERT(int,ROW_NUMBER() OVER(PARTITION BY c.venue_id ORDER BY c.id)) n INTO #courts FROM dbo.courts c;
DECLARE @accounts TABLE(email nvarchar(320),full_name nvarchar(255),role_code varchar(100),venue_id bigint);
INSERT @accounts VALUES
 ('manager.hn@tksc.local',N'Nguyễn Minh Quân - Quản lý Hà Nội','Manager',@hn),
 ('staff.hn01@tksc.local',N'Nguyễn Thu Hà - Nhân viên Hà Nội','Staff',@hn),
 ('staff.hn02@tksc.local',N'Trần Minh Anh - Nhân viên Hà Nội','Staff',@hn),
 ('staff.vp01@tksc.local',N'Nguyễn Hoàng Nam - Nhân viên Vĩnh Phúc','Staff',@vp),
 ('staff.vp02@tksc.local',N'Lê Thanh Mai - Nhân viên Vĩnh Phúc','Staff',@vp),
 ('player.hn01@tksc.local',N'Nguyễn Hải Anh','Player',NULL),
 ('player.hn02@tksc.local',N'Trần Ngọc Linh','Player',NULL),
 ('player.hn03@tksc.local',N'Phạm Minh Đức','Player',NULL),
 ('player.hn04@tksc.local',N'Lê Thu Trang','Player',NULL),
 ('player.vp01@tksc.local',N'Nguyễn Tuấn Anh','Player',NULL),
 ('player.vp02@tksc.local',N'Trần Hoài An','Player',NULL),
 ('player.vp03@tksc.local',N'Vũ Minh Khang','Player',NULL),
 ('player.vp04@tksc.local',N'Đỗ Phương Thảo','Player',NULL),
 ('player_unrated@tksc.local',N'Người chơi mới - Chưa khảo sát','Player',NULL);
INSERT @accounts
SELECT CONCAT('referee.',CASE WHEN venue_id=@hn THEN 'hn' ELSE 'vp' END,FORMAT(n,'00'),'@tksc.local'),
 CONCAT(N'Trọng tài ',CASE WHEN venue_id=@hn THEN N'Hà Nội ' ELSE N'Vĩnh Phúc ' END,FORMAT(n,'00')),'Referee',venue_id FROM #courts;
INSERT dbo.users(role_id,email,full_name,status,venue_id)
SELECT r.id,a.email,a.full_name,'Active',a.venue_id FROM @accounts a JOIN dbo.roles r ON r.code=a.role_code WHERE NOT EXISTS(SELECT 1 FROM dbo.users u WHERE u.email=a.email);
INSERT dbo.players(user_id) SELECT u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Player' AND NOT EXISTS(SELECT 1 FROM dbo.players p WHERE p.user_id=u.id);
INSERT dbo.player_sport_profiles(player_id,sport_id,rating_status) SELECT p.id,s.id,'Unrated' FROM dbo.players p CROSS JOIN dbo.sports s WHERE NOT EXISTS(SELECT 1 FROM dbo.player_sport_profiles x WHERE x.player_id=p.id AND x.sport_id=s.id);
DECLARE @managerVP bigint=(SELECT id FROM dbo.users WHERE email='manager@tksc.local');
DECLARE @managerHN bigint=(SELECT id FROM dbo.users WHERE email='manager.hn@tksc.local');
DECLARE @day date='20261111';
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
SELECT c.sport_id,c.id,t.id,DATEADD(day,d.n,@day),NULL,8,CASE WHEN s.code='badminton' THEN 80000 ELSE 100000 END,'Published',CASE WHEN c.venue_id=@hn THEN @managerHN ELSE @managerVP END
FROM #courts c JOIN dbo.sports s ON s.id=c.sport_id CROSS JOIN dbo.time_slots t CROSS JOIN(VALUES(0),(1),(2),(3),(4),(5),(6)) d(n);
INSERT dbo.session_referees(session_id,referee_id,assigned_by)
SELECT s.id,u.id,s.created_by FROM dbo.sessions s JOIN #courts c ON c.id=s.court_id JOIN dbo.users u ON u.email=CONCAT('referee.',CASE WHEN c.venue_id=@hn THEN 'hn' ELSE 'vp' END,FORMAT(c.n,'00'),'@tksc.local');
-- Keep the existing Google referee usable on the first Vinh Phuc court.
INSERT dbo.session_referees(session_id,referee_id,assigned_by) SELECT s.id,u.id,s.created_by FROM dbo.sessions s JOIN #courts c ON c.id=s.court_id CROSS JOIN dbo.users u WHERE c.venue_id=@vp AND c.n=1 AND u.email='referee@tksc.local';
INSERT dbo.session_staff(session_id,staff_id)
SELECT s.id,u.id FROM dbo.sessions s JOIN #courts c ON c.id=s.court_id JOIN dbo.users u ON u.email IN(CASE WHEN c.venue_id=@hn THEN 'staff.hn01@tksc.local' ELSE 'staff.vp01@tksc.local' END,CASE WHEN c.venue_id=@hn THEN 'staff.hn02@tksc.local' ELSE 'staff.vp02@tksc.local' END);
INSERT dbo.session_staff(session_id,staff_id) SELECT s.id,u.id FROM dbo.sessions s JOIN #courts c ON c.id=s.court_id CROSS JOIN dbo.users u WHERE c.venue_id=@vp AND u.email='staff@tksc.local';
-- Completed self-surveys for the eight prepared players, leaving existing personal accounts unrated.
INSERT dbo.assessments(player_sport_profile_id,rubric_id,assessment_type,assessor_id,status,total_score,band_result)
SELECT sp.id,r.id,'SelfSurvey',p.user_id,'Published',50,'B3' FROM dbo.player_sport_profiles sp JOIN dbo.players p ON p.id=sp.player_id JOIN dbo.users u ON u.id=p.user_id JOIN dbo.rubrics r ON r.sport_id=sp.sport_id AND r.is_active=1 WHERE u.email LIKE 'player.hn0%@tksc.local' OR u.email LIKE 'player.vp0%@tksc.local';
INSERT dbo.assessment_scores(assessment_id,dimension_id,score) SELECT a.id,d.id,2 FROM dbo.assessments a JOIN dbo.rubric_dimensions d ON d.rubric_id=a.rubric_id;
INSERT dbo.player_rating_history(player_sport_profile_id,new_score,new_band,changed_by,assessment_id) SELECT player_sport_profile_id,50,'B3',assessor_id,id FROM dbo.assessments;
UPDATE sp SET current_score=50,current_band='B3',rating_status='Provisional' FROM dbo.player_sport_profiles sp JOIN dbo.assessments a ON a.player_sport_profile_id=sp.id;
-- Four participants per center: two badminton and two pickleball, at 10:00 tomorrow.
DECLARE @fixtures TABLE(user_id bigint,venue_id bigint,court_id bigint,checked bit);
INSERT @fixtures
SELECT u.id,v.venue_id,c.id,CASE WHEN RIGHT(LEFT(u.email,11),1) IN('1','3') THEN 1 ELSE 0 END
FROM dbo.users u CROSS APPLY(SELECT CASE WHEN u.email LIKE 'player.hn%' THEN @hn ELSE @vp END venue_id,CASE WHEN u.email LIKE '%01@%' OR u.email LIKE '%02@%' THEN 'badminton' ELSE 'pickleball' END sport) v
JOIN dbo.sports sport ON sport.code=v.sport JOIN #courts c ON c.venue_id=v.venue_id AND c.sport_id=sport.id
WHERE (u.email LIKE 'player.hn0%@tksc.local' OR u.email LIKE 'player.vp0%@tksc.local') AND c.id=(SELECT MIN(id) FROM #courts c2 WHERE c2.venue_id=c.venue_id AND c2.sport_id=c.sport_id);
UPDATE f SET checked=CASE WHEN u.email LIKE '%01@%' OR u.email LIKE '%03@%' THEN 1 ELSE 0 END FROM @fixtures f JOIN dbo.users u ON u.id=f.user_id;
-- Personal Google player accounts receive a paid check-in ticket at Vinh Phuc, spread across courts.
INSERT @fixtures SELECT u.id,@vp,c.id,0 FROM (SELECT u.id,ROW_NUMBER() OVER(ORDER BY u.id) n FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE r.code='Player' AND u.google_subject IS NOT NULL AND u.status='Active') u JOIN #courts c ON c.venue_id=@vp AND c.n=((u.n-1)%7)+1;
DECLARE @uid bigint,@vid bigint,@cid bigint,@checked bit,@pid bigint,@sid bigint,@oid bigint,@price int,@staff bigint,@ref bigint,@ix int;
DECLARE fixtures CURSOR LOCAL FAST_FORWARD FOR SELECT user_id,venue_id,court_id,checked FROM @fixtures;
OPEN fixtures; FETCH NEXT FROM fixtures INTO @uid,@vid,@cid,@checked;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @pid=id FROM dbo.players WHERE user_id=@uid;
 SELECT @staff=id FROM dbo.users WHERE email=CASE WHEN @vid=@hn THEN 'staff.hn01@tksc.local' ELSE 'staff.vp01@tksc.local' END;
 SET @ix=0;
 WHILE @ix<2
 BEGIN
  SELECT @sid=s.id,@price=s.price_vnd FROM dbo.sessions s JOIN dbo.time_slots t ON t.id=s.time_slot_id WHERE s.court_id=@cid AND s.session_date=@day AND t.start_time=CASE WHEN @ix=0 THEN '10:00' ELSE '14:00' END;
  SELECT @ref=MIN(referee_id) FROM dbo.session_referees WHERE session_id=@sid;
  INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(CONCAT('RUN11-',@uid,'-',@ix),@uid,@price,CASE WHEN @ix=0 THEN 'paid' ELSE 'pending' END); SET @oid=SCOPE_IDENTITY();
  INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd,checked_in_by) VALUES(@oid,@sid,@pid,CONCAT('TK11-',@uid,'-',@ix),CASE WHEN @ix=1 THEN 'reserved' WHEN @checked=1 THEN 'checkedin' ELSE 'confirmed' END,@price,CASE WHEN @ix=0 AND @checked=1 THEN @ref END);
  INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@oid,@uid,@price,'cash',CASE WHEN @ix=0 THEN 'paid' ELSE 'pending' END,CONCAT('PAY11-',@uid,'-',@ix),CASE WHEN @ix=0 THEN @staff END);
  SET @ix+=1;
 END;
 FETCH NEXT FROM fixtures INTO @uid,@vid,@cid,@checked;
END;
CLOSE fixtures; DEALLOCATE fixtures;
-- Draft referee forms for the four checked-in players.
INSERT dbo.assessments(player_sport_profile_id,session_id,rubric_id,assessment_type,assessor_id,status)
SELECT sp.id,s.id,r.id,'on_court',sr.referee_id,'draft' FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.player_sport_profiles sp ON sp.player_id=t.player_id AND sp.sport_id=s.sport_id JOIN dbo.rubrics r ON r.sport_id=s.sport_id AND r.is_active=1 JOIN dbo.session_referees sr ON sr.session_id=s.id WHERE t.status='checkedin';
INSERT dbo.assessment_scores(assessment_id,dimension_id,score) SELECT a.id,d.id,NULL FROM dbo.assessments a JOIN dbo.rubric_dimensions d ON d.rubric_id=a.rubric_id WHERE a.status='draft';
-- Separate catalog and traceable opening stock in each center.
INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id)
SELECT CONCAT(v.code,'-',p.sku),p.name,p.category,p.price,1,v.id FROM (VALUES(@hn,'HN'),(@vp,'VP')) v(id,code) CROSS JOIN(VALUES('WATER',N'Nước suối 500ml','Drink',10000),('SHUTTLE',N'Quả cầu lông','Equipment',25000),('BALL',N'Bóng Pickleball','Equipment',50000),('TOWEL',N'Khăn thể thao','Equipment',40000)) p(sku,name,category,price);
INSERT dbo.product_units(product_id,unit_name,factor_to_base,barcode) SELECT id,CASE WHEN sku LIKE '%WATER' THEN N'Chai' ELSE N'Cái' END,1,CONCAT('TKSC',id) FROM dbo.products;
INSERT dbo.product_units(product_id,unit_name,factor_to_base) SELECT id,N'Thùng',24 FROM dbo.products WHERE sku LIKE '%WATER';
INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) SELECT id,CASE WHEN sku LIKE '%TOWEL' THEN 3 ELSE 100 END,10,venue_id FROM dbo.products;
INSERT dbo.stock_movements(stock_item_id,movement_type,quantity,reference_type,created_by,entered_unit,entered_quantity,source_ref)
SELECT si.id,'Receipt',si.quantity_on_hand,'Receipt',u.id,pu.unit_name,si.quantity_on_hand,'OPENING-20261111' FROM dbo.stock_items si JOIN dbo.products p ON p.id=si.product_id JOIN dbo.product_units pu ON pu.product_id=p.id AND pu.factor_to_base=1 JOIN dbo.users u ON u.email=CASE WHEN si.venue_id=@hn THEN 'staff.hn01@tksc.local' ELSE 'staff.vp01@tksc.local' END;
DECLARE goods CURSOR LOCAL FAST_FORWARD FOR SELECT id FROM dbo.venues;
OPEN goods; FETCH NEXT FROM goods INTO @vid;
WHILE @@FETCH_STATUS=0
BEGIN
 SELECT @staff=id FROM dbo.users WHERE email=CASE WHEN @vid=@hn THEN 'staff.hn01@tksc.local' ELSE 'staff.vp01@tksc.local' END;
 SELECT @uid=id FROM dbo.users WHERE email=CASE WHEN @vid=@hn THEN 'player.hn01@tksc.local' ELSE 'player.vp01@tksc.local' END;
 SET @ix=0;
 WHILE @ix<2
 BEGIN
  INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id,buyer_id)
  VALUES(CONCAT('SALE11-',@vid,'-',@ix),@staff,N'Khách chạy thử ngày 11/11',45000,CASE WHEN @ix=0 THEN 'Cart' ELSE 'Paid' END,CASE WHEN @ix=0 THEN 'Unpaid' ELSE 'Paid' END,'Pending',@vid,@uid); SET @oid=SCOPE_IDENTITY();
  INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) SELECT @oid,id,CASE WHEN sku LIKE '%WATER' THEN 2 ELSE 1 END,unit_price_vnd,unit_price_vnd*CASE WHEN sku LIKE '%WATER' THEN 2 ELSE 1 END FROM dbo.products WHERE venue_id=@vid AND (sku LIKE '%WATER' OR sku LIKE '%SHUTTLE');
  IF @ix=1 INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@oid,@uid,45000,'cash','paid',CONCAT('GOODSPAY11-',@vid),@staff);
  SET @ix+=1;
 END;
 FETCH NEXT FROM goods INTO @vid;
END;
CLOSE goods; DEALLOCATE goods;
-- Simulated receipts dated before tomorrow's 10:00 rehearsal, so its daily report is populated.
UPDATE dbo.revenue_entries SET recognized_at='2026-11-11T09:45:00+07:00',note=N'Dữ liệu chạy thử web 11/11/2026; thanh toán mô phỏng';
INSERT dbo.late_entry_offers(session_id,max_tickets,price_vnd,is_open,revision,updated_by,updated_at)
SELECT s.id,2,50000,1,1,s.created_by,SYSDATETIMEOFFSET() FROM dbo.sessions s JOIN #courts c ON c.id=s.court_id JOIN dbo.time_slots t ON t.id=s.time_slot_id WHERE c.n=1 AND s.session_date=@day AND t.start_time='10:00';
SET @q=''; SELECT @q=@q+'ALTER TABLE dbo.'+QUOTENAME(name)+' WITH CHECK CHECK CONSTRAINT ALL;' FROM sys.tables; EXEC sys.sp_executesql @q;
IF (SELECT COUNT(*) FROM dbo.venues)<>2 THROW 51000,'Expected exactly two centers',1;
IF EXISTS(SELECT 1 FROM dbo.sessions s JOIN dbo.tickets t ON t.session_id=s.id WHERE t.status IN('confirmed','checkedin','reserved') GROUP BY s.id,s.capacity HAVING COUNT(*)>s.capacity) THROW 51000,'Capacity exceeded',1;
IF EXISTS(SELECT 1 FROM dbo.sessions s WHERE NOT EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id)) THROW 51000,'Unassigned session',1;
IF EXISTS(SELECT 1 FROM dbo.session_referees a JOIN dbo.session_referees b ON a.referee_id=b.referee_id AND a.session_id<b.session_id JOIN dbo.sessions sa ON sa.id=a.session_id JOIN dbo.sessions sb ON sb.id=b.session_id JOIN dbo.time_slots ta ON ta.id=sa.time_slot_id JOIN dbo.time_slots tb ON tb.id=sb.time_slot_id WHERE sa.session_date=sb.session_date AND ta.start_time<tb.end_time AND tb.start_time<ta.end_time) THROW 51000,'Referee overlaps',1;
IF EXISTS(SELECT 1 FROM dbo.users WHERE email LIKE '%@demo.tksc.invalid' OR email LIKE 'demo-%@example.invalid') THROW 51000,'Old demo account remains',1;
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
SELECT 'TRIAL OK' result;
ROLLBACK;
