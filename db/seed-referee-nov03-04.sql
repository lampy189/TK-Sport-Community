-- Assign existing sample sessions on 3-4 November 2026 to the local referee.
USE [Sep490];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock = sys.sp_getapplock @Resource = N'TKSC-referee-nov03-04',
        @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
    IF @lock < 0 THROW 50001, 'Cannot acquire seed lock.', 1;
    DECLARE @referee bigint = (SELECT u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
        WHERE u.email=N'referee@tksc.local' AND r.code='Referee' AND u.status='Active');
    DECLARE @manager bigint = (SELECT TOP (1) u.id FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id
        WHERE r.code='Manager' AND u.status='Active' ORDER BY u.id);
    IF @referee IS NULL OR @manager IS NULL THROW 50002, 'Active referee and manager required.', 1;
    DECLARE @wanted TABLE (day date, court varchar(100), starts time);
    INSERT @wanted VALUES
        ('20261103','Court 2 - Pickleball','10:00'),
        ('20261103','Court 2 - Pickleball','16:00'),
        ('20261103','Court 1 - Badminton','18:00'),
        ('20261103','Court 1 - Badminton','20:00'),
        ('20261104','Court 2 - Pickleball','08:00'),
        ('20261104','Court 1 - Badminton','14:00'),
        ('20261104','Court 1 - Badminton','18:00'),
        ('20261104','Court 1 - Badminton','20:00');
    DECLARE @targets TABLE (id bigint, day date, starts time, ends time);
    INSERT @targets
    SELECT s.id,s.session_date,t.start_time,t.end_time FROM dbo.sessions s
    JOIN dbo.courts c ON c.id=s.court_id AND c.is_active=1
    JOIN dbo.time_slots t ON t.id=s.time_slot_id
    JOIN @wanted w ON w.day=s.session_date AND w.court=c.name AND w.starts=t.start_time
    WHERE s.status='Published';
    IF (SELECT COUNT(*) FROM @targets) <> 8 THROW 50003, 'Expected eight published sample sessions.', 1;
    IF EXISTS (SELECT 1 FROM @targets x JOIN dbo.sessions s ON s.session_date=x.day AND s.id<>x.id
        JOIN dbo.time_slots t ON t.id=s.time_slot_id
        JOIN dbo.session_referees sr ON sr.session_id=s.id AND sr.referee_id=@referee
        WHERE s.status NOT IN ('Cancelled','Canceled') AND t.start_time<x.ends AND t.end_time>x.starts)
        THROW 50004, 'Referee has an overlapping assignment.', 1;
    INSERT dbo.session_referees(session_id,referee_id,assigned_by)
    SELECT x.id,@referee,@manager FROM @targets x
    WHERE NOT EXISTS (SELECT 1 FROM dbo.session_referees sr WHERE sr.session_id=x.id AND sr.referee_id=@referee);
    SELECT @@ROWCOUNT AS inserted_assignments;
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK;
    THROW;
END CATCH;
