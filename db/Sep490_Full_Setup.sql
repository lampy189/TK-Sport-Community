-- =============================================================================
-- TKSC (TK Sport Community) - FULL DATABASE SETUP & SEED SCRIPT (Sep490)
-- Chi can mo file nay trong SSMS va bam Execute (F5) 1 lan duy nhat.
-- Script tu dong tao DB Sep490, 41 bang, 80 FK, Indexes va nap san du lieu.
-- =============================================================================

-- TKSC development schema from the supplied Mermaid ERD.
-- Install target: local SQL Server default instance; database Sep490.
-- Social booking orders and goods orders are separate.
IF DB_ID(N'Sep490') IS NULL CREATE DATABASE [Sep490];
GO
USE [Sep490];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.[roles]', N'U') IS NULL
CREATE TABLE dbo.[roles] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_roles] PRIMARY KEY,
    [code] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[users]', N'U') IS NULL
CREATE TABLE dbo.[users] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_users] PRIMARY KEY,
    [role_id] BIGINT NOT NULL,
    [email] NVARCHAR(320) NOT NULL,
    [full_name] NVARCHAR(255) NOT NULL,
    [password_hash] NVARCHAR(255) NULL,
    [status] VARCHAR(100) NOT NULL,
    [deleted_at] DATETIMEOFFSET(0) NULL,
    [google_subject] VARCHAR(255) COLLATE Latin1_General_100_BIN2 NULL,
    CONSTRAINT [CK_users_role_positive] CHECK ([role_id] > 0)
);
GO
IF OBJECT_ID(N'dbo.[account_audit_logs]', N'U') IS NULL
CREATE TABLE dbo.[account_audit_logs] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_account_audit_logs] PRIMARY KEY,
    [user_id] BIGINT NOT NULL,
    [actor_id] BIGINT NULL,
    [action] VARCHAR(100) NULL,
    [old_value] NVARCHAR(MAX) NULL,
    [new_value] NVARCHAR(MAX) NULL
);
GO
IF OBJECT_ID(N'dbo.[sports]', N'U') IS NULL
CREATE TABLE dbo.[sports] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_sports] PRIMARY KEY,
    [code] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[players]', N'U') IS NULL
CREATE TABLE dbo.[players] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_players] PRIMARY KEY,
    [user_id] BIGINT NOT NULL,
    [date_of_birth] DATE NULL,
    [gender] VARCHAR(100) NULL,
    [avatar_url] VARCHAR(2048) NULL
);
GO
IF OBJECT_ID(N'dbo.[player_sport_profiles]', N'U') IS NULL
CREATE TABLE dbo.[player_sport_profiles] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_player_sport_profiles] PRIMARY KEY,
    [player_id] BIGINT NOT NULL,
    [sport_id] BIGINT NOT NULL,
    [current_score] SMALLINT NULL,
    [current_band] VARCHAR(100) NULL,
    [rating_status] VARCHAR(100) NULL,
    CONSTRAINT [CK_profiles_score] CHECK ([current_score] BETWEEN 0 AND 100)
);
GO
IF OBJECT_ID(N'dbo.[player_rating_history]', N'U') IS NULL
CREATE TABLE dbo.[player_rating_history] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_player_rating_history] PRIMARY KEY,
    [player_sport_profile_id] BIGINT NOT NULL,
    [old_score] SMALLINT NULL,
    [new_score] SMALLINT NULL,
    [old_band] VARCHAR(100) NULL,
    [new_band] VARCHAR(100) NULL,
    [changed_by] BIGINT NOT NULL,
    [assessment_id] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[rubrics]', N'U') IS NULL
CREATE TABLE dbo.[rubrics] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_rubrics] PRIMARY KEY,
    [sport_id] BIGINT NOT NULL,
    [version] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL,
    [is_active] BIT NULL
);
GO
IF OBJECT_ID(N'dbo.[rubric_dimensions]', N'U') IS NULL
CREATE TABLE dbo.[rubric_dimensions] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_rubric_dimensions] PRIMARY KEY,
    [rubric_id] BIGINT NOT NULL,
    [code] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL,
    [weight_percent] SMALLINT NULL
);
GO
IF OBJECT_ID(N'dbo.[sessions]', N'U') IS NULL
CREATE TABLE dbo.[sessions] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_sessions] PRIMARY KEY,
    [sport_id] BIGINT NOT NULL,
    [court_id] BIGINT NOT NULL,
    [time_slot_id] BIGINT NOT NULL,
    [session_date] DATE NOT NULL,
    [band] VARCHAR(100) NULL,
    [capacity] SMALLINT NOT NULL,
    [price_vnd] INT NOT NULL,
    [status] VARCHAR(100) NOT NULL,
    [created_by] BIGINT NOT NULL,
    CONSTRAINT [CK_sessions_capacity] CHECK ([capacity] BETWEEN 1 AND 8)
);
GO
IF OBJECT_ID(N'dbo.[assessments]', N'U') IS NULL
CREATE TABLE dbo.[assessments] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_assessments] PRIMARY KEY,
    [player_sport_profile_id] BIGINT NOT NULL,
    [session_id] BIGINT NULL,
    [rubric_id] BIGINT NOT NULL,
    [assessment_type] VARCHAR(100) NULL,
    [assessor_id] BIGINT NOT NULL,
    [status] VARCHAR(100) NULL,
    [total_score] SMALLINT NULL,
    [band_result] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[assessment_scores]', N'U') IS NULL
CREATE TABLE dbo.[assessment_scores] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_assessment_scores] PRIMARY KEY,
    [assessment_id] BIGINT NOT NULL,
    [dimension_id] BIGINT NOT NULL,
    [score] SMALLINT NULL
);
GO
IF OBJECT_ID(N'dbo.[rating_appeals]', N'U') IS NULL
CREATE TABLE dbo.[rating_appeals] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_rating_appeals] PRIMARY KEY,
    [player_sport_profile_id] BIGINT NOT NULL,
    [submitted_by] BIGINT NOT NULL,
    [assessment_id] BIGINT NULL,
    [status] VARCHAR(100) NULL,
    [reviewed_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[venues]', N'U') IS NULL
CREATE TABLE dbo.[venues] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_venues] PRIMARY KEY,
    [name] NVARCHAR(255) NULL,
    [address] NVARCHAR(500) NULL,
    [is_active] BIT NULL
);
GO
IF OBJECT_ID(N'dbo.[courts]', N'U') IS NULL
CREATE TABLE dbo.[courts] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_courts] PRIMARY KEY,
    [venue_id] BIGINT NOT NULL,
    [name] NVARCHAR(255) NULL,
    [sport_id] BIGINT NULL,
    [is_active] BIT NULL
);
GO
IF OBJECT_ID(N'dbo.[time_slots]', N'U') IS NULL
CREATE TABLE dbo.[time_slots] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_time_slots] PRIMARY KEY,
    [start_time] TIME(0) NOT NULL,
    [end_time] TIME(0) NOT NULL,
    [label] NVARCHAR(255) NULL
);
GO
IF OBJECT_ID(N'dbo.[session_referees]', N'U') IS NULL
CREATE TABLE dbo.[session_referees] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_session_referees] PRIMARY KEY,
    [session_id] BIGINT NOT NULL,
    [referee_id] BIGINT NOT NULL,
    [assigned_by] BIGINT NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[session_staff]', N'U') IS NULL
CREATE TABLE dbo.[session_staff] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_session_staff] PRIMARY KEY,
    [session_id] BIGINT NOT NULL,
    [staff_id] BIGINT NOT NULL,
    [role_in_session] NVARCHAR(255) NULL
);
GO
IF OBJECT_ID(N'dbo.[session_exceptions]', N'U') IS NULL
CREATE TABLE dbo.[session_exceptions] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_session_exceptions] PRIMARY KEY,
    [session_id] BIGINT NOT NULL,
    [player_id] BIGINT NOT NULL,
    [approved_by] BIGINT NULL,
    [reason] NVARCHAR(255) NULL
);
GO
IF OBJECT_ID(N'dbo.[social_booking_orders]', N'U') IS NULL
CREATE TABLE dbo.[social_booking_orders] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_social_booking_orders] PRIMARY KEY,
    [order_code] VARCHAR(100) NOT NULL,
    [payer_id] BIGINT NOT NULL,
    [total_amount] INT NOT NULL,
    [status] VARCHAR(100) NOT NULL,
    [expires_at] DATETIMEOFFSET(0) NULL
);
GO
IF OBJECT_ID(N'dbo.[tickets]', N'U') IS NULL
CREATE TABLE dbo.[tickets] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_tickets] PRIMARY KEY,
    [order_id] BIGINT NOT NULL,
    [session_id] BIGINT NOT NULL,
    [player_id] BIGINT NOT NULL,
    [ticket_code] VARCHAR(100) NULL,
    [status] VARCHAR(100) NOT NULL,
    [amount_vnd] INT NOT NULL,
    [checked_in_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[invitations]', N'U') IS NULL
CREATE TABLE dbo.[invitations] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_invitations] PRIMARY KEY,
    [order_id] BIGINT NULL,
    [inviter_id] BIGINT NOT NULL,
    [invitee_id] BIGINT NOT NULL,
    [session_id] BIGINT NOT NULL,
    [ticket_id] BIGINT NULL,
    [status] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[partner_requests]', N'U') IS NULL
CREATE TABLE dbo.[partner_requests] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_partner_requests] PRIMARY KEY,
    [session_id] BIGINT NOT NULL,
    [requester_id] BIGINT NOT NULL,
    [partner_id] BIGINT NOT NULL,
    [status] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[orders]', N'U') IS NULL
CREATE TABLE dbo.[orders] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_orders] PRIMARY KEY,
    [order_code] VARCHAR(100) NOT NULL,
    [staff_id] BIGINT NOT NULL,
    [customer_name] NVARCHAR(255) NULL,
    [total_amount] INT NOT NULL,
    [status] VARCHAR(100) NOT NULL,
    [payment_state] VARCHAR(100) NULL,
    [handover_state] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[payments]', N'U') IS NULL
CREATE TABLE dbo.[payments] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_payments] PRIMARY KEY,
    [order_id] BIGINT NULL,
    [goods_order_id] BIGINT NULL,
    [payer_id] BIGINT NULL,
    [amount_vnd] INT NOT NULL,
    [method] VARCHAR(100) NULL,
    [status] VARCHAR(100) NOT NULL,
    [reference_code] VARCHAR(100) NOT NULL,
    [confirmed_by] BIGINT NULL,
    CONSTRAINT [CK_payments_one_order] CHECK ((CASE WHEN [order_id] IS NULL THEN 0 ELSE 1 END) + (CASE WHEN [goods_order_id] IS NULL THEN 0 ELSE 1 END) = 1)
);
GO
IF OBJECT_ID(N'dbo.[bank_transactions]', N'U') IS NULL
CREATE TABLE dbo.[bank_transactions] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_bank_transactions] PRIMARY KEY,
    [external_id] VARCHAR(200) NOT NULL,
    [amount_vnd] INT NOT NULL,
    [reference_code] VARCHAR(100) NULL,
    [matched_payment_id] BIGINT NULL,
    [match_status] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[refunds]', N'U') IS NULL
CREATE TABLE dbo.[refunds] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_refunds] PRIMARY KEY,
    [payment_id] BIGINT NOT NULL,
    [ticket_id] BIGINT NOT NULL,
    [recipient_id] BIGINT NOT NULL,
    [amount_vnd] INT NULL,
    [status] VARCHAR(100) NULL,
    [initiated_by] BIGINT NOT NULL,
    [approved_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[products]', N'U') IS NULL
CREATE TABLE dbo.[products] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_products] PRIMARY KEY,
    [sku] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL,
    [category] VARCHAR(100) NULL,
    [unit_price_vnd] INT NOT NULL,
    [is_active] BIT NULL
);
GO
IF OBJECT_ID(N'dbo.[order_items]', N'U') IS NULL
CREATE TABLE dbo.[order_items] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_order_items] PRIMARY KEY,
    [order_id] BIGINT NOT NULL,
    [product_id] BIGINT NOT NULL,
    [quantity] SMALLINT NOT NULL,
    [unit_price_vnd] INT NOT NULL,
    [subtotal_vnd] INT NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[stock_items]', N'U') IS NULL
