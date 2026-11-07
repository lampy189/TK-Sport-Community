-- Synthetic business documents in the real local database. No gateway/email calls.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRANSACTION;
DECLARE @venue bigint,@staff bigint,@payer bigint,@player bigint,@court bigint,@session bigint,@order bigint,@payment bigint,@ticket bigint,@product bigint,@refund bigint;
DECLARE @sport bigint=(SELECT TOP(1) id FROM dbo.sports WHERE code='badminton');
DECLARE @slot bigint=(SELECT MIN(id) FROM dbo.time_slots);
IF @sport IS NULL OR @slot IS NULL THROW 50000,'Sports and time slots are required.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Thủ Đức') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Thủ Đức',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Thủ Đức';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc01@example.invalid',N'[DEMO] Staff Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc01@example.invalid',N'[DEMO] Manager Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc01@example.invalid',N'[DEMO] Referee Thủ Đức','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc01@example.invalid',N'[DEMO] Player Thủ Đức','Active',NULL FROM dbo.roles WHERE code=N'Player';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-owner-sc01@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-owner-sc01@example.invalid',N'[DEMO] Owner Thủ Đức','Active',NULL FROM dbo.roles WHERE code=N'Owner';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc01@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc01@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC01') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC01',N'[DEMO] Combo cầu và nước Thủ Đức','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC01';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,600000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-0',@payer,600000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-0','confirmed',600000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,600000,'cash','paid',N'DEMO-SC-SOC-0-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-0',@staff,N'[DEMO] Khách mẫu',150000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,150000,150000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,150000,'cash','paid',N'DEMO-SC-GOODS-0-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,680000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-0',@payer,680000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-0','confirmed',680000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,680000,'cash','paid',N'DEMO-SC-SOC-1-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-0',@staff,N'[DEMO] Khách mẫu',170000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,170000,170000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,170000,'cash','paid',N'DEMO-SC-GOODS-1-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,760000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-0',@payer,760000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-0','confirmed',760000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,760000,'cash','paid',N'DEMO-SC-SOC-2-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-0',@staff,N'[DEMO] Khách mẫu',190000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,190000,190000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,190000,'cash','paid',N'DEMO-SC-GOODS-2-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,840000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-0',@payer,840000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-0','confirmed',840000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,840000,'cash','paid',N'DEMO-SC-SOC-3-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-0',@staff,N'[DEMO] Khách mẫu',210000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,210000,210000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,210000,'cash','paid',N'DEMO-SC-GOODS-3-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,920000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-0',@payer,920000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-0','confirmed',920000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,920000,'cash','paid',N'DEMO-SC-SOC-4-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-0',@staff,N'[DEMO] Khách mẫu',230000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,230000,230000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,230000,'cash','paid',N'DEMO-SC-GOODS-4-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1000000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-0',@payer,1000000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-0','confirmed',1000000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1000000,'cash','paid',N'DEMO-SC-SOC-5-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-0',@staff,N'[DEMO] Khách mẫu',250000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,250000,250000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,250000,'cash','paid',N'DEMO-SC-GOODS-5-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-0') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1080000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-0',@payer,1080000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-0','confirmed',1080000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1080000,'cash','paid',N'DEMO-SC-SOC-6-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-0') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-0',@staff,N'[DEMO] Khách mẫu',270000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,270000,270000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,270000,'cash','paid',N'DEMO-SC-GOODS-6-0',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-CUP-01') BEGIN
INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,N'DEMO-SC-CUP-01',@sport,1); SET @court=SCOPE_IDENTITY();
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,2400000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-CUP-01',@payer,2400000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-CUP-01','confirmed',2400000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,2400000,'cash','paid',N'DEMO-SC-CUP-01',@staff); SET @payment=SCOPE_IDENTITY();
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T08:00:00+07:00',note=N'[DEMO] Giải giao hữu liên cơ sở · phần thu Thủ Đức',event_code=N'CUP-DEMO' WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-0-0';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,80000,'completed',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T10:00:00+07:00',note=N'[DEMO] Hoàn vé đã thu ngày 30/10' WHERE refund_id=@refund;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Bình Thạnh') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Bình Thạnh',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Bình Thạnh';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc02@example.invalid',N'[DEMO] Staff Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc02@example.invalid',N'[DEMO] Manager Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc02@example.invalid',N'[DEMO] Referee Bình Thạnh','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc02@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc02@example.invalid',N'[DEMO] Player Bình Thạnh','Active',NULL FROM dbo.roles WHERE code=N'Player';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc02@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc02@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC02') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC02',N'[DEMO] Combo cầu và nước Bình Thạnh','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC02';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,720000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-1',@payer,720000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-1','confirmed',720000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,720000,'cash','paid',N'DEMO-SC-SOC-0-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-1',@staff,N'[DEMO] Khách mẫu',185000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,185000,185000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,185000,'cash','paid',N'DEMO-SC-GOODS-0-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,800000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-1',@payer,800000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-1','confirmed',800000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,800000,'cash','paid',N'DEMO-SC-SOC-1-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-1',@staff,N'[DEMO] Khách mẫu',205000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,205000,205000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,205000,'cash','paid',N'DEMO-SC-GOODS-1-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,880000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-1',@payer,880000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-1','confirmed',880000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,880000,'cash','paid',N'DEMO-SC-SOC-2-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-1',@staff,N'[DEMO] Khách mẫu',225000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,225000,225000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,225000,'cash','paid',N'DEMO-SC-GOODS-2-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,960000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-1',@payer,960000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-1','confirmed',960000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,960000,'cash','paid',N'DEMO-SC-SOC-3-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-1',@staff,N'[DEMO] Khách mẫu',245000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,245000,245000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,245000,'cash','paid',N'DEMO-SC-GOODS-3-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1040000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-1',@payer,1040000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-1','confirmed',1040000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1040000,'cash','paid',N'DEMO-SC-SOC-4-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-1',@staff,N'[DEMO] Khách mẫu',265000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,265000,265000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,265000,'cash','paid',N'DEMO-SC-GOODS-4-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1120000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-1',@payer,1120000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-1','confirmed',1120000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1120000,'cash','paid',N'DEMO-SC-SOC-5-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-1',@staff,N'[DEMO] Khách mẫu',285000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,285000,285000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,285000,'cash','paid',N'DEMO-SC-GOODS-5-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-1') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1200000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-1',@payer,1200000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-1','confirmed',1200000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1200000,'cash','paid',N'DEMO-SC-SOC-6-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-1') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-1',@staff,N'[DEMO] Khách mẫu',305000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,305000,305000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,305000,'cash','paid',N'DEMO-SC-GOODS-6-1',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-PENDING-01') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,320000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-PENDING-01',@payer,320000,'pending'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-PENDING-01','reserved',320000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,320000,'cash','pending',N'DEMO-SC-PENDING-01',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T10:00:00+07:00',note=N'[DEMO] Chưa xác nhận chuyển khoản',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-3-1';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,160000,'completed',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T09:00:00+07:00',note=N'[DEMO] Hoàn hai vé' WHERE refund_id=@refund;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.venues WHERE name=N'[DEMO-SC] Quận 7') INSERT dbo.venues(name,is_active) VALUES(N'[DEMO-SC] Quận 7',1);
SELECT @venue=id FROM dbo.venues WHERE name=N'[DEMO-SC] Quận 7';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-staff-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-staff-sc03@example.invalid',N'[DEMO] Staff Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Staff';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-manager-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-manager-sc03@example.invalid',N'[DEMO] Manager Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Manager';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-referee-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-referee-sc03@example.invalid',N'[DEMO] Referee Quận 7','Active',@venue FROM dbo.roles WHERE code=N'Referee';
IF NOT EXISTS(SELECT 1 FROM dbo.users WHERE email=N'demo-player-sc03@example.invalid') INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,N'demo-player-sc03@example.invalid',N'[DEMO] Player Quận 7','Active',NULL FROM dbo.roles WHERE code=N'Player';
SELECT @payer=id FROM dbo.users WHERE email=N'demo-player-sc03@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.players WHERE user_id=@payer) INSERT dbo.players(user_id) VALUES(@payer);
SELECT @player=id FROM dbo.players WHERE user_id=@payer;
SELECT @staff=id FROM dbo.users WHERE email=N'demo-staff-sc03@example.invalid';
IF NOT EXISTS(SELECT 1 FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court') INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,'DEMO-SC-Court',@sport,1);
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
IF NOT EXISTS(SELECT 1 FROM dbo.products WHERE sku=N'DEMO-SC-SC03') INSERT dbo.products(sku,name,category,unit_price_vnd,is_active,venue_id) VALUES(N'DEMO-SC-SC03',N'[DEMO] Combo cầu và nước Quận 7','Demo',150000,1,@venue);
SELECT @product=id FROM dbo.products WHERE sku=N'DEMO-SC-SC03';
IF NOT EXISTS(SELECT 1 FROM dbo.stock_items WHERE product_id=@product) INSERT dbo.stock_items(product_id,quantity_on_hand,reorder_level,venue_id) VALUES(@product,100,10,@venue);
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-0-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-30',8,840000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-30' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-0-2',@payer,840000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-0-2','confirmed',840000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,840000,'cash','paid',N'DEMO-SC-SOC-0-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-0-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-0-2',@staff,N'[DEMO] Khách mẫu',220000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,220000,220000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,220000,'cash','paid',N'DEMO-SC-GOODS-0-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-30T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-1-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-10-31',8,920000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-10-31' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-1-2',@payer,920000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-1-2','confirmed',920000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,920000,'cash','paid',N'DEMO-SC-SOC-1-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-1-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-1-2',@staff,N'[DEMO] Khách mẫu',240000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,240000,240000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,240000,'cash','paid',N'DEMO-SC-GOODS-1-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-10-31T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-2-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-01',8,1000000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-01' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-2-2',@payer,1000000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-2-2','confirmed',1000000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1000000,'cash','paid',N'DEMO-SC-SOC-2-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-2-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-2-2',@staff,N'[DEMO] Khách mẫu',260000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,260000,260000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,260000,'cash','paid',N'DEMO-SC-GOODS-2-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-01T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-3-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-02',8,1080000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-02' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-3-2',@payer,1080000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-3-2','confirmed',1080000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1080000,'cash','paid',N'DEMO-SC-SOC-3-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-3-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-3-2',@staff,N'[DEMO] Khách mẫu',280000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,280000,280000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,280000,'cash','paid',N'DEMO-SC-GOODS-3-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-02T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-4-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1160000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-4-2',@payer,1160000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-4-2','confirmed',1160000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1160000,'cash','paid',N'DEMO-SC-SOC-4-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-4-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-4-2',@staff,N'[DEMO] Khách mẫu',300000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,300000,300000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,300000,'cash','paid',N'DEMO-SC-GOODS-4-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-5-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-04',8,1240000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-04' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-5-2',@payer,1240000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-5-2','confirmed',1240000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1240000,'cash','paid',N'DEMO-SC-SOC-5-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-5-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-5-2',@staff,N'[DEMO] Khách mẫu',320000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,320000,320000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,320000,'cash','paid',N'DEMO-SC-GOODS-5-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-04T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-SOC-6-2') BEGIN
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-05',8,1320000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-05' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-SOC-6-2',@payer,1320000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-SOC-6-2','confirmed',1320000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1320000,'cash','paid',N'DEMO-SC-SOC-6-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T18:15:00+07:00',note=N'[DEMO] Thu vé buổi chơi',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-GOODS-6-2') BEGIN
INSERT dbo.orders(order_code,staff_id,customer_name,total_amount,status,payment_state,handover_state,venue_id) VALUES(N'DEMO-SC-GOODS-6-2',@staff,N'[DEMO] Khách mẫu',340000,'Paid','Paid','Pending',@venue); SET @order=SCOPE_IDENTITY();
INSERT dbo.order_items(order_id,product_id,quantity,unit_price_vnd,subtotal_vnd) VALUES(@order,@product,1,340000,340000);
INSERT dbo.payments(goods_order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,340000,'cash','paid',N'DEMO-SC-GOODS-6-2',@staff); SET @payment=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T20:30:00+07:00',note=N'[DEMO] Bán cầu, bóng và nước',event_code=NULL WHERE payment_id=@payment AND refund_id IS NULL;
END;
IF NOT EXISTS(SELECT 1 FROM dbo.payments WHERE reference_code=N'DEMO-SC-CUP-02') BEGIN
INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,N'DEMO-SC-CUP-02',@sport,1); SET @court=SCOPE_IDENTITY();
IF NOT EXISTS(SELECT 1 FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot)
INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,'2026-11-03',8,1600000,'Completed',@staff);
SELECT @session=id FROM dbo.sessions WHERE court_id=@court AND session_date='2026-11-03' AND time_slot_id=@slot;
INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status) VALUES(N'DEMO-SC-CUP-02',@payer,1600000,'paid'); SET @order=SCOPE_IDENTITY();
INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd) VALUES(@order,@session,@player,N'DEMO-SC-CUP-02','confirmed',1600000);
INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by) VALUES(@order,@payer,1600000,'cash','paid',N'DEMO-SC-CUP-02',@staff); SET @payment=SCOPE_IDENTITY();
SELECT @court=id FROM dbo.courts WHERE venue_id=@venue AND name='DEMO-SC-Court';
UPDATE dbo.revenue_entries SET recognized_at='2026-11-03T08:05:00+07:00',note=N'[DEMO] Giải giao hữu liên cơ sở · phần thu Quận 7',event_code=N'CUP-DEMO' WHERE payment_id=@payment AND refund_id IS NULL;
END;
SELECT @payment=p.id,@ticket=t.id FROM dbo.payments p JOIN dbo.tickets t ON t.order_id=p.order_id WHERE p.reference_code=N'DEMO-SC-SOC-5-2';
IF NOT EXISTS(SELECT 1 FROM dbo.refunds WHERE payment_id=@payment AND ticket_id=@ticket AND initiated_by=@staff) BEGIN
INSERT dbo.refunds(payment_id,ticket_id,recipient_id,amount_vnd,status,initiated_by) VALUES(@payment,@ticket,@payer,80000,'requested',@staff); SET @refund=SCOPE_IDENTITY();
UPDATE dbo.revenue_entries SET recognized_at='2026-11-05T11:00:00+07:00',note=N'[DEMO] Yêu cầu hoàn tiền, chưa chi' WHERE refund_id=@refund;
END;
COMMIT;
SELECT v.name,e.entry_type,SUM(e.amount_vnd) total_vnd FROM dbo.revenue_entries e JOIN dbo.venues v ON v.id=e.venue_id WHERE v.name LIKE '[[]DEMO-SC]%' GROUP BY v.name,e.entry_type ORDER BY v.name,e.entry_type;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
