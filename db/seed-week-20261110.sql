-- Explicit user request: extend both centers through Sunday and prepare Hyune for scoring.
-- Additive, rerunnable local Sep490 fixture. No payments, revenue or scores are fabricated.
SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; SET XACT_ABORT ON; SET NOCOUNT ON;
IF DB_NAME()<>N'Sep490' THROW 50000,'Select Sep490.',1;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @people TABLE(venue bigint,manager bigint,referee bigint,staff bigint);
 INSERT @people VALUES(1,10042,10044,10043),(2,4,5,6);
 IF EXISTS(SELECT 1 FROM @people p WHERE NOT EXISTS(SELECT 1 FROM users u JOIN roles r ON r.id=u.role_id WHERE u.id=p.manager AND u.venue_id=p.venue AND r.code='Manager')
 OR NOT EXISTS(SELECT 1 FROM users u JOIN roles r ON r.id=u.role_id WHERE u.id=p.referee AND u.venue_id=p.venue AND r.code='Referee')
 OR NOT EXISTS(SELECT 1 FROM users u JOIN roles r ON r.id=u.role_id WHERE u.id=p.staff AND u.venue_id=p.venue AND r.code='Staff'))
 THROW 50000,'Personnel mapping changed; review fixture.',1;
 DECLARE @added TABLE(id bigint);
 ;WITH days AS(SELECT CONVERT(date,'20261110') day UNION ALL SELECT DATEADD(day,1,day) FROM days WHERE day<'20261115'),
 schedule_plan AS(SELECT p.*,d.day,ts.id slot,c.id court,c.sport_id FROM @people p CROSS JOIN days d
 CROSS JOIN dbo.time_slots ts CROSS APPLY(SELECT TOP(1) c.id,c.sport_id FROM dbo.courts c JOIN sports sp ON sp.id=c.sport_id
 WHERE c.venue_id=p.venue AND c.is_active=1 AND sp.code=CASE WHEN ts.start_time IN ('14:00','18:00') THEN 'badminton' ELSE 'pickleball' END ORDER BY c.id)c
 WHERE ts.start_time IN ('14:00','16:00','18:00','20:00') AND DATEDIFF(minute,ts.start_time,ts.end_time)=120)
 INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
 OUTPUT inserted.id INTO @added
 SELECT p.sport_id,p.court,p.slot,p.day,NULL,8,80000,'Published',p.manager FROM schedule_plan p
 JOIN time_slots target ON target.id=p.slot
 WHERE NOT EXISTS(SELECT 1 FROM sessions s JOIN time_slots t ON t.id=s.time_slot_id WHERE s.court_id=p.court AND s.session_date=p.day AND t.start_time<target.end_time AND t.end_time>target.start_time)
 AND NOT EXISTS(SELECT 1 FROM session_referees sr JOIN sessions s ON s.id=sr.session_id JOIN time_slots t ON t.id=s.time_slot_id WHERE sr.referee_id=p.referee AND s.session_date=p.day AND s.status<>'cancelled' AND t.start_time<target.end_time AND t.end_time>target.start_time);
 INSERT session_referees(session_id,referee_id,assigned_by)
 SELECT a.id,p.referee,p.manager FROM @added a JOIN sessions s ON s.id=a.id JOIN courts c ON c.id=s.court_id JOIN @people p ON p.venue=c.venue_id;
 INSERT session_staff(session_id,staff_id,role_in_session)
 SELECT a.id,p.staff,N'Vận hành' FROM @added a JOIN sessions s ON s.id=a.id JOIN courts c ON c.id=s.court_id JOIN @people p ON p.venue=c.venue_id;

 DECLARE @player bigint=(SELECT p.id FROM players p JOIN users u ON u.id=p.user_id WHERE u.id=3 AND u.full_name=N'Hyune'),
 @testSession bigint,@order bigint,@court bigint=(SELECT MIN(c.id) FROM courts c JOIN sports sp ON sp.id=c.sport_id WHERE c.venue_id=2 AND c.is_active=1 AND sp.code='badminton'),
 @slot bigint=(SELECT MIN(id) FROM time_slots WHERE start_time='10:00' AND end_time='12:00');
 IF @player IS NULL OR @court IS NULL OR @slot IS NULL THROW 50000,'Hyune or test court/slot missing.',1;
 SELECT @testSession=id FROM sessions WHERE court_id=@court AND session_date='20261110' AND time_slot_id=@slot;
 IF @testSession IS NULL
 BEGIN
  IF EXISTS(SELECT 1 FROM session_referees sr JOIN sessions s ON s.id=sr.session_id JOIN time_slots ts ON ts.id=s.time_slot_id WHERE sr.referee_id=5 AND s.session_date='20261110' AND ts.start_time<'12:00' AND ts.end_time>'10:00' AND s.status<>'cancelled') THROW 50000,'Test referee has a conflict.',1;
  INSERT sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
  SELECT sport_id,id,@slot,'20261110',NULL,8,80000,'Published',4 FROM courts WHERE id=@court;
  SET @testSession=SCOPE_IDENTITY();
 END;
 IF NOT EXISTS(SELECT 1 FROM session_referees WHERE session_id=@testSession AND referee_id=5) INSERT session_referees(session_id,referee_id,assigned_by)VALUES(@testSession,5,4);
 IF NOT EXISTS(SELECT 1 FROM session_staff WHERE session_id=@testSession AND staff_id=6) INSERT session_staff(session_id,staff_id,role_in_session)VALUES(@testSession,6,N'Test chấm điểm');
 IF NOT EXISTS(SELECT 1 FROM player_sport_profiles WHERE player_id=@player AND sport_id=(SELECT sport_id FROM sessions WHERE id=@testSession)) THROW 50000,'Hyune needs the sport profile first.',1;
 IF NOT EXISTS(SELECT 1 FROM tickets WHERE player_id=@player AND session_id=@testSession)
 BEGIN
  IF (SELECT COUNT(*) FROM tickets WHERE session_id=@testSession AND status IN('confirmed','checkedin'))>=8 THROW 50000,'Test session full.',1;
  IF EXISTS(SELECT 1 FROM social_booking_orders WHERE order_code='TEST-HYUNE-SCORING-20261110') THROW 50000,'Fixture order exists without expected ticket; review.',1;
  INSERT social_booking_orders(order_code,payer_id,total_amount,status,expires_at)VALUES('TEST-HYUNE-SCORING-20261110',@player,0,'paid',NULL);
  SET @order=SCOPE_IDENTITY();
  INSERT tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd,checked_in_by)
  VALUES(@order,@testSession,@player,'TEST-HYUNE-SCORING-20261110','checkedin',0,5);
  INSERT audit_logs(actor_id,action,entity_type,entity_id,new_value)
  VALUES(4,'demo_scoring_attendance_seeded','Ticket',SCOPE_IDENTITY(),N'User-requested local test fixture: Hyune, zero-charge settled order, checked in for scoring; no payment or rating created.');
 END;
 SELECT COUNT(*) added_regular_sessions FROM @added;
 SELECT @testSession scoring_session_id,@player player_id,5 referee_user_id;
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
 THROW;
END CATCH;
EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;