CREATE TABLE dbo.[stock_items] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_stock_items] PRIMARY KEY,
    [product_id] BIGINT NOT NULL,
    [quantity_on_hand] INT NOT NULL,
    [reorder_level] INT NOT NULL,
    CONSTRAINT [CK_stock_nonnegative] CHECK ([quantity_on_hand] >= 0 AND [reorder_level] >= 0)
);
GO
IF OBJECT_ID(N'dbo.[stock_movements]', N'U') IS NULL
CREATE TABLE dbo.[stock_movements] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_stock_movements] PRIMARY KEY,
    [stock_item_id] BIGINT NOT NULL,
    [movement_type] VARCHAR(100) NULL,
    [quantity] INT NOT NULL,
    [reference_type] VARCHAR(100) NULL,
    [reference_id] BIGINT NULL,
    [created_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[stock_counts]', N'U') IS NULL
CREATE TABLE dbo.[stock_counts] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_stock_counts] PRIMARY KEY,
    [stock_item_id] BIGINT NOT NULL,
    [counted_quantity] INT NOT NULL,
    [system_quantity] INT NULL,
    [difference] INT NULL,
    [counted_by] BIGINT NOT NULL,
    [approved_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[shift_reconciliations]', N'U') IS NULL
CREATE TABLE dbo.[shift_reconciliations] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_shift_reconciliations] PRIMARY KEY,
    [shift_date] DATE NULL,
    [staff_id] BIGINT NOT NULL,
    [expected_cash] INT NULL,
    [actual_cash] INT NULL,
    [expected_bank] INT NULL,
    [actual_bank] INT NULL,
    [difference] INT NULL,
    [status] VARCHAR(100) NULL,
    [approved_by] BIGINT NULL
);
GO
IF OBJECT_ID(N'dbo.[reconciliation_items]', N'U') IS NULL
CREATE TABLE dbo.[reconciliation_items] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_reconciliation_items] PRIMARY KEY,
    [reconciliation_id] BIGINT NOT NULL,
    [item_type] VARCHAR(100) NULL,
    [reference_id] BIGINT NULL,
    [expected_amount] INT NULL,
    [actual_amount] INT NULL,
    [difference] INT NULL
);
GO
IF OBJECT_ID(N'dbo.[policies]', N'U') IS NULL
CREATE TABLE dbo.[policies] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_policies] PRIMARY KEY,
    [code] VARCHAR(100) NOT NULL,
    [name] NVARCHAR(255) NOT NULL,
    [value] NVARCHAR(MAX) NULL,
    [approved_by] BIGINT NULL,
    [effective_from] DATETIMEOFFSET(0) NULL,
    [effective_to] DATETIMEOFFSET(0) NULL
);
GO
IF OBJECT_ID(N'dbo.[approvals]', N'U') IS NULL
CREATE TABLE dbo.[approvals] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_approvals] PRIMARY KEY,
    [entity_type] VARCHAR(100) NULL,
    [entity_id] BIGINT NULL,
    [requested_by] BIGINT NOT NULL,
    [approved_by] BIGINT NULL,
    [status] VARCHAR(100) NULL
);
GO
IF OBJECT_ID(N'dbo.[audit_logs]', N'U') IS NULL
CREATE TABLE dbo.[audit_logs] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_audit_logs] PRIMARY KEY,
    [actor_id] BIGINT NULL,
    [action] VARCHAR(100) NULL,
    [entity_type] VARCHAR(100) NULL,
    [entity_id] BIGINT NULL,
    [old_value] NVARCHAR(MAX) NULL,
    [new_value] NVARCHAR(MAX) NULL
);
GO
IF OBJECT_ID(N'dbo.[friendships]', N'U') IS NULL
CREATE TABLE dbo.[friendships] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_friendships] PRIMARY KEY,
    [requester_id] BIGINT NOT NULL,
    [addressee_id] BIGINT NOT NULL,
    [status] VARCHAR(100) NOT NULL
);
GO
IF OBJECT_ID(N'dbo.[messages]', N'U') IS NULL
CREATE TABLE dbo.[messages] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_messages] PRIMARY KEY,
    [sender_id] BIGINT NOT NULL,
    [recipient_id] BIGINT NOT NULL,
    [content] NVARCHAR(MAX) NULL,
    [is_read] BIT NULL
);
GO
IF OBJECT_ID(N'dbo.[notifications]', N'U') IS NULL
CREATE TABLE dbo.[notifications] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_notifications] PRIMARY KEY,
    [user_id] BIGINT NOT NULL,
    [channel] VARCHAR(100) NULL,
    [title] NVARCHAR(255) NULL,
    [reference_type] VARCHAR(100) NULL,
    [reference_id] BIGINT NULL,
    [status] VARCHAR(100) NULL
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_users_role_id')
    ALTER TABLE dbo.[users] ADD CONSTRAINT [FK_users_role_id] FOREIGN KEY ([role_id]) REFERENCES dbo.[roles]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_account_audit_logs_user_id')
    ALTER TABLE dbo.[account_audit_logs] ADD CONSTRAINT [FK_account_audit_logs_user_id] FOREIGN KEY ([user_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_account_audit_logs_actor_id')
    ALTER TABLE dbo.[account_audit_logs] ADD CONSTRAINT [FK_account_audit_logs_actor_id] FOREIGN KEY ([actor_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_players_user_id')
    ALTER TABLE dbo.[players] ADD CONSTRAINT [FK_players_user_id] FOREIGN KEY ([user_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_player_sport_profiles_player_id')
    ALTER TABLE dbo.[player_sport_profiles] ADD CONSTRAINT [FK_player_sport_profiles_player_id] FOREIGN KEY ([player_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_player_sport_profiles_sport_id')
    ALTER TABLE dbo.[player_sport_profiles] ADD CONSTRAINT [FK_player_sport_profiles_sport_id] FOREIGN KEY ([sport_id]) REFERENCES dbo.[sports]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_player_rating_history_player_sport_profile_id')
    ALTER TABLE dbo.[player_rating_history] ADD CONSTRAINT [FK_player_rating_history_player_sport_profile_id] FOREIGN KEY ([player_sport_profile_id]) REFERENCES dbo.[player_sport_profiles]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_player_rating_history_changed_by')
    ALTER TABLE dbo.[player_rating_history] ADD CONSTRAINT [FK_player_rating_history_changed_by] FOREIGN KEY ([changed_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_player_rating_history_assessment_id')
    ALTER TABLE dbo.[player_rating_history] ADD CONSTRAINT [FK_player_rating_history_assessment_id] FOREIGN KEY ([assessment_id]) REFERENCES dbo.[assessments]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rubrics_sport_id')
    ALTER TABLE dbo.[rubrics] ADD CONSTRAINT [FK_rubrics_sport_id] FOREIGN KEY ([sport_id]) REFERENCES dbo.[sports]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rubric_dimensions_rubric_id')
    ALTER TABLE dbo.[rubric_dimensions] ADD CONSTRAINT [FK_rubric_dimensions_rubric_id] FOREIGN KEY ([rubric_id]) REFERENCES dbo.[rubrics]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_sessions_sport_id')
    ALTER TABLE dbo.[sessions] ADD CONSTRAINT [FK_sessions_sport_id] FOREIGN KEY ([sport_id]) REFERENCES dbo.[sports]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_sessions_court_id')
    ALTER TABLE dbo.[sessions] ADD CONSTRAINT [FK_sessions_court_id] FOREIGN KEY ([court_id]) REFERENCES dbo.[courts]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_sessions_time_slot_id')
    ALTER TABLE dbo.[sessions] ADD CONSTRAINT [FK_sessions_time_slot_id] FOREIGN KEY ([time_slot_id]) REFERENCES dbo.[time_slots]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_sessions_created_by')
    ALTER TABLE dbo.[sessions] ADD CONSTRAINT [FK_sessions_created_by] FOREIGN KEY ([created_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessments_player_sport_profile_id')
    ALTER TABLE dbo.[assessments] ADD CONSTRAINT [FK_assessments_player_sport_profile_id] FOREIGN KEY ([player_sport_profile_id]) REFERENCES dbo.[player_sport_profiles]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessments_session_id')
    ALTER TABLE dbo.[assessments] ADD CONSTRAINT [FK_assessments_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessments_rubric_id')
    ALTER TABLE dbo.[assessments] ADD CONSTRAINT [FK_assessments_rubric_id] FOREIGN KEY ([rubric_id]) REFERENCES dbo.[rubrics]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessments_assessor_id')
    ALTER TABLE dbo.[assessments] ADD CONSTRAINT [FK_assessments_assessor_id] FOREIGN KEY ([assessor_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessment_scores_assessment_id')
    ALTER TABLE dbo.[assessment_scores] ADD CONSTRAINT [FK_assessment_scores_assessment_id] FOREIGN KEY ([assessment_id]) REFERENCES dbo.[assessments]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_assessment_scores_dimension_id')
    ALTER TABLE dbo.[assessment_scores] ADD CONSTRAINT [FK_assessment_scores_dimension_id] FOREIGN KEY ([dimension_id]) REFERENCES dbo.[rubric_dimensions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rating_appeals_player_sport_profile_id')
    ALTER TABLE dbo.[rating_appeals] ADD CONSTRAINT [FK_rating_appeals_player_sport_profile_id] FOREIGN KEY ([player_sport_profile_id]) REFERENCES dbo.[player_sport_profiles]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rating_appeals_submitted_by')
    ALTER TABLE dbo.[rating_appeals] ADD CONSTRAINT [FK_rating_appeals_submitted_by] FOREIGN KEY ([submitted_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rating_appeals_assessment_id')
    ALTER TABLE dbo.[rating_appeals] ADD CONSTRAINT [FK_rating_appeals_assessment_id] FOREIGN KEY ([assessment_id]) REFERENCES dbo.[assessments]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_rating_appeals_reviewed_by')
    ALTER TABLE dbo.[rating_appeals] ADD CONSTRAINT [FK_rating_appeals_reviewed_by] FOREIGN KEY ([reviewed_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_courts_venue_id')
    ALTER TABLE dbo.[courts] ADD CONSTRAINT [FK_courts_venue_id] FOREIGN KEY ([venue_id]) REFERENCES dbo.[venues]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_courts_sport_id')
    ALTER TABLE dbo.[courts] ADD CONSTRAINT [FK_courts_sport_id] FOREIGN KEY ([sport_id]) REFERENCES dbo.[sports]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_referees_session_id')
    ALTER TABLE dbo.[session_referees] ADD CONSTRAINT [FK_session_referees_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_referees_referee_id')
    ALTER TABLE dbo.[session_referees] ADD CONSTRAINT [FK_session_referees_referee_id] FOREIGN KEY ([referee_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_referees_assigned_by')
    ALTER TABLE dbo.[session_referees] ADD CONSTRAINT [FK_session_referees_assigned_by] FOREIGN KEY ([assigned_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_staff_session_id')
    ALTER TABLE dbo.[session_staff] ADD CONSTRAINT [FK_session_staff_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_staff_staff_id')
    ALTER TABLE dbo.[session_staff] ADD CONSTRAINT [FK_session_staff_staff_id] FOREIGN KEY ([staff_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_exceptions_session_id')
    ALTER TABLE dbo.[session_exceptions] ADD CONSTRAINT [FK_session_exceptions_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_exceptions_player_id')
    ALTER TABLE dbo.[session_exceptions] ADD CONSTRAINT [FK_session_exceptions_player_id] FOREIGN KEY ([player_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_session_exceptions_approved_by')
    ALTER TABLE dbo.[session_exceptions] ADD CONSTRAINT [FK_session_exceptions_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_social_booking_orders_payer_id')
    ALTER TABLE dbo.[social_booking_orders] ADD CONSTRAINT [FK_social_booking_orders_payer_id] FOREIGN KEY ([payer_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tickets_order_id')
    ALTER TABLE dbo.[tickets] ADD CONSTRAINT [FK_tickets_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[social_booking_orders]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tickets_session_id')
    ALTER TABLE dbo.[tickets] ADD CONSTRAINT [FK_tickets_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tickets_player_id')
    ALTER TABLE dbo.[tickets] ADD CONSTRAINT [FK_tickets_player_id] FOREIGN KEY ([player_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tickets_checked_in_by')
    ALTER TABLE dbo.[tickets] ADD CONSTRAINT [FK_tickets_checked_in_by] FOREIGN KEY ([checked_in_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_invitations_order_id')
    ALTER TABLE dbo.[invitations] ADD CONSTRAINT [FK_invitations_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[social_booking_orders]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_invitations_inviter_id')
    ALTER TABLE dbo.[invitations] ADD CONSTRAINT [FK_invitations_inviter_id] FOREIGN KEY ([inviter_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_invitations_invitee_id')
    ALTER TABLE dbo.[invitations] ADD CONSTRAINT [FK_invitations_invitee_id] FOREIGN KEY ([invitee_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_invitations_session_id')
    ALTER TABLE dbo.[invitations] ADD CONSTRAINT [FK_invitations_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_invitations_ticket_id')
    ALTER TABLE dbo.[invitations] ADD CONSTRAINT [FK_invitations_ticket_id] FOREIGN KEY ([ticket_id]) REFERENCES dbo.[tickets]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_partner_requests_session_id')
    ALTER TABLE dbo.[partner_requests] ADD CONSTRAINT [FK_partner_requests_session_id] FOREIGN KEY ([session_id]) REFERENCES dbo.[sessions]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_partner_requests_requester_id')
    ALTER TABLE dbo.[partner_requests] ADD CONSTRAINT [FK_partner_requests_requester_id] FOREIGN KEY ([requester_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_partner_requests_partner_id')
    ALTER TABLE dbo.[partner_requests] ADD CONSTRAINT [FK_partner_requests_partner_id] FOREIGN KEY ([partner_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_orders_staff_id')
    ALTER TABLE dbo.[orders] ADD CONSTRAINT [FK_orders_staff_id] FOREIGN KEY ([staff_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_order_id')
    ALTER TABLE dbo.[payments] ADD CONSTRAINT [FK_payments_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[social_booking_orders]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_goods_order_id')
    ALTER TABLE dbo.[payments] ADD CONSTRAINT [FK_payments_goods_order_id] FOREIGN KEY ([goods_order_id]) REFERENCES dbo.[orders]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_payer_id')
    ALTER TABLE dbo.[payments] ADD CONSTRAINT [FK_payments_payer_id] FOREIGN KEY ([payer_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_payments_confirmed_by')
    ALTER TABLE dbo.[payments] ADD CONSTRAINT [FK_payments_confirmed_by] FOREIGN KEY ([confirmed_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_bank_transactions_matched_payment_id')
    ALTER TABLE dbo.[bank_transactions] ADD CONSTRAINT [FK_bank_transactions_matched_payment_id] FOREIGN KEY ([matched_payment_id]) REFERENCES dbo.[payments]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_refunds_payment_id')
    ALTER TABLE dbo.[refunds] ADD CONSTRAINT [FK_refunds_payment_id] FOREIGN KEY ([payment_id]) REFERENCES dbo.[payments]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_refunds_ticket_id')
    ALTER TABLE dbo.[refunds] ADD CONSTRAINT [FK_refunds_ticket_id] FOREIGN KEY ([ticket_id]) REFERENCES dbo.[tickets]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_refunds_recipient_id')
    ALTER TABLE dbo.[refunds] ADD CONSTRAINT [FK_refunds_recipient_id] FOREIGN KEY ([recipient_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_refunds_initiated_by')
    ALTER TABLE dbo.[refunds] ADD CONSTRAINT [FK_refunds_initiated_by] FOREIGN KEY ([initiated_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_refunds_approved_by')
    ALTER TABLE dbo.[refunds] ADD CONSTRAINT [FK_refunds_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_order_items_order_id')
    ALTER TABLE dbo.[order_items] ADD CONSTRAINT [FK_order_items_order_id] FOREIGN KEY ([order_id]) REFERENCES dbo.[orders]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_order_items_product_id')
    ALTER TABLE dbo.[order_items] ADD CONSTRAINT [FK_order_items_product_id] FOREIGN KEY ([product_id]) REFERENCES dbo.[products]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_items_product_id')
    ALTER TABLE dbo.[stock_items] ADD CONSTRAINT [FK_stock_items_product_id] FOREIGN KEY ([product_id]) REFERENCES dbo.[products]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_movements_stock_item_id')
    ALTER TABLE dbo.[stock_movements] ADD CONSTRAINT [FK_stock_movements_stock_item_id] FOREIGN KEY ([stock_item_id]) REFERENCES dbo.[stock_items]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_movements_created_by')
    ALTER TABLE dbo.[stock_movements] ADD CONSTRAINT [FK_stock_movements_created_by] FOREIGN KEY ([created_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_counts_stock_item_id')
    ALTER TABLE dbo.[stock_counts] ADD CONSTRAINT [FK_stock_counts_stock_item_id] FOREIGN KEY ([stock_item_id]) REFERENCES dbo.[stock_items]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_counts_counted_by')
    ALTER TABLE dbo.[stock_counts] ADD CONSTRAINT [FK_stock_counts_counted_by] FOREIGN KEY ([counted_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_stock_counts_approved_by')
    ALTER TABLE dbo.[stock_counts] ADD CONSTRAINT [FK_stock_counts_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_shift_reconciliations_staff_id')
    ALTER TABLE dbo.[shift_reconciliations] ADD CONSTRAINT [FK_shift_reconciliations_staff_id] FOREIGN KEY ([staff_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_shift_reconciliations_approved_by')
    ALTER TABLE dbo.[shift_reconciliations] ADD CONSTRAINT [FK_shift_reconciliations_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_reconciliation_items_reconciliation_id')
    ALTER TABLE dbo.[reconciliation_items] ADD CONSTRAINT [FK_reconciliation_items_reconciliation_id] FOREIGN KEY ([reconciliation_id]) REFERENCES dbo.[shift_reconciliations]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_policies_approved_by')
    ALTER TABLE dbo.[policies] ADD CONSTRAINT [FK_policies_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_approvals_requested_by')
    ALTER TABLE dbo.[approvals] ADD CONSTRAINT [FK_approvals_requested_by] FOREIGN KEY ([requested_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_approvals_approved_by')
    ALTER TABLE dbo.[approvals] ADD CONSTRAINT [FK_approvals_approved_by] FOREIGN KEY ([approved_by]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_audit_logs_actor_id')
    ALTER TABLE dbo.[audit_logs] ADD CONSTRAINT [FK_audit_logs_actor_id] FOREIGN KEY ([actor_id]) REFERENCES dbo.[users]([id]);
GO
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_requester_id')
    ALTER TABLE dbo.[friendships] DROP CONSTRAINT [FK_friendships_requester_id];
GO
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_addressee_id')
    ALTER TABLE dbo.[friendships] DROP CONSTRAINT [FK_friendships_addressee_id];
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_requester')
    ALTER TABLE dbo.[friendships] ADD CONSTRAINT [FK_friendships_requester] FOREIGN KEY ([requester_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_addressee')
    ALTER TABLE dbo.[friendships] ADD CONSTRAINT [FK_friendships_addressee] FOREIGN KEY ([addressee_id]) REFERENCES dbo.[players]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_messages_sender_id')
    ALTER TABLE dbo.[messages] ADD CONSTRAINT [FK_messages_sender_id] FOREIGN KEY ([sender_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_messages_recipient_id')
    ALTER TABLE dbo.[messages] ADD CONSTRAINT [FK_messages_recipient_id] FOREIGN KEY ([recipient_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_notifications_user_id')
    ALTER TABLE dbo.[notifications] ADD CONSTRAINT [FK_notifications_user_id] FOREIGN KEY ([user_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_roles_code' AND object_id = OBJECT_ID(N'dbo.[roles]'))
    CREATE UNIQUE INDEX [UX_roles_code] ON dbo.[roles]([code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_email' AND object_id = OBJECT_ID(N'dbo.[users]'))
    CREATE UNIQUE INDEX [UX_users_email] ON dbo.[users]([email]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_google_subject' AND object_id = OBJECT_ID(N'dbo.[users]'))
    CREATE UNIQUE INDEX [UX_users_google_subject] ON dbo.[users]([google_subject]) WHERE [google_subject] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_sports_code' AND object_id = OBJECT_ID(N'dbo.[sports]'))
    CREATE UNIQUE INDEX [UX_sports_code] ON dbo.[sports]([code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_social_booking_orders_order_code' AND object_id = OBJECT_ID(N'dbo.[social_booking_orders]'))
    CREATE UNIQUE INDEX [UX_social_booking_orders_order_code] ON dbo.[social_booking_orders]([order_code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_tickets_ticket_code' AND object_id = OBJECT_ID(N'dbo.[tickets]'))
    CREATE UNIQUE INDEX [UX_tickets_ticket_code] ON dbo.[tickets]([ticket_code]) WHERE [ticket_code] IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_orders_order_code' AND object_id = OBJECT_ID(N'dbo.[orders]'))
    CREATE UNIQUE INDEX [UX_orders_order_code] ON dbo.[orders]([order_code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_payments_reference_code' AND object_id = OBJECT_ID(N'dbo.[payments]'))
    CREATE UNIQUE INDEX [UX_payments_reference_code] ON dbo.[payments]([reference_code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_bank_transactions_external_id' AND object_id = OBJECT_ID(N'dbo.[bank_transactions]'))
    CREATE UNIQUE INDEX [UX_bank_transactions_external_id] ON dbo.[bank_transactions]([external_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_products_sku' AND object_id = OBJECT_ID(N'dbo.[products]'))
    CREATE UNIQUE INDEX [UX_products_sku] ON dbo.[products]([sku]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_policies_code' AND object_id = OBJECT_ID(N'dbo.[policies]'))
    CREATE UNIQUE INDEX [UX_policies_code] ON dbo.[policies]([code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_players_user_id' AND object_id = OBJECT_ID(N'dbo.[players]'))
    CREATE UNIQUE INDEX [UX_players_user_id] ON dbo.[players]([user_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_player_sport_profiles_player_id_sport_id' AND object_id = OBJECT_ID(N'dbo.[player_sport_profiles]'))
    CREATE UNIQUE INDEX [UX_player_sport_profiles_player_id_sport_id] ON dbo.[player_sport_profiles]([player_id],[sport_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_rubric_dimensions_rubric_id_code' AND object_id = OBJECT_ID(N'dbo.[rubric_dimensions]'))
    CREATE UNIQUE INDEX [UX_rubric_dimensions_rubric_id_code] ON dbo.[rubric_dimensions]([rubric_id],[code]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_items_product_id' AND object_id = OBJECT_ID(N'dbo.[stock_items]'))
    CREATE UNIQUE INDEX [UX_stock_items_product_id] ON dbo.[stock_items]([product_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_sessions_court_id_session_date_time_slot_id' AND object_id = OBJECT_ID(N'dbo.[sessions]'))
    CREATE UNIQUE INDEX [UX_sessions_court_id_session_date_time_slot_id] ON dbo.[sessions]([court_id],[session_date],[time_slot_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_session_referees_session_id_referee_id' AND object_id = OBJECT_ID(N'dbo.[session_referees]'))
    CREATE UNIQUE INDEX [UX_session_referees_session_id_referee_id] ON dbo.[session_referees]([session_id],[referee_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_session_staff_session_id_staff_id' AND object_id = OBJECT_ID(N'dbo.[session_staff]'))
    CREATE UNIQUE INDEX [UX_session_staff_session_id_staff_id] ON dbo.[session_staff]([session_id],[staff_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_assessment_scores_assessment_id_dimension_id' AND object_id = OBJECT_ID(N'dbo.[assessment_scores]'))
    CREATE UNIQUE INDEX [UX_assessment_scores_assessment_id_dimension_id] ON dbo.[assessment_scores]([assessment_id],[dimension_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_order_items_order_id_product_id' AND object_id = OBJECT_ID(N'dbo.[order_items]'))
    CREATE UNIQUE INDEX [UX_order_items_order_id_product_id] ON dbo.[order_items]([order_id],[product_id]);
GO
-- =============================================================================
-- ADDITIVE SCHEMA UPGRADES (Chat, FCM Tokens & PayOS)
-- =============================================================================
IF OBJECT_ID(N'dbo.[user_fcm_tokens]', N'U') IS NULL
CREATE TABLE dbo.[user_fcm_tokens] (
    [id] BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_user_fcm_tokens] PRIMARY KEY,
    [user_id] BIGINT NOT NULL,
    [fcm_token] NVARCHAR(512) NOT NULL,
    [device_type] VARCHAR(50) NULL,
    [updated_at] DATETIMEOFFSET(0) NOT NULL CONSTRAINT [DF_user_fcm_tokens_updated_at] DEFAULT (SYSDATETIMEOFFSET()),
    CONSTRAINT [FK_user_fcm_tokens_user] FOREIGN KEY ([user_id]) REFERENCES dbo.[users]([id])
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_user_fcm_tokens_token' AND object_id = OBJECT_ID(N'dbo.[user_fcm_tokens]'))
    CREATE UNIQUE INDEX [UX_user_fcm_tokens_token] ON dbo.[user_fcm_tokens]([fcm_token]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_user_fcm_tokens_user_id' AND object_id = OBJECT_ID(N'dbo.[user_fcm_tokens]'))
    CREATE INDEX [IX_user_fcm_tokens_user_id] ON dbo.[user_fcm_tokens]([user_id]);
GO
IF COL_LENGTH('dbo.friendships', 'created_at') IS NULL
    ALTER TABLE dbo.[friendships] ADD [created_at] DATETIMEOFFSET(0) NOT NULL CONSTRAINT [DF_friendships_created_at] DEFAULT (SYSDATETIMEOFFSET());
GO
IF COL_LENGTH('dbo.friendships', 'responded_at') IS NULL
    ALTER TABLE dbo.[friendships] ADD [responded_at] DATETIMEOFFSET(0) NULL;
GO
IF COL_LENGTH('dbo.friendships', 'player_low_id') IS NULL
    ALTER TABLE dbo.[friendships] ADD [player_low_id] AS (CASE WHEN [requester_id] < [addressee_id] THEN [requester_id] ELSE [addressee_id] END) PERSISTED;
GO
IF COL_LENGTH('dbo.friendships', 'player_high_id') IS NULL
    ALTER TABLE dbo.[friendships] ADD [player_high_id] AS (CASE WHEN [requester_id] < [addressee_id] THEN [addressee_id] ELSE [requester_id] END) PERSISTED;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_friendships_requester' AND object_id = OBJECT_ID(N'dbo.[friendships]'))
    CREATE INDEX [IX_friendships_requester] ON dbo.[friendships]([requester_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_friendships_addressee' AND object_id = OBJECT_ID(N'dbo.[friendships]'))
    CREATE INDEX [IX_friendships_addressee] ON dbo.[friendships]([addressee_id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_friendships_active_pair' AND object_id = OBJECT_ID(N'dbo.[friendships]'))
    CREATE UNIQUE INDEX [UX_friendships_active_pair] ON dbo.[friendships]([player_low_id], [player_high_id]) WHERE ([status] IN ('Pending', 'Accepted'));
GO
IF COL_LENGTH('dbo.messages', 'created_at') IS NULL
    ALTER TABLE dbo.[messages] ADD [created_at] DATETIMEOFFSET(0) NOT NULL CONSTRAINT [DF_messages_created_at] DEFAULT (SYSDATETIMEOFFSET());
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_messages_conversation' AND object_id = OBJECT_ID(N'dbo.[messages]'))
    CREATE INDEX [IX_messages_conversation] ON dbo.[messages]([sender_id], [recipient_id], [id]);
GO
IF COL_LENGTH('dbo.payments', 'gateway_payment_link_id') IS NULL
    ALTER TABLE dbo.[payments] ADD [gateway_payment_link_id] VARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.payments', 'checkout_url') IS NULL
    ALTER TABLE dbo.[payments] ADD [checkout_url] NVARCHAR(2048) NULL;
GO
IF COL_LENGTH('dbo.bank_transactions', 'raw_payload') IS NULL
    ALTER TABLE dbo.[bank_transactions] ADD [raw_payload] NVARCHAR(MAX) NULL;
GO
IF OBJECT_ID('dbo.ticket_changes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ticket_changes] (
        [id] BIGINT IDENTITY PRIMARY KEY,
        [ticket_id] BIGINT NOT NULL REFERENCES dbo.[tickets]([id]),
        [target_session_id] BIGINT NOT NULL REFERENCES dbo.[sessions]([id]),
        [old_price_vnd] INT NOT NULL CHECK([old_price_vnd] >= 0),
        [new_price_vnd] INT NOT NULL CHECK([new_price_vnd] >= 0),
        [status] VARCHAR(30) NOT NULL,
        [requested_by] BIGINT NOT NULL REFERENCES dbo.[users]([id]),
        [processed_by] BIGINT NULL REFERENCES dbo.[users]([id]),
        [new_ticket_id] BIGINT NULL REFERENCES dbo.[tickets]([id]),
        [created_at] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [completed_at] DATETIMEOFFSET NULL,
        CONSTRAINT [CK_ticket_changes_status] CHECK([status] IN ('requested', 'completed', 'cancelled'))
    );
    CREATE UNIQUE INDEX [UX_ticket_changes_pending] ON dbo.[ticket_changes]([ticket_id]) WHERE [status] = 'requested';
END;
GO
IF OBJECT_ID('dbo.ticket_funding', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ticket_funding] (
        [id] BIGINT IDENTITY PRIMARY KEY,
        [ticket_id] BIGINT NOT NULL REFERENCES dbo.[tickets]([id]),
        [payment_id] BIGINT NOT NULL REFERENCES dbo.[payments]([id]),
        [amount_vnd] INT NOT NULL CHECK([amount_vnd] > 0),
        [retired] BIT NOT NULL DEFAULT 0,
        CONSTRAINT [UX_ticket_funding] UNIQUE([ticket_id], [payment_id])
    );
END;
GO
IF OBJECT_ID('dbo.ticket_support_messages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.[ticket_support_messages] (
        [id] BIGINT IDENTITY PRIMARY KEY,
        [ticket_id] BIGINT NOT NULL REFERENCES dbo.[tickets]([id]),
        [sender_id] BIGINT NOT NULL REFERENCES dbo.[users]([id]),
        [is_staff] BIT NOT NULL,
        [content] NVARCHAR(2000) NOT NULL,
        [image_data] VARBINARY(MAX) NULL,
        [image_type] VARCHAR(20) NULL,
        [created_at] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [CK_ticket_support_image] CHECK (([image_data] IS NULL AND [image_type] IS NULL) OR ([image_data] IS NOT NULL AND DATALENGTH([image_data]) BETWEEN 1 AND 2097152 AND [image_type] IN ('image/png', 'image/jpeg')))
    );
    CREATE INDEX [IX_ticket_support_ticket] ON dbo.[ticket_support_messages]([ticket_id], [id]);
END;
GO
-- =============================================================================
-- SNAPSHOT DATA FROM LIVE Sep490 DATABASE (Updated 2026-11-02 12:09)
-- Includes Weekly Timetable Sessions (005) & Rubrics/Assessments/Skill History (006)
-- =============================================================================
SET IDENTITY_INSERT dbo.[roles] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 1)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (1, 'Player', N'Player');
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 2)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (2, 'Referee', N'Referee');
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 3)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (3, 'Staff', N'Staff');
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 4)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (4, 'Manager', N'Manager');
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 5)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (5, 'Owner', N'Owner');
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [id] = 6)
    INSERT INTO dbo.[roles] ([id], [code], [name]) VALUES (6, 'SystemAdmin', N'SystemAdmin');
SET IDENTITY_INSERT dbo.[roles] OFF;
GO
SET IDENTITY_INSERT dbo.[sports] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[sports] WHERE [id] = 1)
    INSERT INTO dbo.[sports] ([id], [code], [name]) VALUES (1, 'badminton', N'Badminton');
IF NOT EXISTS (SELECT 1 FROM dbo.[sports] WHERE [id] = 2)
    INSERT INTO dbo.[sports] ([id], [code], [name]) VALUES (2, 'pickleball', N'Pickleball');
SET IDENTITY_INSERT dbo.[sports] OFF;
GO
SET IDENTITY_INSERT dbo.[users] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 1)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (1, 1, N'nhatlthe180545@fpt.edu.vn', N'Le Trong Nhat (K18 HL)', NULL, 'Active', NULL, '101026349336814090722');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 2)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (2, 1, N'hyperslator@gmail.com', N'nhatlt', NULL, 'Active', NULL, '114019429860673938140');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 3)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (3, 1, N'shogunsusanoo@gmail.com', N'Hyune', NULL, 'Active', NULL, '103557709134702352271');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 4)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (4, 4, N'manager@tksc.local', N'Tran Van Lam (Manager)', NULL, 'Active', NULL, 'dev-4');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 5)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (5, 2, N'referee@tksc.local', N'Nguyen Trong Tai (Referee)', NULL, 'Active', NULL, 'dev-5');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 6)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (6, 3, N'staff@tksc.local', N'Pham Nhan Vien (Staff)', NULL, 'Active', NULL, 'dev-6');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 7)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (7, 5, N'owner@tksc.local', N'Le Chu San (Owner)', NULL, 'Active', NULL, 'dev-7');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 8)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (8, 6, N'admin@tksc.local', N'Nguyen Quan Tri (SystemAdmin)', NULL, 'Active', NULL, 'dev-8');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 9)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (9, 1, N'player1@tksc.local', N'Nguyễn Văn An (Player 1)', NULL, 'Active', NULL, 'dev-9');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 10)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (10, 1, N'player2@tksc.local', N'Trần Thị Bình (Player 2)', NULL, 'Active', NULL, 'dev-10');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 11)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (11, 1, N'player3@tksc.local', N'Lê Hoàng Cường (Player 3)', NULL, 'Active', NULL, 'dev-11');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 12)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (12, 1, N'lananh.player@tksc.local', N'Phạm Thị Lan Anh', NULL, 'Active', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 13)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (13, 1, N'triet.vo@tksc.local', N'Võ Minh Triết', NULL, 'Active', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 10012)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (10012, 1, N'nhatletrong1806@gmail.com', N'Nhật Lê Trọng', NULL, 'Active', NULL, '103151372894914562743');
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 10013)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (10013, 1, N'khanh.do@tksc.local', N'Đỗ Quốc Khánh', NULL, 'Active', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 10014)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (10014, 1, N'my.nguyen@tksc.local', N'Nguyễn Trà My', NULL, 'Active', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[users] WHERE [id] = 10015)
    INSERT INTO dbo.[users] ([id], [role_id], [email], [full_name], [password_hash], [status], [deleted_at], [google_subject]) VALUES (10015, 1, N'huy.hoang@tksc.local', N'Hoàng Gia Huy', NULL, 'Active', NULL, NULL);
SET IDENTITY_INSERT dbo.[users] OFF;
GO
SET IDENTITY_INSERT dbo.[account_audit_logs] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 1)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (1, 1, 1, 'GoogleRegistration', NULL, N'Player');
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 2)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (2, 2, 2, 'GoogleRegistration', NULL, N'Player');
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 3)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (3, 3, 3, 'GoogleRegistration', NULL, N'Player');
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 4)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (4, 9, 9, 'CompleteSportSurvey', N'{"Sport":"badminton","Score":35,"Band":"B2","Status":"Provisional"}', N'{"Sport":"badminton","Score":0,"Band":"string","Status":"Provisional","Notes":"string"}');
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 5)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (5, 9, 9, 'SendFriendRequest', NULL, N'{"TargetPlayerId":1,"FriendshipId":5}');
IF NOT EXISTS (SELECT 1 FROM dbo.[account_audit_logs] WHERE [id] = 10004)
    INSERT INTO dbo.[account_audit_logs] ([id], [user_id], [actor_id], [action], [old_value], [new_value]) VALUES (10004, 10012, 10012, 'GoogleRegistration', NULL, N'Player');
SET IDENTITY_INSERT dbo.[account_audit_logs] OFF;
GO
SET IDENTITY_INSERT dbo.[players] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 1)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (1, 1, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 2)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (2, 2, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 3)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (3, 3, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 4)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (4, 9, '1998-05-15', 'Male', 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 5)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (5, 10, '1998-05-15', 'Male', 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 6)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (6, 11, '1998-05-15', 'Male', 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 7)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (7, 12, '2001-08-15', 'Female', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 8)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (8, 13, '1998-04-12', 'Male', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 10007)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (10007, 10012, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 10008)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (10008, 10013, '1999-05-20', 'Male', 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=100');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 10009)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (10009, 10014, '1999-05-20', 'Female', 'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=100');
IF NOT EXISTS (SELECT 1 FROM dbo.[players] WHERE [id] = 10010)
    INSERT INTO dbo.[players] ([id], [user_id], [date_of_birth], [gender], [avatar_url]) VALUES (10010, 10015, '1999-05-20', 'Male', 'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=100');
SET IDENTITY_INSERT dbo.[players] OFF;
GO
SET IDENTITY_INSERT dbo.[player_sport_profiles] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 1)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (1, 1, 1, 55, 'B3', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 2)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (2, 2, 1, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 3)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (3, 3, 1, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 4)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (4, 1, 2, NULL, NULL, 'Unrated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 5)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (5, 2, 2, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 6)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (6, 3, 2, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 7)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (7, 4, 1, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 8)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (8, 5, 1, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 9)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (9, 6, 1, 35, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 10)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (10, 4, 2, 60, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 11)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (11, 5, 2, 60, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 12)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (12, 6, 2, 60, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 10007)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (10007, 7, 1, 40, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 10008)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (10008, 7, 2, 25, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 10009)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (10009, 8, 1, 65, 'B3', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 10010)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (10010, 8, 2, 50, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20007)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20007, 10008, 1, 55, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20008)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20008, 10009, 1, 55, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20009)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20009, 10010, 1, 55, 'B3', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20010)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20010, 10008, 2, 50, 'B1', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20011)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20011, 10009, 2, 50, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20012)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20012, 10010, 2, 50, 'B2', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20013)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20013, 10007, 1, 45, 'B3', 'Rated');
IF NOT EXISTS (SELECT 1 FROM dbo.[player_sport_profiles] WHERE [id] = 20014)
    INSERT INTO dbo.[player_sport_profiles] ([id], [player_id], [sport_id], [current_score], [current_band], [rating_status]) VALUES (20014, 10007, 2, 65, 'B4', 'Provisional');
SET IDENTITY_INSERT dbo.[player_sport_profiles] OFF;
GO
SET IDENTITY_INSERT dbo.[rubrics] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[rubrics] WHERE [id] = 1)
    INSERT INTO dbo.[rubrics] ([id], [sport_id], [version], [name], [is_active]) VALUES (1, 1, 'BM-CLB-V1', N'Khung đánh giá Cầu lông Phong trào VN (Yếu / TB- / TB / TB+ / Khá)', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubrics] WHERE [id] = 2)
    INSERT INTO dbo.[rubrics] ([id], [sport_id], [version], [name], [is_active]) VALUES (2, 2, 'PK-IPTPA-DUPR-V1', N'Khung đánh giá Pickleball chuẩn IPTPA & DUPR (2.0 – 4.0+)', 1);
SET IDENTITY_INSERT dbo.[rubrics] OFF;
GO
SET IDENTITY_INSERT dbo.[rubric_dimensions] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 1)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (1, 1, 'BM_SERVE_NET', N'Giao cầu, Trả giao & Xử lý trên lưới', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 2)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (2, 1, 'BM_CLEAR_BACKHAND', N'Phông cầu cuối sân (Clear) & Trái tay (Backhand)', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 3)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (3, 1, 'BM_SMASH_DEFENSE', N'Đập cầu (Smash), Chặt cầu (Drop) & Phòng thủ', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 4)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (4, 1, 'BM_FOOTWORK', N'Bộ pháp di chuyển 6 góc sân & Hồi vị', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 5)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (5, 1, 'BM_DOUBLES', N'Đội hình Đánh đôi (Công/Thủ) & Độ bền Rally', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 6)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (6, 2, 'PK_SERVE_RETURN', N'Giao bóng (Serve) & Trả giao bóng sâu (Return)', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 7)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (7, 2, 'PK_THIRD_SHOT', N'Cú đánh thứ 3 (3rd Shot Drop/Drive) & Lên lưới', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 8)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (8, 2, 'PK_DINK_NVZ', N'Kỹ thuật Dink & Kiểm soát vạch Kitchen (NVZ)', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 9)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (9, 2, 'PK_VOLLEY_RESET', N'Bắt Volley, Đấu tay đôi (Hand Battle) & Reset bóng', 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[rubric_dimensions] WHERE [id] = 10)
    INSERT INTO dbo.[rubric_dimensions] ([id], [rubric_id], [code], [name], [weight_percent]) VALUES (10, 2, 'PK_DOUBLES_STRATEGY', N'Groundstroke 2 tay & Chiến thuật Đánh đôi (Stacking)', 20);
SET IDENTITY_INSERT dbo.[rubric_dimensions] OFF;
GO
SET IDENTITY_INSERT dbo.[venues] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[venues] WHERE [id] = 1)
    INSERT INTO dbo.[venues] ([id], [name], [address], [is_active]) VALUES (1, N'TK Sport Community Center', NULL, 1);
SET IDENTITY_INSERT dbo.[venues] OFF;
GO
SET IDENTITY_INSERT dbo.[courts] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[courts] WHERE [id] = 1)
    INSERT INTO dbo.[courts] ([id], [venue_id], [name], [sport_id], [is_active]) VALUES (1, 1, N'Court 1 - Badminton', 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[courts] WHERE [id] = 2)
    INSERT INTO dbo.[courts] ([id], [venue_id], [name], [sport_id], [is_active]) VALUES (2, 1, N'Court 2 - Pickleball', 2, 1);
SET IDENTITY_INSERT dbo.[courts] OFF;
GO
SET IDENTITY_INSERT dbo.[time_slots] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 1)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (1, '08:00:00', '10:00:00', N'08:00 - 10:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 2)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (2, '10:00:00', '12:00:00', N'10:00 - 12:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 3)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (3, '14:00:00', '16:00:00', N'14:00 - 16:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 4)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (4, '16:00:00', '18:00:00', N'16:00 - 18:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 5)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (5, '18:00:00', '20:00:00', N'18:00 - 20:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[time_slots] WHERE [id] = 6)
    INSERT INTO dbo.[time_slots] ([id], [start_time], [end_time], [label]) VALUES (6, '20:00:00', '22:00:00', N'20:00 - 22:00');
SET IDENTITY_INSERT dbo.[time_slots] OFF;
GO
SET IDENTITY_INSERT dbo.[sessions] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 1)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (1, 1, 1, 5, '2026-10-15', 'B2', 4, 50000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 2)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (2, 2, 2, 3, '2026-10-16', 'B1', 4, 60000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10002)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10002, 1, 1, 5, '2026-10-20', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10003)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10003, 2, 2, 1, '2026-10-20', 'B1', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10004)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10004, 1, 1, 4, '2026-10-20', 'B2', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10005)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10005, 1, 1, 6, '2026-10-21', 'B1', 6, 80000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10006)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10006, 2, 2, 4, '2026-10-21', NULL, 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10007)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10007, 1, 1, 6, '2026-10-20', 'B3', 4, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 10008)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (10008, 2, 2, 2, '2026-10-22', 'B2', 6, 120000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20002)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20002, 1, 1, 1, '2026-10-26', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20003)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20003, 2, 2, 4, '2026-10-26', 'B1', 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20004)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20004, 1, 1, 5, '2026-10-26', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20005)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20005, 2, 2, 5, '2026-10-26', 'B2', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20006)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20006, 1, 1, 6, '2026-10-26', NULL, 8, 80000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20007)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20007, 2, 2, 2, '2026-10-27', NULL, 6, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20008)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20008, 1, 1, 4, '2026-10-27', 'B1', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20009)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20009, 1, 1, 5, '2026-10-27', 'B3', 6, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20010)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20010, 2, 2, 6, '2026-10-27', 'B1', 4, 100000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20011)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20011, 2, 2, 1, '2026-10-28', 'B1', 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20012)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20012, 1, 1, 3, '2026-10-28', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20013)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20013, 1, 1, 5, '2026-10-28', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20014)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20014, 2, 2, 5, '2026-10-28', NULL, 6, 100000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20015)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20015, 1, 1, 6, '2026-10-28', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20016)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20016, 1, 1, 1, '2026-10-29', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20017)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20017, 2, 2, 2, '2026-10-29', 'B1', 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20018)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20018, 1, 1, 4, '2026-10-29', 'B2', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20019)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20019, 2, 2, 4, '2026-10-29', NULL, 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20020)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20020, 1, 1, 5, '2026-10-29', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20021)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20021, 2, 2, 5, '2026-10-29', 'B2', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20022)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20022, 1, 1, 6, '2026-10-29', 'B3', 6, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20023)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20023, 2, 2, 3, '2026-10-30', 'B1', 6, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20024)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20024, 1, 1, 4, '2026-10-30', 'B1', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20025)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20025, 1, 1, 5, '2026-10-30', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20026)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20026, 1, 1, 6, '2026-10-30', NULL, 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20027)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20027, 2, 2, 6, '2026-10-30', 'B2', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20028)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20028, 1, 1, 1, '2026-10-31', 'B1', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20029)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20029, 2, 2, 1, '2026-10-31', 'B1', 6, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20030)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20030, 1, 1, 2, '2026-10-31', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20031)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20031, 2, 2, 3, '2026-10-31', NULL, 6, 105000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20032)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20032, 1, 1, 4, '2026-10-31', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20033)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20033, 1, 1, 5, '2026-10-31', 'B3', 8, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20034)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20034, 2, 2, 5, '2026-10-31', 'B2', 4, 115000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20035)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20035, 1, 1, 6, '2026-10-31', NULL, 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20036)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20036, 2, 2, 1, '2026-11-01', NULL, 6, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20037)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20037, 1, 1, 2, '2026-11-01', 'B1', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20038)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20038, 2, 2, 2, '2026-11-01', 'B2', 4, 115000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20039)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20039, 1, 1, 4, '2026-11-01', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20040)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20040, 1, 1, 5, '2026-11-01', 'B2', 8, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20041)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20041, 2, 2, 5, '2026-11-01', 'B1', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20042)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20042, 2, 2, 6, '2026-11-01', NULL, 6, 100000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20043)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20043, 1, 1, 1, '2026-11-02', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20044)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20044, 1, 1, 4, '2026-11-02', 'B2', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20045)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20045, 1, 1, 5, '2026-11-02', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20046)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20046, 2, 2, 5, '2026-11-02', 'B1', 4, 105000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20047)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20047, 2, 2, 6, '2026-11-02', NULL, 6, 100000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20048)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20048, 2, 2, 2, '2026-11-03', 'B1', 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20049)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20049, 2, 2, 4, '2026-11-03', NULL, 6, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20050)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20050, 1, 1, 5, '2026-11-03', 'B3', 6, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20051)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20051, 1, 1, 6, '2026-11-03', 'B1', 8, 80000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20052)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20052, 2, 2, 1, '2026-11-04', NULL, 6, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20053)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20053, 1, 1, 3, '2026-11-04', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20054)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20054, 1, 1, 5, '2026-11-04', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20055)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20055, 2, 2, 5, '2026-11-04', 'B2', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20056)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20056, 1, 1, 6, '2026-11-04', NULL, 8, 80000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20057)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20057, 1, 1, 2, '2026-11-05', 'B1', 8, 70000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20058)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20058, 1, 1, 4, '2026-11-05', 'B2', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20059)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20059, 2, 2, 4, '2026-11-05', 'B1', 4, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20060)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20060, 1, 1, 5, '2026-11-05', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20061)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20061, 2, 2, 6, '2026-11-05', 'B2', 4, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20062)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20062, 2, 2, 3, '2026-11-06', 'B1', 6, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20063)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20063, 1, 1, 4, '2026-11-06', 'B1', 8, 75000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20064)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20064, 1, 1, 5, '2026-11-06', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20065)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20065, 2, 2, 5, '2026-11-06', NULL, 6, 105000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20066)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20066, 1, 1, 6, '2026-11-06', 'B3', 6, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20067)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20067, 1, 1, 1, '2026-11-07', 'B1', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20068)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20068, 2, 2, 1, '2026-11-07', 'B1', 6, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20069)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20069, 1, 1, 2, '2026-11-07', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20070)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20070, 2, 2, 4, '2026-11-07', 'B2', 4, 115000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20071)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20071, 1, 1, 5, '2026-11-07', 'B2', 8, 90000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20072)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20072, 2, 2, 5, '2026-11-07', NULL, 6, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20073)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20073, 1, 1, 6, '2026-11-07', NULL, 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20074)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20074, 2, 2, 1, '2026-11-08', NULL, 6, 110000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20075)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20075, 1, 1, 2, '2026-11-08', 'B1', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20076)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20076, 2, 2, 3, '2026-11-08', 'B1', 6, 105000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20077)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20077, 1, 1, 4, '2026-11-08', 'B2', 8, 85000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20078)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20078, 1, 1, 5, '2026-11-08', 'B3', 6, 95000, 'Published', 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[sessions] WHERE [id] = 20079)
    INSERT INTO dbo.[sessions] ([id], [sport_id], [court_id], [time_slot_id], [session_date], [band], [capacity], [price_vnd], [status], [created_by]) VALUES (20079, 2, 2, 5, '2026-11-08', 'B2', 4, 115000, 'Published', 4);
SET IDENTITY_INSERT dbo.[sessions] OFF;
GO
SET IDENTITY_INSERT dbo.[session_referees] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[session_referees] WHERE [id] = 1)
    INSERT INTO dbo.[session_referees] ([id], [session_id], [referee_id], [assigned_by]) VALUES (1, 1, 5, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[session_referees] WHERE [id] = 2)
    INSERT INTO dbo.[session_referees] ([id], [session_id], [referee_id], [assigned_by]) VALUES (2, 2, 5, 4);
SET IDENTITY_INSERT dbo.[session_referees] OFF;
GO
SET IDENTITY_INSERT dbo.[session_staff] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[session_staff] WHERE [id] = 1)
    INSERT INTO dbo.[session_staff] ([id], [session_id], [staff_id], [role_in_session]) VALUES (1, 1, 6, N'Ho tro san va check-in');
SET IDENTITY_INSERT dbo.[session_staff] OFF;
GO
SET IDENTITY_INSERT dbo.[assessments] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 1)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (1, 1, NULL, 1, 'SelfSurvey', 1, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 2)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (2, 1, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 3)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (3, 2, NULL, 1, 'SelfSurvey', 2, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 4)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (4, 2, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 5)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (5, 3, NULL, 1, 'SelfSurvey', 3, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 6)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (6, 3, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 9)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (9, 5, NULL, 2, 'SelfSurvey', 2, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 10)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (10, 5, 20079, 2, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 11)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (11, 6, NULL, 2, 'SelfSurvey', 3, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 12)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (12, 6, 20079, 2, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 13)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (13, 7, NULL, 1, 'SelfSurvey', 9, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 14)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (14, 7, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 15)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (15, 8, NULL, 1, 'SelfSurvey', 10, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 16)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (16, 8, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 17)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (17, 9, NULL, 1, 'SelfSurvey', 11, 'Published', 25, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 18)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (18, 9, 20078, 1, 'OnCourt', 5, 'Published', 35, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 19)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (19, 10, NULL, 2, 'SelfSurvey', 9, 'Published', 50, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 20)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (20, 10, 20079, 2, 'OnCourt', 5, 'Published', 60, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 21)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (21, 11, NULL, 2, 'SelfSurvey', 10, 'Published', 50, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 22)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (22, 11, 20079, 2, 'OnCourt', 5, 'Published', 60, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 23)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (23, 12, NULL, 2, 'SelfSurvey', 11, 'Published', 50, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 24)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (24, 12, 20079, 2, 'OnCourt', 5, 'Published', 60, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 25)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (25, 10007, NULL, 1, 'SelfSurvey', 12, 'Published', 30, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 26)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (26, 10007, 20078, 1, 'OnCourt', 5, 'Published', 40, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 27)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (27, 10008, NULL, 2, 'SelfSurvey', 12, 'Published', 15, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 28)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (28, 10008, 20079, 2, 'OnCourt', 5, 'Published', 25, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 29)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (29, 10009, NULL, 1, 'SelfSurvey', 13, 'Published', 55, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 30)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (30, 10009, 20078, 1, 'OnCourt', 5, 'Published', 65, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 31)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (31, 10010, NULL, 2, 'SelfSurvey', 13, 'Published', 40, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 32)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (32, 10010, 20079, 2, 'OnCourt', 5, 'Published', 50, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 33)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (33, 20007, NULL, 1, 'SelfSurvey', 10013, 'Published', 45, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 34)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (34, 20007, 20078, 1, 'OnCourt', 5, 'Published', 55, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 35)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (35, 20008, NULL, 1, 'SelfSurvey', 10014, 'Published', 45, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 36)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (36, 20008, 20078, 1, 'OnCourt', 5, 'Published', 55, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 37)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (37, 20009, NULL, 1, 'SelfSurvey', 10015, 'Published', 45, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 38)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (38, 20009, 20078, 1, 'OnCourt', 5, 'Published', 55, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 39)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (39, 20010, NULL, 2, 'SelfSurvey', 10013, 'Published', 40, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 40)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (40, 20010, 20079, 2, 'OnCourt', 5, 'Published', 50, 'B1');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 41)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (41, 20011, NULL, 2, 'SelfSurvey', 10014, 'Published', 40, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 42)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (42, 20011, 20079, 2, 'OnCourt', 5, 'Published', 50, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 43)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (43, 20012, NULL, 2, 'SelfSurvey', 10015, 'Published', 40, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 44)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (44, 20012, 20079, 2, 'OnCourt', 5, 'Published', 50, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 45)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (45, 20013, NULL, 1, 'SelfSurvey', 10012, 'Submitted', 30, 'B2');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 46)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (46, 20013, 20075, 1, 'OnCourt', 5, 'Submitted', 45, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 47)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (47, 1, NULL, 1, 'SelfSurvey', 1, 'Published', 55, 'B3');
IF NOT EXISTS (SELECT 1 FROM dbo.[assessments] WHERE [id] = 48)
    INSERT INTO dbo.[assessments] ([id], [player_sport_profile_id], [session_id], [rubric_id], [assessment_type], [assessor_id], [status], [total_score], [band_result]) VALUES (48, 20014, NULL, 2, 'SelfSurvey', 10012, 'Published', 65, 'B4');
