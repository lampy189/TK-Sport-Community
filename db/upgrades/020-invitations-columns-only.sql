-- Source: dbdoan_moi_20261114.sql. Add missing columns only; no imported data.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'Sep490' THROW 51000, 'Expected database Sep490.', 1;
IF OBJECT_ID(N'dbo.invitations', N'U') IS NULL
    THROW 51000, 'Expected existing dbo.invitations table.', 1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.invitations') AND name=N'payer_user_id')
    ALTER TABLE dbo.invitations ADD payer_user_id bigint NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.invitations') AND name=N'expires_at')
    ALTER TABLE dbo.invitations ADD expires_at datetimeoffset(0) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.invitations') AND name=N'created_at')
    ALTER TABLE dbo.invitations ADD created_at datetimeoffset(0) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.invitations') AND name=N'responded_at')
    ALTER TABLE dbo.invitations ADD responded_at datetimeoffset(0) NULL;
COMMIT;
