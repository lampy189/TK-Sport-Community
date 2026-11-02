-- Approved test fixture, 30 Oct 2026. Run after upgrade 005 in local Sep490 only.
-- Reuses four demo players and referee@tksc.local. No real payment is made.
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource=N'TKSC:skill-test-20261030',
        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
    IF @lock < 0 THROW 51000, 'Test fixture is busy.', 1;
    DECLARE @date date = '20261030';
    DECLARE @referee bigint = (SELECT u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
        WHERE u.email='referee@tksc.local' AND r.code='Referee' AND u.status='Active' AND u.deleted_at IS NULL);
    DECLARE @manager bigint = (SELECT u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
        WHERE u.email='manager@tksc.local' AND r.code='Manager' AND u.status='Active' AND u.deleted_at IS NULL);
    DECLARE @sport bigint = (SELECT id FROM dbo.sports WHERE code='badminton');
    DECLARE @court bigint = (SELECT id FROM dbo.courts WHERE id=1 AND sport_id=@sport AND is_active=1);
    DECLARE @slot bigint = (SELECT MIN(id) FROM dbo.time_slots WHERE start_time='16:00' AND end_time='18:00');
    IF @referee IS NULL OR @manager IS NULL OR @court IS NULL OR @slot IS NULL
        THROW 51000, 'Required referee, manager, court or slot is missing.', 1;
    DECLARE @players TABLE(player_id bigint PRIMARY KEY, user_id bigint, email nvarchar(320));
    INSERT @players SELECT p.id,u.id,u.email FROM dbo.players p JOIN dbo.users u ON u.id=p.user_id
        JOIN dbo.roles r ON r.id=u.role_id
        WHERE u.email IN ('player01@demo.tksc.invalid','player02@demo.tksc.invalid',
            'player03@demo.tksc.invalid','player04@demo.tksc.invalid')
        AND r.code='Player' AND u.status='Active' AND u.deleted_at IS NULL;
    IF (SELECT COUNT(*) FROM @players) <> 4 THROW 51000, 'Expected four active demo players.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.rubrics WHERE sport_id=@sport AND version='badminton-pilot-r1' AND is_active=1)
        THROW 51000, 'Apply the approved badminton rubric first.', 1;
    DECLARE @orderCode varchar(100) = 'TEST-SKILL-20261030';
    DECLARE @order bigint = (SELECT id FROM dbo.social_booking_orders WHERE order_code=@orderCode);
    DECLARE @session bigint;
    IF @order IS NULL
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.sessions s JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            WHERE s.session_date=@date AND s.status <> 'cancelled' AND ts.start_time < '18:00' AND ts.end_time > '16:00'
            AND (s.court_id=@court OR EXISTS(SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=s.id AND sr.referee_id=@referee)))
            THROW 51000, 'Court or referee already assigned during this test slot.', 1;
        IF EXISTS(SELECT 1 FROM dbo.tickets t JOIN @players p ON p.player_id=t.player_id
            JOIN dbo.sessions s ON s.id=t.session_id JOIN dbo.time_slots ts ON ts.id=s.time_slot_id
            WHERE s.session_date=@date AND ts.start_time < '18:00' AND ts.end_time > '16:00'
                AND t.status IN ('confirmed','checkedin'))
            THROW 51000, 'A demo player already has a ticket during this test slot.', 1;
        INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
        VALUES(@sport,@court,@slot,@date,NULL,8,100000,'Published',@manager);
        SET @session=SCOPE_IDENTITY();
        INSERT dbo.session_referees(session_id,referee_id,assigned_by) VALUES(@session,@referee,@manager);
        INSERT dbo.player_sport_profiles(player_id,sport_id,rating_status)
        SELECT p.player_id,@sport,'Unrated' FROM @players p
        WHERE NOT EXISTS(SELECT 1 FROM dbo.player_sport_profiles sp WHERE sp.player_id=p.player_id AND sp.sport_id=@sport);
        DECLARE @payer bigint = (SELECT user_id FROM @players WHERE email='player01@demo.tksc.invalid');
        INSERT dbo.social_booking_orders(order_code,payer_id,total_amount,status)
        VALUES(@orderCode,@payer,400000,'paid');
        SET @order=SCOPE_IDENTITY();
        INSERT dbo.payments(order_id,payer_id,amount_vnd,method,status,reference_code,confirmed_by)
        VALUES(@order,@payer,400000,'test_seed','paid','TEST-SKILL-20261030-PAYMENT',@manager);
        INSERT dbo.tickets(order_id,session_id,player_id,ticket_code,status,amount_vnd,checked_in_by)
        SELECT @order,@session,player_id,CONCAT('TEST-SKILL-20261030-',player_id),'checkedin',100000,@referee FROM @players;
        INSERT dbo.audit_logs(actor_id,action,entity_type,entity_id,new_value)
        VALUES(@manager,'test_fixture_created','Session',@session,
            N'Approved local test fixture: four demo players checked in; simulated paid order; no real transfer. 2026-10-30.');
    END
    ELSE
        SELECT @session=MIN(session_id) FROM dbo.tickets WHERE order_id=@order;
    COMMIT;
    SELECT @session session_id,@sport sport_id,@referee referee_id,@order order_id;
    SELECT t.id ticket_id,t.player_id,u.email,u.full_name,t.ticket_code,t.status,
        sp.id profile_id,sp.current_score,sp.current_band
    FROM dbo.tickets t JOIN dbo.players p ON p.id=t.player_id JOIN dbo.users u ON u.id=p.user_id
    JOIN dbo.player_sport_profiles sp ON sp.player_id=p.id AND sp.sport_id=@sport
    WHERE t.order_id=@order ORDER BY u.email;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
