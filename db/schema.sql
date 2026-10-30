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
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_requester_id')
    ALTER TABLE dbo.[friendships] ADD CONSTRAINT [FK_friendships_requester_id] FOREIGN KEY ([requester_id]) REFERENCES dbo.[users]([id]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_friendships_addressee_id')
    ALTER TABLE dbo.[friendships] ADD CONSTRAINT [FK_friendships_addressee_id] FOREIGN KEY ([addressee_id]) REFERENCES dbo.[users]([id]);
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
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Player')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Player',N'Player');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Referee')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Referee',N'Referee');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Staff')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Staff',N'Staff');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Manager')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Manager',N'Manager');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'Owner')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('Owner',N'Owner');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[roles] WHERE [code] = 'SystemAdmin')
INSERT INTO dbo.[roles] ([code],[name]) VALUES ('SystemAdmin',N'SystemAdmin');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[sports] WHERE [code] = 'badminton')
INSERT INTO dbo.[sports] ([code],[name]) VALUES ('badminton',N'Badminton');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.[sports] WHERE [code] = 'pickleball')
INSERT INTO dbo.[sports] ([code],[name]) VALUES ('pickleball',N'Pickleball');
GO
