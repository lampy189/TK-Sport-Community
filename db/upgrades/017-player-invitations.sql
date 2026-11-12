-- UC-09/10/11. Additive upgrade; does not reserve, charge or modify existing tickets.
-- Back up the target database before applying. No automatic startup migration.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.invitations','payer_user_id') IS NULL
    ALTER TABLE dbo.invitations ADD payer_user_id bigint NULL REFERENCES dbo.users(id);
IF COL_LENGTH('dbo.invitations','expires_at') IS NULL
    ALTER TABLE dbo.invitations ADD expires_at datetimeoffset(0) NULL;
IF COL_LENGTH('dbo.invitations','created_at') IS NULL
    ALTER TABLE dbo.invitations ADD created_at datetimeoffset(0) NULL;
IF COL_LENGTH('dbo.invitations','responded_at') IS NULL
    ALTER TABLE dbo.invitations ADD responded_at datetimeoffset(0) NULL;
-- Historical rows without explicit payment consent remain read-only and cannot create a hold.
COMMIT;
