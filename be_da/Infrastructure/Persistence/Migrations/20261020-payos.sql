-- Apply to the existing Sep490 database before enabling payment API.
-- Additive only; no changes to existing orders or payments.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('payments', 'gateway_payment_link_id') IS NULL
    ALTER TABLE payments ADD gateway_payment_link_id varchar(100) NULL;
IF COL_LENGTH('payments', 'checkout_url') IS NULL
    ALTER TABLE payments ADD checkout_url nvarchar(2048) NULL;
IF COL_LENGTH('bank_transactions', 'raw_payload') IS NULL
    ALTER TABLE bank_transactions ADD raw_payload nvarchar(max) NULL;
COMMIT;
