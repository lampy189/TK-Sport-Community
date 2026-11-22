-- Prepared for review; NOT applied automatically at startup.
-- Additive storage only. Does not create guests, tickets, prices, or change skill scores.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock @Resource=N'TKSC:upgrade:021:zalo',
    @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
IF @lockResult < 0 THROW 51000, 'Cannot acquire Zalo migration lock.', 1;
IF OBJECT_ID(N'dbo.users',N'U') IS NULL OR OBJECT_ID(N'dbo.social_booking_orders',N'U') IS NULL
    THROW 51000, 'Missing TKSC base schema.', 1;

IF OBJECT_ID(N'dbo.zalo_customers',N'U') IS NULL
CREATE TABLE dbo.zalo_customers (
    id bigint IDENTITY PRIMARY KEY,
    oa_id varchar(100) NOT NULL,
    sender_id varchar(100) NOT NULL,
    user_id bigint NULL REFERENCES dbo.users(id),
    display_name nvarchar(255) NULL,
    created_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_zalo_customers_identity UNIQUE(oa_id,sender_id)
);

IF OBJECT_ID(N'dbo.zalo_skill_declarations',N'U') IS NULL
CREATE TABLE dbo.zalo_skill_declarations (
    id bigint IDENTITY PRIMARY KEY,
    customer_id bigint NOT NULL REFERENCES dbo.zalo_customers(id),
    sport_id bigint NOT NULL REFERENCES dbo.sports(id),
    declared_level nvarchar(100) NOT NULL,
    mapped_band varchar(100) NULL,
    source_message_id varchar(200) NOT NULL,
    declared_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_zalo_skill_declaration UNIQUE(customer_id,sport_id,source_message_id)
);

IF OBJECT_ID(N'dbo.zalo_conversations',N'U') IS NULL
CREATE TABLE dbo.zalo_conversations (
    customer_id bigint PRIMARY KEY REFERENCES dbo.zalo_customers(id),
    state_json nvarchar(max) NOT NULL DEFAULT N'{}' CHECK(ISJSON(state_json)=1),
    expires_at datetimeoffset(0) NOT NULL,
    updated_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    revision rowversion
);

IF OBJECT_ID(N'dbo.zalo_inbox',N'U') IS NULL
CREATE TABLE dbo.zalo_inbox (
    id bigint IDENTITY PRIMARY KEY,
    oa_id varchar(100) NOT NULL,
    sender_id varchar(100) NOT NULL,
    message_id varchar(200) NOT NULL,
    message_text nvarchar(1200) NOT NULL,
    event_timestamp bigint NOT NULL CHECK(event_timestamp>0),
    state varchar(20) NOT NULL DEFAULT 'pending'
        CHECK(state IN ('pending','processing','completed','failed')),
    attempts int NOT NULL DEFAULT 0 CHECK(attempts>=0),
    lease_until datetimeoffset(0) NULL,
    received_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_zalo_inbox_message UNIQUE(oa_id,message_id)
);

IF OBJECT_ID(N'dbo.zalo_booking_orders',N'U') IS NULL
CREATE TABLE dbo.zalo_booking_orders (
    order_id bigint PRIMARY KEY REFERENCES dbo.social_booking_orders(id),
    customer_id bigint NOT NULL REFERENCES dbo.zalo_customers(id),
    confirmation_message_id varchar(200) NOT NULL,
    confirmed_quote_json nvarchar(max) NOT NULL CHECK(ISJSON(confirmed_quote_json)=1),
    created_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_zalo_booking_confirmation UNIQUE(customer_id,confirmation_message_id)
);

IF OBJECT_ID(N'dbo.zalo_outbox',N'U') IS NULL
CREATE TABLE dbo.zalo_outbox (
    id bigint IDENTITY PRIMARY KEY,
    customer_id bigint NOT NULL REFERENCES dbo.zalo_customers(id),
    deduplication_key varchar(200) NOT NULL,
    order_id bigint NULL REFERENCES dbo.social_booking_orders(id),
    payload_json nvarchar(max) NOT NULL CHECK(ISJSON(payload_json)=1),
    state varchar(20) NOT NULL DEFAULT 'pending'
        CHECK(state IN ('pending','sending','sent','unknown','failed','expired')),
    provider_message_id varchar(200) NULL,
    attempts int NOT NULL DEFAULT 0 CHECK(attempts>=0),
    lease_until datetimeoffset(0) NULL,
    expires_at datetimeoffset(0) NULL,
    created_at datetimeoffset(0) NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT UQ_zalo_outbox_deduplication UNIQUE(customer_id,deduplication_key)
);
COMMIT;