SET IDENTITY_INSERT dbo.[assessments] OFF;
GO
SET IDENTITY_INSERT dbo.[assessment_scores] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 1)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (1, 1, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 2)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (2, 1, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 3)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (3, 1, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 4)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (4, 1, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 5)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (5, 1, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 6)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (6, 2, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 7)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (7, 2, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 8)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (8, 2, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 9)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (9, 2, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 10)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (10, 2, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 11)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (11, 3, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 12)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (12, 3, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 13)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (13, 3, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 14)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (14, 3, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 15)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (15, 3, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 16)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (16, 4, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 17)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (17, 4, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 18)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (18, 4, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 19)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (19, 4, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 20)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (20, 4, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 21)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (21, 5, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 22)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (22, 5, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 23)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (23, 5, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 24)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (24, 5, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 25)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (25, 5, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 26)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (26, 6, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 27)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (27, 6, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 28)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (28, 6, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 29)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (29, 6, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 30)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (30, 6, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 41)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (41, 9, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 42)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (42, 9, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 43)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (43, 9, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 44)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (44, 9, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 45)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (45, 9, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 46)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (46, 10, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 47)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (47, 10, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 48)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (48, 10, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 49)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (49, 10, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 50)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (50, 10, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 51)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (51, 11, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 52)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (52, 11, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 53)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (53, 11, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 54)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (54, 11, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 55)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (55, 11, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 56)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (56, 12, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 57)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (57, 12, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 58)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (58, 12, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 59)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (59, 12, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 60)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (60, 12, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 61)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (61, 13, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 62)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (62, 13, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 63)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (63, 13, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 64)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (64, 13, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 65)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (65, 13, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 66)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (66, 14, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 67)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (67, 14, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 68)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (68, 14, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 69)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (69, 14, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 70)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (70, 14, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 71)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (71, 15, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 72)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (72, 15, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 73)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (73, 15, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 74)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (74, 15, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 75)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (75, 15, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 76)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (76, 16, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 77)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (77, 16, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 78)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (78, 16, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 79)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (79, 16, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 80)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (80, 16, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 81)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (81, 17, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 82)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (82, 17, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 83)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (83, 17, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 84)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (84, 17, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 85)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (85, 17, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 86)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (86, 18, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 87)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (87, 18, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 88)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (88, 18, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 89)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (89, 18, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 90)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (90, 18, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 91)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (91, 19, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 92)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (92, 19, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 93)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (93, 19, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 94)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (94, 19, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 95)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (95, 19, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 96)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (96, 20, 6, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 97)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (97, 20, 7, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 98)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (98, 20, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 99)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (99, 20, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 100)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (100, 20, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 101)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (101, 21, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 102)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (102, 21, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 103)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (103, 21, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 104)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (104, 21, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 105)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (105, 21, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 106)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (106, 22, 6, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 107)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (107, 22, 7, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 108)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (108, 22, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 109)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (109, 22, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 110)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (110, 22, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 111)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (111, 23, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 112)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (112, 23, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 113)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (113, 23, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 114)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (114, 23, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 115)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (115, 23, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 116)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (116, 24, 6, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 117)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (117, 24, 7, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 118)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (118, 24, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 119)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (119, 24, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 120)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (120, 24, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 121)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (121, 25, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 122)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (122, 25, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 123)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (123, 25, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 124)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (124, 25, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 125)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (125, 25, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 126)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (126, 26, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 127)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (127, 26, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 128)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (128, 26, 3, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 129)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (129, 26, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 130)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (130, 26, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 131)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (131, 27, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 132)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (132, 27, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 133)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (133, 27, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 134)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (134, 27, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 135)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (135, 27, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 136)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (136, 28, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 137)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (137, 28, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 138)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (138, 28, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 139)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (139, 28, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 140)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (140, 28, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 141)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (141, 29, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 142)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (142, 29, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 143)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (143, 29, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 144)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (144, 29, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 145)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (145, 29, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 146)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (146, 30, 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 147)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (147, 30, 2, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 148)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (148, 30, 3, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 149)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (149, 30, 4, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 150)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (150, 30, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 151)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (151, 31, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 152)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (152, 31, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 153)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (153, 31, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 154)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (154, 31, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 155)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (155, 31, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 156)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (156, 32, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 157)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (157, 32, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 158)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (158, 32, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 159)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (159, 32, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 160)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (160, 32, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 161)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (161, 33, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 162)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (162, 33, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 163)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (163, 33, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 164)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (164, 33, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 165)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (165, 33, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 166)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (166, 34, 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 167)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (167, 34, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 168)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (168, 34, 3, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 169)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (169, 34, 4, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 170)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (170, 34, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 171)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (171, 35, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 172)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (172, 35, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 173)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (173, 35, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 174)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (174, 35, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 175)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (175, 35, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 176)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (176, 36, 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 177)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (177, 36, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 178)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (178, 36, 3, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 179)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (179, 36, 4, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 180)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (180, 36, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 181)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (181, 37, 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 182)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (182, 37, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 183)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (183, 37, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 184)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (184, 37, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 185)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (185, 37, 5, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 186)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (186, 38, 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 187)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (187, 38, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 188)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (188, 38, 3, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 189)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (189, 38, 4, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 190)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (190, 38, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 191)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (191, 39, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 192)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (192, 39, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 193)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (193, 39, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 194)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (194, 39, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 195)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (195, 39, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 196)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (196, 40, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 197)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (197, 40, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 198)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (198, 40, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 199)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (199, 40, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 200)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (200, 40, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 201)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (201, 41, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 202)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (202, 41, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 203)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (203, 41, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 204)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (204, 41, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 205)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (205, 41, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 206)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (206, 42, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 207)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (207, 42, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 208)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (208, 42, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 209)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (209, 42, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 210)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (210, 42, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 211)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (211, 43, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 212)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (212, 43, 7, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 213)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (213, 43, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 214)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (214, 43, 9, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 215)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (215, 43, 10, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 216)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (216, 44, 6, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 217)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (217, 44, 7, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 218)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (218, 44, 8, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 219)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (219, 44, 9, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 220)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (220, 44, 10, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 221)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (221, 45, 2, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 222)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (222, 45, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 223)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (223, 45, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 224)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (224, 45, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 225)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (225, 45, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 226)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (226, 46, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 227)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (227, 46, 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 228)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (228, 46, 4, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 229)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (229, 46, 1, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 230)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (230, 46, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 231)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (231, 47, 1, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 232)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (232, 47, 2, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 233)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (233, 47, 3, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 234)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (234, 47, 4, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 235)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (235, 47, 5, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 236)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (236, 48, 6, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 237)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (237, 48, 7, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 238)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (238, 48, 8, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 239)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (239, 48, 9, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[assessment_scores] WHERE [id] = 240)
    INSERT INTO dbo.[assessment_scores] ([id], [assessment_id], [dimension_id], [score]) VALUES (240, 48, 10, 4);
