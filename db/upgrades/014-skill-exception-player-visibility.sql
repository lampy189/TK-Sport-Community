-- UC-15 (UC-19 in task assignment): review a skill exception BEFORE a ticket exists.
-- Add narrowly scoped read paths for Manager/Referee in the request's Sport Center.
-- Existing ticket-based access remains unchanged; Staff gains no exception-only access.
-- No rows, decisions or ratings are changed. The existing policy stays enabled.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select Sep490 before applying upgrade 014.', 1;
IF NOT EXISTS(SELECT 1 FROM sys.security_policies WHERE object_id=OBJECT_ID(N'dbo.SportCenterIsolation') AND is_enabled=1)
 THROW 50001, 'Apply enabled SportCenterIsolation upgrades 011 and 013 first.', 1;

BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID(N'dbo.sc_player_exception',N'IF') IS NULL
 EXEC(N'CREATE FUNCTION dbo.sc_player_exception(@id bigint)
 RETURNS TABLE WITH SCHEMABINDING AS RETURN
 SELECT 1 AS allowed
 WHERE EXISTS(SELECT 1 FROM dbo.sc_player(@id))
 OR (
  CONVERT(nvarchar(30),SESSION_CONTEXT(N''tksc_role'')) IN (N''Manager'',N''manager'',N''Referee'',N''referee'')
  AND EXISTS(SELECT 1 FROM dbo.session_exceptions e
   CROSS APPLY dbo.sc_session(e.session_id) allowed_scope
   WHERE e.player_id=@id)
 );');

 IF OBJECT_ID(N'dbo.sc_profile_exception',N'IF') IS NULL
 EXEC(N'CREATE FUNCTION dbo.sc_profile_exception(@id bigint)
 RETURNS TABLE WITH SCHEMABINDING AS RETURN
 SELECT 1 AS allowed
 WHERE EXISTS(SELECT 1 FROM dbo.player_sport_profiles p
  CROSS APPLY dbo.sc_player_exception(p.player_id) allowed_scope
  WHERE p.id=@id);');

 IF OBJECT_ID(N'dbo.sc_user_exception',N'IF') IS NULL
 EXEC(N'CREATE FUNCTION dbo.sc_user_exception(@id bigint,@venue bigint)
 RETURNS TABLE WITH SCHEMABINDING AS RETURN
 SELECT 1 AS allowed
 WHERE EXISTS(SELECT 1 FROM dbo.sc_venue(@venue))
 OR EXISTS(SELECT 1 FROM dbo.players p
  CROSS APPLY dbo.sc_player_exception(p.id) allowed_scope
  WHERE p.user_id=@id);');

 -- Compile the policy switch after the new functions have been created.
 EXEC(N'ALTER SECURITY POLICY dbo.SportCenterIsolation
  ALTER FILTER PREDICATE dbo.sc_player_exception(id) ON dbo.players,
  ALTER FILTER PREDICATE dbo.sc_player_exception(player_id) ON dbo.player_sport_profiles,
  ALTER BLOCK PREDICATE dbo.sc_player_exception(player_id) ON dbo.player_sport_profiles AFTER INSERT,
  ALTER BLOCK PREDICATE dbo.sc_player_exception(player_id) ON dbo.player_sport_profiles AFTER UPDATE,
  ALTER FILTER PREDICATE dbo.sc_profile_exception(player_sport_profile_id) ON dbo.player_rating_history,
  ALTER BLOCK PREDICATE dbo.sc_profile_exception(player_sport_profile_id) ON dbo.player_rating_history AFTER INSERT,
  ALTER BLOCK PREDICATE dbo.sc_profile_exception(player_sport_profile_id) ON dbo.player_rating_history AFTER UPDATE,
  ALTER FILTER PREDICATE dbo.sc_user_exception(id,venue_id) ON dbo.users;');
 COMMIT;
END TRY
BEGIN CATCH
 IF XACT_STATE() <> 0 ROLLBACK;
 THROW;
END CATCH;
