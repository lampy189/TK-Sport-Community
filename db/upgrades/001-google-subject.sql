-- Approved additive upgrade for Google sign-in. Run explicitly; never on application startup.
-- sqlcmd -S localhost -d Sep490 -E -C -b -i db/upgrades/001-google-subject.sql
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;
IF OBJECT_ID(N'dbo.users', N'U') IS NULL THROW 50000, 'Baseline users table is missing.', 1;
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.users', N'google_subject') IS NULL
    ALTER TABLE dbo.users ADD google_subject VARCHAR(255) COLLATE Latin1_General_100_BIN2 NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.users') AND name=N'UX_users_google_subject')
    EXEC(N'CREATE UNIQUE INDEX UX_users_google_subject ON dbo.users(google_subject) WHERE google_subject IS NOT NULL;');
COMMIT;