SET IDENTITY_INSERT dbo.[assessment_scores] OFF;
GO
SET IDENTITY_INSERT dbo.[player_rating_history] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 1)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (1, 1, NULL, 25, NULL, 'B2', 1, 1);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 2)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (2, 1, 25, 35, 'B2', 'B2', 5, 2);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 3)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (3, 2, NULL, 25, NULL, 'B2', 2, 3);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 4)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (4, 2, 25, 35, 'B2', 'B2', 5, 4);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 5)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (5, 3, NULL, 25, NULL, 'B2', 3, 5);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 6)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (6, 3, 25, 35, 'B2', 'B2', 5, 6);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 9)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (9, 5, NULL, 25, NULL, 'B2', 2, 9);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 10)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (10, 5, 25, 35, 'B2', 'B2', 5, 10);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 11)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (11, 6, NULL, 25, NULL, 'B2', 3, 11);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 12)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (12, 6, 25, 35, 'B2', 'B2', 5, 12);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 13)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (13, 7, NULL, 25, NULL, 'B2', 9, 13);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 14)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (14, 7, 25, 35, 'B2', 'B2', 5, 14);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 15)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (15, 8, NULL, 25, NULL, 'B2', 10, 15);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 16)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (16, 8, 25, 35, 'B2', 'B2', 5, 16);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 17)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (17, 9, NULL, 25, NULL, 'B2', 11, 17);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 18)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (18, 9, 25, 35, 'B2', 'B2', 5, 18);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 19)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (19, 10, NULL, 50, NULL, 'B3', 9, 19);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 20)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (20, 10, 50, 60, 'B3', 'B1', 5, 20);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 21)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (21, 11, NULL, 50, NULL, 'B3', 10, 21);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 22)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (22, 11, 50, 60, 'B3', 'B1', 5, 22);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 23)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (23, 12, NULL, 50, NULL, 'B3', 11, 23);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 24)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (24, 12, 50, 60, 'B3', 'B1', 5, 24);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 25)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (25, 10007, NULL, 30, NULL, 'B2', 12, 25);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 26)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (26, 10007, 30, 40, 'B2', 'B2', 5, 26);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 27)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (27, 10008, NULL, 15, NULL, 'B1', 12, 27);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 28)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (28, 10008, 15, 25, 'B1', 'B1', 5, 28);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 29)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (29, 10009, NULL, 55, NULL, 'B3', 13, 29);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 30)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (30, 10009, 55, 65, 'B3', 'B3', 5, 30);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 31)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (31, 10010, NULL, 40, NULL, 'B3', 13, 31);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 32)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (32, 10010, 40, 50, 'B3', 'B2', 5, 32);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 33)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (33, 20007, NULL, 45, NULL, 'B3', 10013, 33);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 34)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (34, 20007, 45, 55, 'B3', 'B1', 5, 34);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 35)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (35, 20008, NULL, 45, NULL, 'B3', 10014, 35);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 36)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (36, 20008, 45, 55, 'B3', 'B2', 5, 36);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 37)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (37, 20009, NULL, 45, NULL, 'B3', 10015, 37);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 38)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (38, 20009, 45, 55, 'B3', 'B3', 5, 38);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 39)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (39, 20010, NULL, 40, NULL, 'B3', 10013, 39);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 40)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (40, 20010, 40, 50, 'B3', 'B1', 5, 40);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 41)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (41, 20011, NULL, 40, NULL, 'B3', 10014, 41);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 42)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (42, 20011, 40, 50, 'B3', 'B2', 5, 42);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 43)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (43, 20012, NULL, 40, NULL, 'B3', 10015, 43);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 44)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (44, 20012, 40, 50, 'B3', 'B2', 5, 44);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 45)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (45, 20013, NULL, 30, NULL, 'B2', 10012, 45);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 46)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (46, 20013, 30, 45, 'B2', 'B3', 5, 46);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 47)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (47, 1, 35, 55, 'B2', 'B3', 1, 47);
IF NOT EXISTS (SELECT 1 FROM dbo.[player_rating_history] WHERE [id] = 48)
    INSERT INTO dbo.[player_rating_history] ([id], [player_sport_profile_id], [old_score], [new_score], [old_band], [new_band], [changed_by], [assessment_id]) VALUES (48, 20014, NULL, 65, NULL, 'B4', 10012, 48);
