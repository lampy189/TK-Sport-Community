-- Local assessment fixtures requested for Nguyen Van Lam (K18 HL).
-- Payment is simulated (test_seed); no actual money is received.
USE [Sep490];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'TKSC-lam-assessment-nov03-04',
  @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
 IF @lock<0 THROW 51000,'Fixture is busy.',1;
 DECLARE @user bigint,@player bigint,@referee bigint,@sport bigint;
 SELECT @user=u.id,@player=p.id FROM dbo.users u JOIN dbo.players p ON p.user_id=u.id
 WHERE u.email='lamnvhe181229@fpt.edu.vn' AND u.status='Active';
 SELECT @referee=u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
 WHERE u.email='referee@tksc.local' AND r.code='Referee';
 SELECT @sport=id FROM dbo.sports WHERE code='pickleball';
 IF @player IS NULL OR @referee IS NULL OR @sport IS NULL THROW 51001,'Required accounts/sport missing.',1;
 IF (SELECT COUNT(*) FROM dbo.rubrics WHERE sport_id=@sport AND is_active=1)<>1
  THROW 51002,'Exactly one active rubric required.',1;
 DECLARE @targets TABLE(session_id bigint,code varchar(100));
 INSERT @targets
 SELECT s.id,CONCAT('TEST-LAM-SKILL-',CONVERT(char(8),s.session_date,112))
 FROM dbo.sessions s JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
 JOIN dbo.courts c ON c.id=s.court_id
 WHERE s.sport_id=@sport AND c.name='Court 2 - Pickleball' AND s.status='Published'
 AND ((s.session_date='20261103' AND ts.start_time='16:00') OR (s.session_date='20261104' AND ts.start_time='08:00'))
 AND EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id AND sr.referee_id=@referee);
 IF (SELECT COUNT(*) FROM @targets)<>2 THROW 51003,'Expected two assigned sessions.',1;
 INSERT dbo.player_sport_profiles(player_id,sport_id,rating_status)
 SELECT @player,@sport,'Unrated' WHERE NOT EXISTS
 (SELECT 1 FROM dbo.player_sport_profiles WHERE player_id=@player AND sport_id=@sport);
 DECLARE @session bigint,@code varchar(100),@order bigint,@price int;
 DECLARE fixtures CURSOR LOCAL FAST_FORWARD FOR SELECT session_id,code FROM @targets;
 OPEN fixtures;
 FETCH NEXT FROM fixtures INTO @session,@code;
 WHILE @@FETCH_STATUS=0
 BEGIN
  IF NOT EXISTS(SELECT 1 FROM dbo.social_booking_orders WHERE order_code=@code)
  BEGIN
   IF EXISTS(SELECT 1 FROM dbo.sessions s WHERE s.id=@session AND s.capacity<=
    (SELECT COUNT(*) FROM dbo.tickets t WHERE t.session_id=s.id AND t.status NOT IN ('cancelled','expired','refunded')))
    THROW 51004,'Session is full.',1;
   IF EXISTS(SELECT 1 FROM dbo.tickets t JOIN dbo.sessions s ON s.id=t.session_id
    JOIN dbo.time_slots ts ON ts.id=s.time_slot_id JOIN dbo.sessions target ON target.id=@session
    JOIN dbo.time_slots slot ON slot.id=target.time_slot_id
    WHERE t.player_id=@player AND t.status NOT IN ('cancelled','expired','refunded')
    AND s.session_date=target.session_date AND ts.start_time<slot.end_time AND ts.end_time>slot.start_time)
    THROW 51005,'Player has an overlapping ticket.',1;
   SELECT @price=price_vnd FROM dbo.sessions WHERE id=@session;
   INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status)
   VALUES(@code,@user,@price,'paid');
   SET @order=SCOPE_IDENTITY();
   INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by)
   VALUES(@order,@user,@price,'test_seed','paid',CONCAT(@code,'-PAY'),@referee);
   INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd,checked_in_by)
   VALUES(@order,@session,@player,@code,'checkedin',@price,@referee);
   INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value)
   VALUES(@referee,'test_fixture_created','Ticket',SCOPE_IDENTITY(),
    N'User-requested local assessment fixture: simulated paid ticket and check-in; no real payment; existing skill ratings preserved.');
  END;
  FETCH NEXT FROM fixtures INTO @session,@code;
 END;
 CLOSE fixtures;
 DEALLOCATE fixtures;
 COMMIT;
 SELECT t.id,t.session_id,t.status,t.checked_in_by,o.status order_status
 FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
 JOIN @targets x ON x.code=o.order_code WHERE t.player_id=@player;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
