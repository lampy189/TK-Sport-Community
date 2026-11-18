-- Deploy together with application center authorization (11 Nov 2026).
-- Authorization is enforced by backend query filters and write checks, not SQL RLS.
-- Preserve accounting triggers/defaults and historical schema. No rows are changed.
SET XACT_ABORT ON;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select Sep490.', 1;
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'SportCenterIsolation' AND schema_id = SCHEMA_ID(N'dbo'))
    ALTER SECURITY POLICY dbo.SportCenterIsolation WITH (STATE = OFF);