SET IDENTITY_INSERT dbo.[player_rating_history] OFF;
GO
SET IDENTITY_INSERT dbo.[social_booking_orders] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 1)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (1, 'ORD-SEED-TEST01', 2, 50000, 'Paid', '2026-10-15T04:22:52+00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 2)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (2, 'SBO-1791956649634-339', 9, 60000, 'Pending', '2026-10-14T05:59:10+00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10002)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10002, 'ORD-AN1-01', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10003)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10003, 'ORD-AN1-02', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10004)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10004, 'ORD-AN1-03', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10005)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10005, 'ORD-AN1-04', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10006)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10006, 'ORD-DP1-01', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10007)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10007, 'ORD-DP1-02', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10008)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10008, 'ORD-TD1-01', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10009)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10009, 'ORD-TD1-02', 12, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10010)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10010, 'ORD-AN2-01', 9, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10011)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10011, 'ORD-AN2-02', 10, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10012)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10012, 'ORD-AN2-03', 11, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10013)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10013, 'ORD-TD1-FULL-01', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10014)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10014, 'ORD-TD1-FULL-02', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10015)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10015, 'ORD-TD1-FULL-03', 11, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10016)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10016, 'ORD-TD1-FULL-04', 13, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10017)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10017, 'ORD-DP1-03', 9, 120000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10018)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10018, 'SBO-261020083320-6438', 10, 75000, 'Pending', '2026-10-20T08:48:20+00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 10019)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (10019, 'SBO-261020085847-2285', 10, 50000, 'Pending', '2026-10-20T09:13:47+00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20002)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20002, 'ORD-W44-20002-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20003)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20003, 'ORD-W44-20002-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20004)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20004, 'ORD-W44-20002-3', 11, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20005)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20005, 'ORD-W44-20003-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20006)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20006, 'ORD-W44-20003-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20007)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20007, 'ORD-W44-20004-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20008)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20008, 'ORD-W44-20004-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20009)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20009, 'ORD-W44-20004-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20010)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20010, 'ORD-W44-20004-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20011)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20011, 'ORD-W44-20004-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20012)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20012, 'ORD-W44-20005-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20013)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20013, 'ORD-W44-20005-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20014)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20014, 'ORD-W44-20005-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20015)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20015, 'ORD-W44-20005-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20016)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20016, 'ORD-W44-20006-1', 9, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20017)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20017, 'ORD-W44-20006-2', 10, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20018)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20018, 'ORD-W44-20006-3', 11, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20019)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20019, 'ORD-W44-20006-4', 12, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20020)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20020, 'ORD-W44-20007-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20021)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20021, 'ORD-W44-20007-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20022)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20022, 'ORD-W44-20008-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20023)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20023, 'ORD-W44-20008-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20024)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20024, 'ORD-W44-20008-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20025)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20025, 'ORD-W44-20009-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20026)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20026, 'ORD-W44-20009-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20027)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20027, 'ORD-W44-20009-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20028)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20028, 'ORD-W44-20009-4', 12, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20029)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20029, 'ORD-W44-20010-1', 9, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20030)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20030, 'ORD-W44-20011-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20031)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20031, 'ORD-W44-20011-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20032)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20032, 'ORD-W44-20011-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20033)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20033, 'ORD-W44-20012-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20034)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20034, 'ORD-W44-20012-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20035)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20035, 'ORD-W44-20013-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20036)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20036, 'ORD-W44-20013-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20037)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20037, 'ORD-W44-20013-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20038)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20038, 'ORD-W44-20013-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20039)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20039, 'ORD-W44-20013-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20040)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20040, 'ORD-W44-20013-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20041)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20041, 'ORD-W44-20013-7', 10014, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20042)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20042, 'ORD-W44-20013-8', 10015, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20043)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20043, 'ORD-W44-20014-1', 9, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20044)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20044, 'ORD-W44-20014-2', 10, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20045)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20045, 'ORD-W44-20014-3', 11, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20046)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20046, 'ORD-W44-20015-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20047)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20047, 'ORD-W44-20015-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20048)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20048, 'ORD-W44-20015-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20049)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20049, 'ORD-W44-20015-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20050)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20050, 'ORD-W44-20015-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20051)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20051, 'ORD-W44-20016-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20052)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20052, 'ORD-W44-20016-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20053)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20053, 'ORD-W44-20016-3', 11, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20054)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20054, 'ORD-W44-20016-4', 12, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20055)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20055, 'ORD-W44-20017-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20056)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20056, 'ORD-W44-20017-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20057)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20057, 'ORD-W44-20018-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20058)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20058, 'ORD-W44-20018-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20059)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20059, 'ORD-W44-20018-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20060)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20060, 'ORD-W44-20018-4', 12, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20061)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20061, 'ORD-W44-20018-5', 13, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20062)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20062, 'ORD-W44-20019-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20063)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20063, 'ORD-W44-20020-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20064)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20064, 'ORD-W44-20020-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20065)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20065, 'ORD-W44-20020-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20066)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20066, 'ORD-W44-20020-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20067)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20067, 'ORD-W44-20020-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20068)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20068, 'ORD-W44-20020-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20069)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20069, 'ORD-W44-20021-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20070)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20070, 'ORD-W44-20021-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20071)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20071, 'ORD-W44-20021-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20072)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20072, 'ORD-W44-20021-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20073)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20073, 'ORD-W44-20022-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20074)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20074, 'ORD-W44-20022-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20075)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20075, 'ORD-W44-20022-3', 11, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20076)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20076, 'ORD-W44-20023-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20077)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20077, 'ORD-W44-20023-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20078)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20078, 'ORD-W44-20024-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20079)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20079, 'ORD-W44-20024-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20080)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20080, 'ORD-W44-20024-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20081)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20081, 'ORD-W44-20024-4', 12, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20082)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20082, 'ORD-W44-20025-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20083)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20083, 'ORD-W44-20025-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20084)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20084, 'ORD-W44-20025-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20085)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20085, 'ORD-W44-20025-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20086)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20086, 'ORD-W44-20025-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20087)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20087, 'ORD-W44-20026-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20088)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20088, 'ORD-W44-20026-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20089)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20089, 'ORD-W44-20026-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20090)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20090, 'ORD-W44-20027-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20091)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20091, 'ORD-W44-20027-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20092)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20092, 'ORD-W44-20028-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20093)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20093, 'ORD-W44-20028-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20094)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20094, 'ORD-W44-20028-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20095)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20095, 'ORD-W44-20028-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20096)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20096, 'ORD-W44-20028-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20097)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20097, 'ORD-W44-20028-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20098)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20098, 'ORD-W44-20029-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20099)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20099, 'ORD-W44-20029-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20100)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20100, 'ORD-W44-20029-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20101)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20101, 'ORD-W44-20029-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20102)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20102, 'ORD-W44-20030-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20103)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20103, 'ORD-W44-20030-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20104)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20104, 'ORD-W44-20030-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20105)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20105, 'ORD-W44-20030-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20106)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20106, 'ORD-W44-20031-1', 9, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20107)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20107, 'ORD-W44-20031-2', 10, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20108)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20108, 'ORD-W44-20031-3', 11, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20109)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20109, 'ORD-W44-20032-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20110)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20110, 'ORD-W44-20032-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20111)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20111, 'ORD-W44-20032-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20112)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20112, 'ORD-W44-20032-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20113)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20113, 'ORD-W44-20032-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20114)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20114, 'ORD-W44-20032-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20115)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20115, 'ORD-W44-20032-7', 10014, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20116)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20116, 'ORD-W44-20032-8', 10015, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20117)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20117, 'ORD-W44-20033-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20118)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20118, 'ORD-W44-20033-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20119)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20119, 'ORD-W44-20033-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20120)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20120, 'ORD-W44-20033-4', 12, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20121)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20121, 'ORD-W44-20033-5', 13, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20122)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20122, 'ORD-W44-20034-1', 9, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20123)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20123, 'ORD-W44-20034-2', 10, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20124)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20124, 'ORD-W44-20035-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20125)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20125, 'ORD-W44-20035-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20126)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20126, 'ORD-W44-20035-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20127)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20127, 'ORD-W44-20035-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20128)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20128, 'ORD-W44-20036-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20129)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20129, 'ORD-W44-20036-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20130)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20130, 'ORD-W44-20036-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20131)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20131, 'ORD-W44-20036-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20132)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20132, 'ORD-W44-20036-5', 13, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20133)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20133, 'ORD-W44-20037-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20134)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20134, 'ORD-W44-20037-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20135)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20135, 'ORD-W44-20037-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20136)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20136, 'ORD-W44-20038-1', 9, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20137)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20137, 'ORD-W44-20038-2', 10, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20138)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20138, 'ORD-W44-20039-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20139)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20139, 'ORD-W44-20039-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20140)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20140, 'ORD-W44-20039-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20141)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20141, 'ORD-W44-20039-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20142)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20142, 'ORD-W44-20040-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20143)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20143, 'ORD-W44-20040-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20144)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20144, 'ORD-W44-20040-3', 11, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20145)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20145, 'ORD-W44-20040-4', 12, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20146)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20146, 'ORD-W44-20040-5', 13, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20147)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20147, 'ORD-W44-20040-6', 10013, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20148)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20148, 'ORD-W44-20041-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20149)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20149, 'ORD-W44-20041-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20150)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20150, 'ORD-W44-20041-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20151)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20151, 'ORD-W44-20041-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20152)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20152, 'ORD-W44-20042-1', 9, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20153)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20153, 'ORD-W44-20042-2', 10, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20154)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20154, 'ORD-W44-20043-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20155)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20155, 'ORD-W44-20043-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20156)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20156, 'ORD-W44-20044-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20157)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20157, 'ORD-W44-20044-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20158)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20158, 'ORD-W44-20044-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20159)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20159, 'ORD-W44-20045-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20160)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20160, 'ORD-W44-20045-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20161)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20161, 'ORD-W44-20045-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20162)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20162, 'ORD-W44-20045-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20163)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20163, 'ORD-W44-20046-1', 9, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20164)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20164, 'ORD-W44-20046-2', 10, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20165)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20165, 'ORD-W44-20047-1', 9, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20166)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20166, 'ORD-W44-20047-2', 10, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20167)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20167, 'ORD-W44-20047-3', 11, 100000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20168)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20168, 'ORD-W44-20048-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20169)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20169, 'ORD-W44-20049-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20170)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20170, 'ORD-W44-20049-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20171)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20171, 'ORD-W44-20049-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20172)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20172, 'ORD-W44-20050-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20173)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20173, 'ORD-W44-20050-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20174)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20174, 'ORD-W44-20050-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20175)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20175, 'ORD-W44-20050-4', 12, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20176)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20176, 'ORD-W44-20051-1', 9, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20177)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20177, 'ORD-W44-20051-2', 10, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20178)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20178, 'ORD-W44-20051-3', 11, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20179)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20179, 'ORD-W44-20051-4', 12, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20180)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20180, 'ORD-W44-20051-5', 13, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20181)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20181, 'ORD-W44-20052-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20182)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20182, 'ORD-W44-20052-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20183)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20183, 'ORD-W44-20053-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20184)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20184, 'ORD-W44-20053-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20185)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20185, 'ORD-W44-20053-3', 11, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20186)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20186, 'ORD-W44-20054-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20187)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20187, 'ORD-W44-20054-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20188)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20188, 'ORD-W44-20054-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20189)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20189, 'ORD-W44-20054-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20190)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20190, 'ORD-W44-20054-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20191)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20191, 'ORD-W44-20054-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20192)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20192, 'ORD-W44-20055-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20193)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20193, 'ORD-W44-20055-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20194)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20194, 'ORD-W44-20055-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20195)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20195, 'ORD-W44-20055-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20196)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20196, 'ORD-W44-20056-1', 9, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20197)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20197, 'ORD-W44-20056-2', 10, 80000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20198)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20198, 'ORD-W44-20057-1', 9, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20199)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20199, 'ORD-W44-20057-2', 10, 70000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20200)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20200, 'ORD-W44-20058-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20201)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20201, 'ORD-W44-20058-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20202)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20202, 'ORD-W44-20058-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20203)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20203, 'ORD-W44-20058-4', 12, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20204)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20204, 'ORD-W44-20059-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20205)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20205, 'ORD-W44-20059-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20206)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20206, 'ORD-W44-20060-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20207)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20207, 'ORD-W44-20060-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20208)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20208, 'ORD-W44-20060-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20209)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20209, 'ORD-W44-20060-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20210)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20210, 'ORD-W44-20060-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20211)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20211, 'ORD-W44-20061-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20212)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20212, 'ORD-W44-20061-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20213)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20213, 'ORD-W44-20061-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20214)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20214, 'ORD-W44-20062-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20215)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20215, 'ORD-W44-20062-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20216)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20216, 'ORD-W44-20063-1', 9, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20217)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20217, 'ORD-W44-20063-2', 10, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20218)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20218, 'ORD-W44-20063-3', 11, 75000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20219)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20219, 'ORD-W44-20064-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20220)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20220, 'ORD-W44-20064-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20221)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20221, 'ORD-W44-20064-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20222)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20222, 'ORD-W44-20064-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20223)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20223, 'ORD-W44-20064-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20224)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20224, 'ORD-W44-20064-6', 10013, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20225)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20225, 'ORD-W44-20064-7', 10014, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20226)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20226, 'ORD-W44-20064-8', 10015, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20227)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20227, 'ORD-W44-20065-1', 9, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20228)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20228, 'ORD-W44-20065-2', 10, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20229)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20229, 'ORD-W44-20065-3', 11, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20230)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20230, 'ORD-W44-20065-4', 12, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20231)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20231, 'ORD-W44-20066-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20232)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20232, 'ORD-W44-20066-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20233)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20233, 'ORD-W44-20067-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20234)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20234, 'ORD-W44-20067-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20235)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20235, 'ORD-W44-20067-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20236)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20236, 'ORD-W44-20067-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20237)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20237, 'ORD-W44-20067-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20238)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20238, 'ORD-W44-20068-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20239)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20239, 'ORD-W44-20068-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20240)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20240, 'ORD-W44-20068-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20241)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20241, 'ORD-W44-20069-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20242)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20242, 'ORD-W44-20069-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20243)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20243, 'ORD-W44-20069-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20244)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20244, 'ORD-W44-20069-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20245)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20245, 'ORD-W44-20070-1', 9, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20246)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20246, 'ORD-W44-20070-2', 10, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20247)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20247, 'ORD-W44-20071-1', 9, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20248)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20248, 'ORD-W44-20071-2', 10, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20249)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20249, 'ORD-W44-20071-3', 11, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20250)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20250, 'ORD-W44-20071-4', 12, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20251)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20251, 'ORD-W44-20071-5', 13, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20252)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20252, 'ORD-W44-20071-6', 10013, 90000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20253)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20253, 'ORD-W44-20072-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20254)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20254, 'ORD-W44-20072-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20255)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20255, 'ORD-W44-20072-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20256)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20256, 'ORD-W44-20073-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20257)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20257, 'ORD-W44-20073-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20258)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20258, 'ORD-W44-20073-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20259)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20259, 'ORD-W44-20074-1', 9, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20260)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20260, 'ORD-W44-20074-2', 10, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20261)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20261, 'ORD-W44-20074-3', 11, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20262)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20262, 'ORD-W44-20074-4', 12, 110000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20263)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20263, 'ORD-W44-20075-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20264)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20264, 'ORD-W44-20075-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20265)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20265, 'ORD-W44-20075-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20266)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20266, 'ORD-W44-20075-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20267)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20267, 'ORD-W44-20076-1', 9, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20268)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20268, 'ORD-W44-20076-2', 10, 105000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20269)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20269, 'ORD-W44-20077-1', 9, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20270)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20270, 'ORD-W44-20077-2', 10, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20271)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20271, 'ORD-W44-20077-3', 11, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20272)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20272, 'ORD-W44-20077-4', 12, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20273)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20273, 'ORD-W44-20077-5', 13, 85000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20274)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20274, 'ORD-W44-20078-1', 9, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20275)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20275, 'ORD-W44-20078-2', 10, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20276)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20276, 'ORD-W44-20078-3', 11, 95000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20277)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20277, 'ORD-W44-20079-1', 9, 115000, 'Paid', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[social_booking_orders] WHERE [id] = 20278)
    INSERT INTO dbo.[social_booking_orders] ([id], [order_code], [payer_id], [total_amount], [status], [expires_at]) VALUES (20278, 'ORD-W44-20079-2', 10, 115000, 'Paid', NULL);
SET IDENTITY_INSERT dbo.[social_booking_orders] OFF;
GO
SET IDENTITY_INSERT dbo.[tickets] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 1)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (1, 1, 1, 2, 'TCK-SEED-001', 'Confirmed', 50000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 2)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (2, 2, 2, 4, 'TCK-1791956649637-530', 'Held', 60000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10002)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10002, 10002, 10002, 4, 'TKSC-AN1-01', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10003)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10003, 10003, 10002, 5, 'TKSC-AN1-02', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10004)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10004, 10004, 10002, 6, 'TKSC-AN1-03', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10005)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10005, 10005, 10002, 7, 'TKSC-AN1-04', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10006)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10006, 10006, 10003, 4, 'TKSC-DP1-01', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10007)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10007, 10007, 10003, 5, 'TKSC-DP1-02', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10008)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10008, 10008, 10004, 6, 'TKSC-TD1-01', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10009)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10009, 10009, 10004, 7, 'TKSC-TD1-02', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10010)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10010, 10010, 10005, 4, 'TKSC-AN2-01', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10011)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10011, 10011, 10005, 5, 'TKSC-AN2-02', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10012)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10012, 10012, 10005, 6, 'TKSC-AN2-03', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10013)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10013, 10013, 10007, 4, 'TKSC-TD1-FULL-01', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10014)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10014, 10014, 10007, 5, 'TKSC-TD1-FULL-02', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10015)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10015, 10015, 10007, 6, 'TKSC-TD1-FULL-03', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10016)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10016, 10016, 10007, 8, 'TKSC-TD1-FULL-04', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10017)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10017, 10017, 10008, 4, 'TKSC-DP1-03', 'Confirmed', 120000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10018)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10018, 10018, 10004, 5, 'TKT-10018-261020083320-521', 'Pending', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 10019)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (10019, 10019, 1, 5, 'TKT-10019-261020085847-760', 'Pending', 50000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20002)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20002, 20002, 20002, 4, 'TKT-W44-20002-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20003)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20003, 20003, 20002, 5, 'TKT-W44-20002-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20004)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20004, 20004, 20002, 6, 'TKT-W44-20002-3', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20005)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20005, 20005, 20003, 4, 'TKT-W44-20003-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20006)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20006, 20006, 20003, 5, 'TKT-W44-20003-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20007)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20007, 20007, 20004, 4, 'TKT-W44-20004-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20008)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20008, 20008, 20004, 5, 'TKT-W44-20004-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20009)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20009, 20009, 20004, 6, 'TKT-W44-20004-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20010)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20010, 20010, 20004, 7, 'TKT-W44-20004-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20011)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20011, 20011, 20004, 8, 'TKT-W44-20004-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20012)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20012, 20012, 20005, 4, 'TKT-W44-20005-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20013)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20013, 20013, 20005, 5, 'TKT-W44-20005-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20014)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20014, 20014, 20005, 6, 'TKT-W44-20005-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20015)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20015, 20015, 20005, 7, 'TKT-W44-20005-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20016)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20016, 20016, 20006, 4, 'TKT-W44-20006-1', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20017)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20017, 20017, 20006, 5, 'TKT-W44-20006-2', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20018)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20018, 20018, 20006, 6, 'TKT-W44-20006-3', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20019)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20019, 20019, 20006, 7, 'TKT-W44-20006-4', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20020)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20020, 20020, 20007, 4, 'TKT-W44-20007-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20021)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20021, 20021, 20007, 5, 'TKT-W44-20007-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20022)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20022, 20022, 20008, 4, 'TKT-W44-20008-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20023)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20023, 20023, 20008, 5, 'TKT-W44-20008-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20024)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20024, 20024, 20008, 6, 'TKT-W44-20008-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20025)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20025, 20025, 20009, 4, 'TKT-W44-20009-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20026)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20026, 20026, 20009, 5, 'TKT-W44-20009-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20027)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20027, 20027, 20009, 6, 'TKT-W44-20009-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20028)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20028, 20028, 20009, 7, 'TKT-W44-20009-4', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20029)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20029, 20029, 20010, 4, 'TKT-W44-20010-1', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20030)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20030, 20030, 20011, 4, 'TKT-W44-20011-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20031)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20031, 20031, 20011, 5, 'TKT-W44-20011-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20032)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20032, 20032, 20011, 6, 'TKT-W44-20011-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20033)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20033, 20033, 20012, 4, 'TKT-W44-20012-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20034)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20034, 20034, 20012, 5, 'TKT-W44-20012-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20035)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20035, 20035, 20013, 4, 'TKT-W44-20013-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20036)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20036, 20036, 20013, 5, 'TKT-W44-20013-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20037)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20037, 20037, 20013, 6, 'TKT-W44-20013-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20038)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20038, 20038, 20013, 7, 'TKT-W44-20013-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20039)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20039, 20039, 20013, 8, 'TKT-W44-20013-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20040)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20040, 20040, 20013, 10008, 'TKT-W44-20013-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20041)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20041, 20041, 20013, 10009, 'TKT-W44-20013-7', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20042)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20042, 20042, 20013, 10010, 'TKT-W44-20013-8', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20043)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20043, 20043, 20014, 4, 'TKT-W44-20014-1', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20044)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20044, 20044, 20014, 5, 'TKT-W44-20014-2', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20045)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20045, 20045, 20014, 6, 'TKT-W44-20014-3', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20046)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20046, 20046, 20015, 4, 'TKT-W44-20015-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20047)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20047, 20047, 20015, 5, 'TKT-W44-20015-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20048)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20048, 20048, 20015, 6, 'TKT-W44-20015-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20049)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20049, 20049, 20015, 7, 'TKT-W44-20015-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20050)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20050, 20050, 20015, 8, 'TKT-W44-20015-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20051)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20051, 20051, 20016, 4, 'TKT-W44-20016-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20052)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20052, 20052, 20016, 5, 'TKT-W44-20016-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20053)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20053, 20053, 20016, 6, 'TKT-W44-20016-3', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20054)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20054, 20054, 20016, 7, 'TKT-W44-20016-4', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20055)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20055, 20055, 20017, 4, 'TKT-W44-20017-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20056)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20056, 20056, 20017, 5, 'TKT-W44-20017-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20057)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20057, 20057, 20018, 4, 'TKT-W44-20018-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20058)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20058, 20058, 20018, 5, 'TKT-W44-20018-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20059)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20059, 20059, 20018, 6, 'TKT-W44-20018-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20060)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20060, 20060, 20018, 7, 'TKT-W44-20018-4', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20061)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20061, 20061, 20018, 8, 'TKT-W44-20018-5', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20062)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20062, 20062, 20019, 4, 'TKT-W44-20019-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20063)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20063, 20063, 20020, 4, 'TKT-W44-20020-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20064)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20064, 20064, 20020, 5, 'TKT-W44-20020-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20065)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20065, 20065, 20020, 6, 'TKT-W44-20020-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20066)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20066, 20066, 20020, 7, 'TKT-W44-20020-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20067)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20067, 20067, 20020, 8, 'TKT-W44-20020-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20068)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20068, 20068, 20020, 10008, 'TKT-W44-20020-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20069)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20069, 20069, 20021, 4, 'TKT-W44-20021-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20070)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20070, 20070, 20021, 5, 'TKT-W44-20021-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20071)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20071, 20071, 20021, 6, 'TKT-W44-20021-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20072)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20072, 20072, 20021, 7, 'TKT-W44-20021-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20073)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20073, 20073, 20022, 4, 'TKT-W44-20022-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20074)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20074, 20074, 20022, 5, 'TKT-W44-20022-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20075)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20075, 20075, 20022, 6, 'TKT-W44-20022-3', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20076)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20076, 20076, 20023, 4, 'TKT-W44-20023-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20077)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20077, 20077, 20023, 5, 'TKT-W44-20023-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20078)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20078, 20078, 20024, 4, 'TKT-W44-20024-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20079)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20079, 20079, 20024, 5, 'TKT-W44-20024-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20080)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20080, 20080, 20024, 6, 'TKT-W44-20024-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20081)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20081, 20081, 20024, 7, 'TKT-W44-20024-4', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20082)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20082, 20082, 20025, 4, 'TKT-W44-20025-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20083)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20083, 20083, 20025, 5, 'TKT-W44-20025-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20084)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20084, 20084, 20025, 6, 'TKT-W44-20025-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20085)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20085, 20085, 20025, 7, 'TKT-W44-20025-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20086)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20086, 20086, 20025, 8, 'TKT-W44-20025-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20087)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20087, 20087, 20026, 4, 'TKT-W44-20026-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20088)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20088, 20088, 20026, 5, 'TKT-W44-20026-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20089)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20089, 20089, 20026, 6, 'TKT-W44-20026-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20090)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20090, 20090, 20027, 4, 'TKT-W44-20027-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20091)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20091, 20091, 20027, 5, 'TKT-W44-20027-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20092)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20092, 20092, 20028, 4, 'TKT-W44-20028-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20093)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20093, 20093, 20028, 5, 'TKT-W44-20028-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20094)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20094, 20094, 20028, 6, 'TKT-W44-20028-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20095)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20095, 20095, 20028, 7, 'TKT-W44-20028-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20096)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20096, 20096, 20028, 8, 'TKT-W44-20028-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20097)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20097, 20097, 20028, 10008, 'TKT-W44-20028-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20098)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20098, 20098, 20029, 4, 'TKT-W44-20029-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20099)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20099, 20099, 20029, 5, 'TKT-W44-20029-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20100)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20100, 20100, 20029, 6, 'TKT-W44-20029-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20101)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20101, 20101, 20029, 7, 'TKT-W44-20029-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20102)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20102, 20102, 20030, 4, 'TKT-W44-20030-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20103)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20103, 20103, 20030, 5, 'TKT-W44-20030-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20104)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20104, 20104, 20030, 6, 'TKT-W44-20030-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20105)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20105, 20105, 20030, 7, 'TKT-W44-20030-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20106)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20106, 20106, 20031, 4, 'TKT-W44-20031-1', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20107)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20107, 20107, 20031, 5, 'TKT-W44-20031-2', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20108)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20108, 20108, 20031, 6, 'TKT-W44-20031-3', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20109)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20109, 20109, 20032, 4, 'TKT-W44-20032-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20110)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20110, 20110, 20032, 5, 'TKT-W44-20032-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20111)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20111, 20111, 20032, 6, 'TKT-W44-20032-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20112)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20112, 20112, 20032, 7, 'TKT-W44-20032-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20113)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20113, 20113, 20032, 8, 'TKT-W44-20032-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20114)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20114, 20114, 20032, 10008, 'TKT-W44-20032-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20115)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20115, 20115, 20032, 10009, 'TKT-W44-20032-7', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20116)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20116, 20116, 20032, 10010, 'TKT-W44-20032-8', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20117)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20117, 20117, 20033, 4, 'TKT-W44-20033-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20118)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20118, 20118, 20033, 5, 'TKT-W44-20033-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20119)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20119, 20119, 20033, 6, 'TKT-W44-20033-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20120)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20120, 20120, 20033, 7, 'TKT-W44-20033-4', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20121)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20121, 20121, 20033, 8, 'TKT-W44-20033-5', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20122)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20122, 20122, 20034, 4, 'TKT-W44-20034-1', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20123)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20123, 20123, 20034, 5, 'TKT-W44-20034-2', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20124)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20124, 20124, 20035, 4, 'TKT-W44-20035-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20125)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20125, 20125, 20035, 5, 'TKT-W44-20035-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20126)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20126, 20126, 20035, 6, 'TKT-W44-20035-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20127)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20127, 20127, 20035, 7, 'TKT-W44-20035-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20128)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20128, 20128, 20036, 4, 'TKT-W44-20036-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20129)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20129, 20129, 20036, 5, 'TKT-W44-20036-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20130)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20130, 20130, 20036, 6, 'TKT-W44-20036-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20131)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20131, 20131, 20036, 7, 'TKT-W44-20036-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20132)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20132, 20132, 20036, 8, 'TKT-W44-20036-5', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20133)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20133, 20133, 20037, 4, 'TKT-W44-20037-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20134)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20134, 20134, 20037, 5, 'TKT-W44-20037-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20135)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20135, 20135, 20037, 6, 'TKT-W44-20037-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20136)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20136, 20136, 20038, 4, 'TKT-W44-20038-1', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20137)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20137, 20137, 20038, 5, 'TKT-W44-20038-2', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20138)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20138, 20138, 20039, 4, 'TKT-W44-20039-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20139)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20139, 20139, 20039, 5, 'TKT-W44-20039-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20140)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20140, 20140, 20039, 6, 'TKT-W44-20039-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20141)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20141, 20141, 20039, 7, 'TKT-W44-20039-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20142)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20142, 20142, 20040, 4, 'TKT-W44-20040-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20143)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20143, 20143, 20040, 5, 'TKT-W44-20040-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20144)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20144, 20144, 20040, 6, 'TKT-W44-20040-3', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20145)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20145, 20145, 20040, 7, 'TKT-W44-20040-4', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20146)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20146, 20146, 20040, 8, 'TKT-W44-20040-5', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20147)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20147, 20147, 20040, 10008, 'TKT-W44-20040-6', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20148)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20148, 20148, 20041, 4, 'TKT-W44-20041-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20149)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20149, 20149, 20041, 5, 'TKT-W44-20041-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20150)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20150, 20150, 20041, 6, 'TKT-W44-20041-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20151)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20151, 20151, 20041, 7, 'TKT-W44-20041-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20152)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20152, 20152, 20042, 4, 'TKT-W44-20042-1', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20153)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20153, 20153, 20042, 5, 'TKT-W44-20042-2', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20154)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20154, 20154, 20043, 4, 'TKT-W44-20043-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20155)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20155, 20155, 20043, 5, 'TKT-W44-20043-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20156)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20156, 20156, 20044, 4, 'TKT-W44-20044-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20157)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20157, 20157, 20044, 5, 'TKT-W44-20044-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20158)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20158, 20158, 20044, 6, 'TKT-W44-20044-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20159)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20159, 20159, 20045, 4, 'TKT-W44-20045-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20160)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20160, 20160, 20045, 5, 'TKT-W44-20045-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20161)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20161, 20161, 20045, 6, 'TKT-W44-20045-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20162)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20162, 20162, 20045, 7, 'TKT-W44-20045-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20163)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20163, 20163, 20046, 4, 'TKT-W44-20046-1', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20164)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20164, 20164, 20046, 5, 'TKT-W44-20046-2', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20165)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20165, 20165, 20047, 4, 'TKT-W44-20047-1', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20166)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20166, 20166, 20047, 5, 'TKT-W44-20047-2', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20167)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20167, 20167, 20047, 6, 'TKT-W44-20047-3', 'Confirmed', 100000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20168)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20168, 20168, 20048, 4, 'TKT-W44-20048-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20169)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20169, 20169, 20049, 4, 'TKT-W44-20049-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20170)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20170, 20170, 20049, 5, 'TKT-W44-20049-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20171)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20171, 20171, 20049, 6, 'TKT-W44-20049-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20172)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20172, 20172, 20050, 4, 'TKT-W44-20050-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20173)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20173, 20173, 20050, 5, 'TKT-W44-20050-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20174)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20174, 20174, 20050, 6, 'TKT-W44-20050-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20175)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20175, 20175, 20050, 7, 'TKT-W44-20050-4', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20176)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20176, 20176, 20051, 4, 'TKT-W44-20051-1', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20177)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20177, 20177, 20051, 5, 'TKT-W44-20051-2', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20178)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20178, 20178, 20051, 6, 'TKT-W44-20051-3', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20179)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20179, 20179, 20051, 7, 'TKT-W44-20051-4', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20180)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20180, 20180, 20051, 8, 'TKT-W44-20051-5', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20181)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20181, 20181, 20052, 4, 'TKT-W44-20052-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20182)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20182, 20182, 20052, 5, 'TKT-W44-20052-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20183)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20183, 20183, 20053, 4, 'TKT-W44-20053-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20184)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20184, 20184, 20053, 5, 'TKT-W44-20053-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20185)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20185, 20185, 20053, 6, 'TKT-W44-20053-3', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20186)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20186, 20186, 20054, 4, 'TKT-W44-20054-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20187)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20187, 20187, 20054, 5, 'TKT-W44-20054-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20188)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20188, 20188, 20054, 6, 'TKT-W44-20054-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20189)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20189, 20189, 20054, 7, 'TKT-W44-20054-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20190)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20190, 20190, 20054, 8, 'TKT-W44-20054-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20191)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20191, 20191, 20054, 10008, 'TKT-W44-20054-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20192)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20192, 20192, 20055, 4, 'TKT-W44-20055-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20193)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20193, 20193, 20055, 5, 'TKT-W44-20055-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20194)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20194, 20194, 20055, 6, 'TKT-W44-20055-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20195)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20195, 20195, 20055, 7, 'TKT-W44-20055-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20196)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20196, 20196, 20056, 4, 'TKT-W44-20056-1', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20197)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20197, 20197, 20056, 5, 'TKT-W44-20056-2', 'Confirmed', 80000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20198)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20198, 20198, 20057, 4, 'TKT-W44-20057-1', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20199)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20199, 20199, 20057, 5, 'TKT-W44-20057-2', 'Confirmed', 70000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20200)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20200, 20200, 20058, 4, 'TKT-W44-20058-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20201)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20201, 20201, 20058, 5, 'TKT-W44-20058-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20202)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20202, 20202, 20058, 6, 'TKT-W44-20058-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20203)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20203, 20203, 20058, 7, 'TKT-W44-20058-4', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20204)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20204, 20204, 20059, 4, 'TKT-W44-20059-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20205)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20205, 20205, 20059, 5, 'TKT-W44-20059-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20206)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20206, 20206, 20060, 4, 'TKT-W44-20060-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20207)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20207, 20207, 20060, 5, 'TKT-W44-20060-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20208)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20208, 20208, 20060, 6, 'TKT-W44-20060-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20209)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20209, 20209, 20060, 7, 'TKT-W44-20060-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20210)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20210, 20210, 20060, 8, 'TKT-W44-20060-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20211)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20211, 20211, 20061, 4, 'TKT-W44-20061-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20212)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20212, 20212, 20061, 5, 'TKT-W44-20061-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20213)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20213, 20213, 20061, 6, 'TKT-W44-20061-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20214)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20214, 20214, 20062, 4, 'TKT-W44-20062-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20215)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20215, 20215, 20062, 5, 'TKT-W44-20062-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20216)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20216, 20216, 20063, 4, 'TKT-W44-20063-1', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20217)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20217, 20217, 20063, 5, 'TKT-W44-20063-2', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20218)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20218, 20218, 20063, 6, 'TKT-W44-20063-3', 'Confirmed', 75000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20219)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20219, 20219, 20064, 4, 'TKT-W44-20064-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20220)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20220, 20220, 20064, 5, 'TKT-W44-20064-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20221)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20221, 20221, 20064, 6, 'TKT-W44-20064-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20222)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20222, 20222, 20064, 7, 'TKT-W44-20064-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20223)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20223, 20223, 20064, 8, 'TKT-W44-20064-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20224)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20224, 20224, 20064, 10008, 'TKT-W44-20064-6', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20225)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20225, 20225, 20064, 10009, 'TKT-W44-20064-7', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20226)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20226, 20226, 20064, 10010, 'TKT-W44-20064-8', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20227)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20227, 20227, 20065, 4, 'TKT-W44-20065-1', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20228)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20228, 20228, 20065, 5, 'TKT-W44-20065-2', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20229)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20229, 20229, 20065, 6, 'TKT-W44-20065-3', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20230)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20230, 20230, 20065, 7, 'TKT-W44-20065-4', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20231)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20231, 20231, 20066, 4, 'TKT-W44-20066-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20232)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20232, 20232, 20066, 5, 'TKT-W44-20066-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20233)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20233, 20233, 20067, 4, 'TKT-W44-20067-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20234)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20234, 20234, 20067, 5, 'TKT-W44-20067-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20235)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20235, 20235, 20067, 6, 'TKT-W44-20067-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20236)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20236, 20236, 20067, 7, 'TKT-W44-20067-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20237)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20237, 20237, 20067, 8, 'TKT-W44-20067-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20238)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20238, 20238, 20068, 4, 'TKT-W44-20068-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20239)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20239, 20239, 20068, 5, 'TKT-W44-20068-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20240)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20240, 20240, 20068, 6, 'TKT-W44-20068-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20241)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20241, 20241, 20069, 4, 'TKT-W44-20069-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20242)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20242, 20242, 20069, 5, 'TKT-W44-20069-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20243)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20243, 20243, 20069, 6, 'TKT-W44-20069-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20244)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20244, 20244, 20069, 7, 'TKT-W44-20069-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20245)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20245, 20245, 20070, 4, 'TKT-W44-20070-1', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20246)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20246, 20246, 20070, 5, 'TKT-W44-20070-2', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20247)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20247, 20247, 20071, 4, 'TKT-W44-20071-1', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20248)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20248, 20248, 20071, 5, 'TKT-W44-20071-2', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20249)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20249, 20249, 20071, 6, 'TKT-W44-20071-3', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20250)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20250, 20250, 20071, 7, 'TKT-W44-20071-4', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20251)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20251, 20251, 20071, 8, 'TKT-W44-20071-5', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20252)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20252, 20252, 20071, 10008, 'TKT-W44-20071-6', 'Confirmed', 90000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20253)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20253, 20253, 20072, 4, 'TKT-W44-20072-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20254)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20254, 20254, 20072, 5, 'TKT-W44-20072-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20255)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20255, 20255, 20072, 6, 'TKT-W44-20072-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20256)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20256, 20256, 20073, 4, 'TKT-W44-20073-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20257)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20257, 20257, 20073, 5, 'TKT-W44-20073-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20258)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20258, 20258, 20073, 6, 'TKT-W44-20073-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20259)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20259, 20259, 20074, 4, 'TKT-W44-20074-1', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20260)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20260, 20260, 20074, 5, 'TKT-W44-20074-2', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20261)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20261, 20261, 20074, 6, 'TKT-W44-20074-3', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20262)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20262, 20262, 20074, 7, 'TKT-W44-20074-4', 'Confirmed', 110000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20263)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20263, 20263, 20075, 4, 'TKT-W44-20075-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20264)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20264, 20264, 20075, 5, 'TKT-W44-20075-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20265)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20265, 20265, 20075, 6, 'TKT-W44-20075-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20266)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20266, 20266, 20075, 7, 'TKT-W44-20075-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20267)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20267, 20267, 20076, 4, 'TKT-W44-20076-1', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20268)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20268, 20268, 20076, 5, 'TKT-W44-20076-2', 'Confirmed', 105000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20269)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20269, 20269, 20077, 4, 'TKT-W44-20077-1', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20270)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20270, 20270, 20077, 5, 'TKT-W44-20077-2', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20271)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20271, 20271, 20077, 6, 'TKT-W44-20077-3', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20272)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20272, 20272, 20077, 7, 'TKT-W44-20077-4', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20273)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20273, 20273, 20077, 8, 'TKT-W44-20077-5', 'Confirmed', 85000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20274)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20274, 20274, 20078, 4, 'TKT-W44-20078-1', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20275)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20275, 20275, 20078, 5, 'TKT-W44-20078-2', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20276)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20276, 20276, 20078, 6, 'TKT-W44-20078-3', 'Confirmed', 95000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20277)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20277, 20277, 20079, 4, 'TKT-W44-20079-1', 'Confirmed', 115000, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[tickets] WHERE [id] = 20278)
    INSERT INTO dbo.[tickets] ([id], [order_id], [session_id], [player_id], [ticket_code], [status], [amount_vnd], [checked_in_by]) VALUES (20278, 20278, 20079, 5, 'TKT-W44-20079-2', 'Confirmed', 115000, NULL);
SET IDENTITY_INSERT dbo.[tickets] OFF;
GO
SET IDENTITY_INSERT dbo.[friendships] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[friendships] WHERE [id] = 1)
    INSERT INTO dbo.[friendships] ([id], [requester_id], [addressee_id], [status], [created_at], [responded_at]) VALUES (1, 1, 2, 'Accepted', '2026-10-14T12:04:32+07:00', '2026-10-14T12:04:32+07:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[friendships] WHERE [id] = 2)
    INSERT INTO dbo.[friendships] ([id], [requester_id], [addressee_id], [status], [created_at], [responded_at]) VALUES (2, 3, 1, 'Pending', '2026-10-14T12:04:32+07:00', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[friendships] WHERE [id] = 3)
    INSERT INTO dbo.[friendships] ([id], [requester_id], [addressee_id], [status], [created_at], [responded_at]) VALUES (3, 4, 5, 'Accepted', '2026-10-14T12:05:10+07:00', '2026-10-14T12:05:10+07:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[friendships] WHERE [id] = 4)
    INSERT INTO dbo.[friendships] ([id], [requester_id], [addressee_id], [status], [created_at], [responded_at]) VALUES (4, 6, 4, 'Pending', '2026-10-14T12:05:10+07:00', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.[friendships] WHERE [id] = 5)
    INSERT INTO dbo.[friendships] ([id], [requester_id], [addressee_id], [status], [created_at], [responded_at]) VALUES (5, 4, 1, 'Pending', '2026-10-14T12:14:34+07:00', NULL);
SET IDENTITY_INSERT dbo.[friendships] OFF;
GO
SET IDENTITY_INSERT dbo.[messages] ON;
IF NOT EXISTS (SELECT 1 FROM dbo.[messages] WHERE [id] = 1)
    INSERT INTO dbo.[messages] ([id], [sender_id], [recipient_id], [content], [is_read], [created_at]) VALUES (1, 9, 10, N'Chieu nay 18h di danh cau khong ban oi?', 1, '2026-10-14T12:25:32+07:00');
IF NOT EXISTS (SELECT 1 FROM dbo.[messages] WHERE [id] = 2)
    INSERT INTO dbo.[messages] ([id], [sender_id], [recipient_id], [content], [is_read], [created_at]) VALUES (2, 9, 10, N'hi', 0, '2026-10-14T12:58:21+07:00');
SET IDENTITY_INSERT dbo.[messages] OFF;
GO
GO

-- Stock units, barcode, receipt source, order buyer (upgrade 010, approved 2026-11-06)
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

-- The row with factor_to_base = 1 is the product's base unit; every stock quantity is counted in it.
IF OBJECT_ID(N'dbo.product_units', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_units (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_product_units PRIMARY KEY,
        product_id BIGINT NOT NULL CONSTRAINT FK_product_units_product_id REFERENCES dbo.products(id),
        unit_name NVARCHAR(50) NOT NULL,
        factor_to_base INT NOT NULL CONSTRAINT CK_product_units_factor CHECK (factor_to_base >= 1),
        barcode VARCHAR(64) NULL
    );
    CREATE UNIQUE INDEX UX_product_units_product_unit ON dbo.product_units(product_id, unit_name);
    CREATE UNIQUE INDEX UX_product_units_one_base ON dbo.product_units(product_id) WHERE factor_to_base = 1;
    CREATE UNIQUE INDEX UX_product_units_barcode ON dbo.product_units(barcode) WHERE barcode IS NOT NULL;
END;

INSERT dbo.product_units (product_id, unit_name, factor_to_base)
SELECT p.id, N'Cái', 1
FROM dbo.products p
WHERE NOT EXISTS (SELECT 1 FROM dbo.product_units u WHERE u.product_id = p.id AND u.factor_to_base = 1);

-- Existing movements keep NULL here: their real posting time and source are unknown.
IF COL_LENGTH(N'dbo.stock_movements', N'created_at') IS NULL
    ALTER TABLE dbo.stock_movements ADD created_at DATETIMEOFFSET(0) NULL
        CONSTRAINT DF_stock_movements_created_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.stock_movements', N'entered_unit') IS NULL
    ALTER TABLE dbo.stock_movements ADD entered_unit NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.stock_movements', N'entered_quantity') IS NULL
    ALTER TABLE dbo.stock_movements ADD entered_quantity INT NULL;
IF COL_LENGTH(N'dbo.stock_movements', N'source_ref') IS NULL
    ALTER TABLE dbo.stock_movements ADD source_ref VARCHAR(100) NULL;

IF COL_LENGTH(N'dbo.orders', N'buyer_id') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD buyer_id BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_buyer_id FOREIGN KEY (buyer_id) REFERENCES dbo.users(id);
END;
COMMIT;
GO
-- Separate batch: the column must exist before the index that references it is compiled.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_source_ref' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_source_ref ON dbo.stock_movements(stock_item_id, source_ref) WHERE source_ref IS NOT NULL;
GO

-- Stock reservations, supply requests, stock counts (upgrade 017, approved 2026-11-13)
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
BEGIN TRANSACTION;

-- 1. Available to sell = quantity_on_hand - reserved_quantity. Unpaid and paid-but-not-handed-over orders hold stock here.
IF COL_LENGTH(N'dbo.stock_items', N'reserved_quantity') IS NULL
    ALTER TABLE dbo.stock_items ADD reserved_quantity INT NOT NULL
        CONSTRAINT DF_stock_items_reserved_quantity DEFAULT 0;

-- 2. Goods order hold, cancellation and handover history. Orders created before this upgrade keep
--    reservation_state NULL: they never reserved stock, so nothing is released or consumed for them.
IF COL_LENGTH(N'dbo.orders', N'created_at') IS NULL
    ALTER TABLE dbo.orders ADD created_at DATETIMEOFFSET(0) NULL CONSTRAINT DF_orders_created_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.orders', N'expires_at') IS NULL
    ALTER TABLE dbo.orders ADD expires_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.orders', N'reservation_state') IS NULL
    ALTER TABLE dbo.orders ADD reservation_state VARCHAR(20) NULL;
IF COL_LENGTH(N'dbo.orders', N'cancelled_by') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD cancelled_by BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_cancelled_by FOREIGN KEY (cancelled_by) REFERENCES dbo.users(id);
END;
IF COL_LENGTH(N'dbo.orders', N'cancelled_at') IS NULL
    ALTER TABLE dbo.orders ADD cancelled_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.orders', N'delivered_by') IS NULL
BEGIN
    ALTER TABLE dbo.orders ADD delivered_by BIGINT NULL;
    ALTER TABLE dbo.orders ADD CONSTRAINT FK_orders_delivered_by FOREIGN KEY (delivered_by) REFERENCES dbo.users(id);
END;
IF COL_LENGTH(N'dbo.orders', N'delivered_at') IS NULL
    ALTER TABLE dbo.orders ADD delivered_at DATETIMEOFFSET(0) NULL;

-- 3a. Referee (or Manager) asks for supplies; Staff issues or rejects. Quantities are in the base unit.
IF OBJECT_ID(N'dbo.session_supply_requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.session_supply_requests (
        id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_session_supply_requests PRIMARY KEY,
        session_id BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_session_id REFERENCES dbo.sessions(id),
        product_id BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_product_id REFERENCES dbo.products(id),
        requested_by BIGINT NOT NULL CONSTRAINT FK_session_supply_requests_requested_by REFERENCES dbo.users(id),
        quantity INT NOT NULL CONSTRAINT CK_session_supply_requests_quantity CHECK (quantity > 0),
        entered_unit NVARCHAR(50) NULL,
        entered_quantity INT NULL,
        note NVARCHAR(300) NULL,
        status VARCHAR(20) NOT NULL CONSTRAINT DF_session_supply_requests_status DEFAULT 'Pending'
            CONSTRAINT CK_session_supply_requests_status CHECK (status IN ('Pending', 'Issued', 'Rejected', 'Cancelled')),
        issued_quantity INT NULL,
        handled_by BIGINT NULL CONSTRAINT FK_session_supply_requests_handled_by REFERENCES dbo.users(id),
        handled_at DATETIMEOFFSET(0) NULL,
        decision_note NVARCHAR(300) NULL,
        created_at DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_session_supply_requests_created_at DEFAULT SYSDATETIMEOFFSET()
    );
    CREATE INDEX IX_session_supply_requests_session ON dbo.session_supply_requests(session_id, status);
END;

-- 3b. Stock count: the count never changes stock; a separately approved adjustment is applied once.
IF COL_LENGTH(N'dbo.stock_counts', N'status') IS NULL
    ALTER TABLE dbo.stock_counts ADD status VARCHAR(20) NOT NULL CONSTRAINT DF_stock_counts_status DEFAULT 'Pending';
IF COL_LENGTH(N'dbo.stock_counts', N'reason') IS NULL
    ALTER TABLE dbo.stock_counts ADD reason NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'evidence') IS NULL
    ALTER TABLE dbo.stock_counts ADD evidence NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'entered_unit') IS NULL
    ALTER TABLE dbo.stock_counts ADD entered_unit NVARCHAR(50) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'entered_quantity') IS NULL
    ALTER TABLE dbo.stock_counts ADD entered_quantity INT NULL;
-- Last stock movement id seen when counting: the "data version" of RP3 UC-43.
IF COL_LENGTH(N'dbo.stock_counts', N'stock_version') IS NULL
    ALTER TABLE dbo.stock_counts ADD stock_version BIGINT NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'counted_at') IS NULL
    ALTER TABLE dbo.stock_counts ADD counted_at DATETIMEOFFSET(0) NULL CONSTRAINT DF_stock_counts_counted_at DEFAULT SYSDATETIMEOFFSET();
IF COL_LENGTH(N'dbo.stock_counts', N'decided_at') IS NULL
    ALTER TABLE dbo.stock_counts ADD decided_at DATETIMEOFFSET(0) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'decision_note') IS NULL
    ALTER TABLE dbo.stock_counts ADD decision_note NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.stock_counts', N'applied_movement_id') IS NULL
BEGIN
    ALTER TABLE dbo.stock_counts ADD applied_movement_id BIGINT NULL;
    ALTER TABLE dbo.stock_counts ADD CONSTRAINT FK_stock_counts_applied_movement_id FOREIGN KEY (applied_movement_id) REFERENCES dbo.stock_movements(id);
END;
COMMIT;
GO
-- Separate batch: constraints and filtered indexes compile after the new columns exist.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stock_items_reserved')
    ALTER TABLE dbo.stock_items ADD CONSTRAINT CK_stock_items_reserved CHECK (reserved_quantity >= 0 AND reserved_quantity <= quantity_on_hand);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_orders_reservation_state')
    ALTER TABLE dbo.orders ADD CONSTRAINT CK_orders_reservation_state CHECK (reservation_state IS NULL OR reservation_state IN ('Held', 'Released', 'Consumed'));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_stock_counts_status')
    ALTER TABLE dbo.stock_counts ADD CONSTRAINT CK_stock_counts_status CHECK (status IN ('NoDifference', 'Pending', 'Applied', 'Rejected'));
-- The expiry job looks for held, unpaid orders past their expiry.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_orders_reservation_expiry' AND object_id = OBJECT_ID(N'dbo.orders'))
    CREATE INDEX IX_orders_reservation_expiry ON dbo.orders(expires_at) WHERE reservation_state = 'Held';
-- Database-level "exactly once": one adjustment movement per stock count, one sale-out per order and stock item.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_stock_count' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_stock_count ON dbo.stock_movements(reference_id) WHERE reference_type = 'StockCount';
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_stock_movements_sale_out' AND object_id = OBJECT_ID(N'dbo.stock_movements'))
    CREATE UNIQUE INDEX UX_stock_movements_sale_out ON dbo.stock_movements(stock_item_id, reference_id) WHERE reference_type = 'GoodsOrder' AND movement_type = 'SaleOut';
COMMIT;
GO
-- Paid orders still waiting for handover must keep their goods: hold them now when stock allows.
-- Unpaid legacy carts are left without a hold; they reserve when payment starts.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @orderId BIGINT = 0;
WHILE 1 = 1
BEGIN
    SELECT TOP (1) @orderId = o.id FROM dbo.orders o
    WHERE o.id > @orderId AND o.reservation_state IS NULL AND o.payment_state = 'Paid'
      AND ISNULL(o.handover_state, 'Pending') <> 'Received'
    ORDER BY o.id;
    IF @@ROWCOUNT = 0 BREAK;
    -- One order at a time, so earlier holds count against later ones.
    IF NOT EXISTS (
        SELECT 1 FROM (SELECT product_id, SUM(quantity) AS qty FROM dbo.order_items WHERE order_id = @orderId GROUP BY product_id) need
        LEFT JOIN dbo.stock_items si ON si.product_id = need.product_id
        WHERE si.id IS NULL OR si.quantity_on_hand - si.reserved_quantity < need.qty)
    BEGIN
        UPDATE si SET reserved_quantity = si.reserved_quantity + need.qty
        FROM dbo.stock_items si
        JOIN (SELECT product_id, SUM(quantity) AS qty FROM dbo.order_items WHERE order_id = @orderId GROUP BY product_id) need
          ON need.product_id = si.product_id;
        UPDATE dbo.orders SET reservation_state = 'Held' WHERE id = @orderId;
    END;
END;
COMMIT;
GO
-- Multi-center databases (upgrade 011): supply requests follow their session's center.
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = N'SportCenterIsolation')
   AND NOT EXISTS (SELECT 1 FROM sys.security_predicates WHERE target_object_id = OBJECT_ID(N'dbo.session_supply_requests'))
    EXEC(N'ALTER SECURITY POLICY dbo.SportCenterIsolation
        ADD FILTER PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests,
        ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests AFTER INSERT,
        ADD BLOCK PREDICATE dbo.sc_session(session_id) ON dbo.session_supply_requests AFTER UPDATE;');
GO
