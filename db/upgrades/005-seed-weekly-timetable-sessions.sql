-- Upgrade 005: Seed lively sessions and demo participant tickets for Current Week (26/10 - 01/11/2026) and Next Week (02/11 - 08/11/2026)
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

DECLARE @sportBadmintonId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'badminton');
DECLARE @sportPickleballId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'pickleball');
DECLARE @courtBadmintonId BIGINT = (SELECT MIN(id) FROM dbo.courts WHERE sport_id = @sportBadmintonId AND is_active = 1);
DECLARE @courtPickleballId BIGINT = (SELECT MIN(id) FROM dbo.courts WHERE sport_id = @sportPickleballId AND is_active = 1);
DECLARE @managerId BIGINT = ISNULL(
    (SELECT TOP 1 u.id FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id WHERE r.code = 'Manager' ORDER BY u.id),
    (SELECT TOP 1 id FROM dbo.users ORDER BY id)
);

-- 1. Ensure 8 demo players exist so full 8/8 sessions have distinct participant rosters
DECLARE @demoAccounts TABLE (
    seq INT IDENTITY(1,1),
    email NVARCHAR(320),
    full_name NVARCHAR(255),
    gender VARCHAR(100),
    avatar_url VARCHAR(2048),
    badminton_band VARCHAR(100),
    pickleball_band VARCHAR(100)
);

INSERT INTO @demoAccounts (email, full_name, gender, avatar_url, badminton_band, pickleball_band)
VALUES
    (N'player1@tksc.local', N'Nguyễn Văn An', 'Male', 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=100', 'B2', 'B1'),
    (N'player2@tksc.local', N'Trần Thị Bình', 'Female', 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=100', 'B2', 'B1'),
    (N'player3@tksc.local', N'Lê Hoàng Cường', 'Male', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=100', 'B2', 'B1'),
    (N'lananh.player@tksc.local', N'Phạm Thị Lan Anh', 'Female', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100', 'B2', 'B1'),
    (N'triet.vo@tksc.local', N'Võ Minh Triết', 'Male', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100', 'B3', 'B2'),
    (N'khanh.do@tksc.local', N'Đỗ Quốc Khánh', 'Male', 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=100', 'B1', 'B1'),
    (N'my.nguyen@tksc.local', N'Nguyễn Trà My', 'Female', 'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=100', 'B2', 'B2'),
    (N'huy.hoang@tksc.local', N'Hoàng Gia Huy', 'Male', 'https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=100', 'B3', 'B2');

INSERT INTO dbo.users (role_id, email, full_name, password_hash, status)
SELECT 1, d.email, d.full_name, NULL, 'Active'
FROM @demoAccounts d
WHERE NOT EXISTS (SELECT 1 FROM dbo.users u WHERE u.email = d.email);

INSERT INTO dbo.players (user_id, date_of_birth, gender, avatar_url)
SELECT u.id, '1999-05-20', d.gender, d.avatar_url
FROM @demoAccounts d
JOIN dbo.users u ON u.email = d.email
WHERE NOT EXISTS (SELECT 1 FROM dbo.players p WHERE p.user_id = u.id);

INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
SELECT p.id, @sportBadmintonId, 55, d.badminton_band, 'Rated'
FROM @demoAccounts d
JOIN dbo.users u ON u.email = d.email
JOIN dbo.players p ON p.user_id = u.id
WHERE NOT EXISTS (SELECT 1 FROM dbo.player_sport_profiles psp WHERE psp.player_id = p.id AND psp.sport_id = @sportBadmintonId);

INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
SELECT p.id, @sportPickleballId, 50, d.pickleball_band, 'Rated'
FROM @demoAccounts d
JOIN dbo.users u ON u.email = d.email
JOIN dbo.players p ON p.user_id = u.id
WHERE NOT EXISTS (SELECT 1 FROM dbo.player_sport_profiles psp WHERE psp.player_id = p.id AND psp.sport_id = @sportPickleballId);

DECLARE @demoPool TABLE (
    rn INT IDENTITY(1,1),
    user_id BIGINT,
    player_id BIGINT
);

INSERT INTO @demoPool (user_id, player_id)
SELECT u.id, p.id
FROM @demoAccounts d
JOIN dbo.users u ON u.email = d.email
JOIN dbo.players p ON p.user_id = u.id
ORDER BY d.seq;

-- 2. Define weekly schedule across Current Week (26/10 - 01/11) and Next Week (02/11 - 08/11)
DECLARE @schedule TABLE (
    row_id INT IDENTITY(1,1),
    sport_id BIGINT,
    court_id BIGINT,
    time_slot_id BIGINT,
    session_date DATE,
    band VARCHAR(100) NULL,
    capacity SMALLINT,
    price_vnd INT,
    occupied SMALLINT
);

INSERT INTO @schedule (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, occupied)
VALUES
    -- ================= CURRENT WEEK: 26/10/2026 - 01/11/2026 =================
    -- Mon 26/10
    (@sportBadmintonId,  @courtBadmintonId,  1, '2026-10-26', 'B1', 8,  70000, 3),
    (@sportPickleballId, @courtPickleballId, 4, '2026-10-26', 'B1', 4,  95000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-26', 'B2', 8,  85000, 5),
    (@sportPickleballId, @courtPickleballId, 5, '2026-10-26', 'B2', 4, 110000, 4), -- Full
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-10-26', NULL, 8,  80000, 4),

    -- Tue 27/10
    (@sportPickleballId, @courtPickleballId, 2, '2026-10-27', NULL, 6,  90000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-10-27', 'B1', 8,  75000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-27', 'B3', 6,  95000, 4),
    (@sportPickleballId, @courtPickleballId, 6, '2026-10-27', 'B1', 4, 100000, 1),

    -- Wed 28/10
    (@sportPickleballId, @courtPickleballId, 1, '2026-10-28', 'B1', 4,  95000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  3, '2026-10-28', 'B1', 8,  70000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-28', 'B2', 8,  85000, 8), -- Full
    (@sportPickleballId, @courtPickleballId, 5, '2026-10-28', NULL, 6, 100000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-10-28', 'B2', 8,  85000, 5),

    -- Thu 29/10 (Today)
    (@sportBadmintonId,  @courtBadmintonId,  1, '2026-10-29', 'B1', 8,  70000, 4),
    (@sportPickleballId, @courtPickleballId, 2, '2026-10-29', 'B1', 4,  95000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-10-29', 'B2', 8,  75000, 5),
    (@sportPickleballId, @courtPickleballId, 4, '2026-10-29', NULL, 4,  95000, 1),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-29', 'B2', 8,  85000, 6),
    (@sportPickleballId, @courtPickleballId, 5, '2026-10-29', 'B2', 4, 110000, 4), -- Full
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-10-29', 'B3', 6,  90000, 3),

    -- Fri 30/10
    (@sportPickleballId, @courtPickleballId, 3, '2026-10-30', 'B1', 6,  90000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-10-30', 'B1', 8,  75000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-30', 'B2', 8,  85000, 5),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-10-30', NULL, 8,  85000, 3),
    (@sportPickleballId, @courtPickleballId, 6, '2026-10-30', 'B2', 4, 110000, 2),

    -- Sat 31/10
    (@sportBadmintonId,  @courtBadmintonId,  1, '2026-10-31', 'B1', 8,  85000, 6),
    (@sportPickleballId, @courtPickleballId, 1, '2026-10-31', 'B1', 6, 110000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  2, '2026-10-31', 'B2', 8,  85000, 4),
    (@sportPickleballId, @courtPickleballId, 3, '2026-10-31', NULL, 6, 105000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-10-31', 'B2', 8,  85000, 8), -- Full
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-10-31', 'B3', 8,  95000, 5),
    (@sportPickleballId, @courtPickleballId, 5, '2026-10-31', 'B2', 4, 115000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-10-31', NULL, 8,  85000, 4),

    -- Sun 01/11
    (@sportPickleballId, @courtPickleballId, 1, '2026-11-01', NULL, 6, 110000, 5),
    (@sportBadmintonId,  @courtBadmintonId,  2, '2026-11-01', 'B1', 8,  85000, 3),
    (@sportPickleballId, @courtPickleballId, 2, '2026-11-01', 'B2', 4, 115000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-11-01', 'B2', 8,  85000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-01', 'B2', 8,  90000, 6),
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-01', 'B1', 4, 110000, 4), -- Full
    (@sportPickleballId, @courtPickleballId, 6, '2026-11-01', NULL, 6, 100000, 2),

    -- ================= NEXT WEEK: 02/11/2026 - 08/11/2026 =================
    -- Mon 02/11
    (@sportBadmintonId,  @courtBadmintonId,  1, '2026-11-02', 'B1', 8,  70000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-11-02', 'B2', 8,  75000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-02', 'B2', 8,  85000, 4),
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-02', 'B1', 4, 105000, 2),
    (@sportPickleballId, @courtPickleballId, 6, '2026-11-02', NULL, 6, 100000, 3),

    -- Tue 03/11
    (@sportPickleballId, @courtPickleballId, 2, '2026-11-03', 'B1', 4,  95000, 1),
    (@sportPickleballId, @courtPickleballId, 4, '2026-11-03', NULL, 6,  95000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-03', 'B3', 6,  95000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-11-03', 'B1', 8,  80000, 5),

    -- Wed 04/11
    (@sportPickleballId, @courtPickleballId, 1, '2026-11-04', NULL, 6,  95000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  3, '2026-11-04', 'B1', 8,  70000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-04', 'B2', 8,  85000, 6),
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-04', 'B2', 4, 110000, 4), -- Full
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-11-04', NULL, 8,  80000, 2),

    -- Thu 05/11
    (@sportBadmintonId,  @courtBadmintonId,  2, '2026-11-05', 'B1', 8,  70000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-11-05', 'B2', 8,  75000, 4),
    (@sportPickleballId, @courtPickleballId, 4, '2026-11-05', 'B1', 4,  95000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-05', 'B2', 8,  85000, 5),
    (@sportPickleballId, @courtPickleballId, 6, '2026-11-05', 'B2', 4, 110000, 3),

    -- Fri 06/11
    (@sportPickleballId, @courtPickleballId, 3, '2026-11-06', 'B1', 6,  90000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-11-06', 'B1', 8,  75000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-06', 'B2', 8,  85000, 8), -- Full
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-06', NULL, 6, 105000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-11-06', 'B3', 6,  90000, 2),

    -- Sat 07/11
    (@sportBadmintonId,  @courtBadmintonId,  1, '2026-11-07', 'B1', 8,  85000, 5),
    (@sportPickleballId, @courtPickleballId, 1, '2026-11-07', 'B1', 6, 110000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  2, '2026-11-07', 'B2', 8,  85000, 4),
    (@sportPickleballId, @courtPickleballId, 4, '2026-11-07', 'B2', 4, 115000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-07', 'B2', 8,  90000, 6),
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-07', NULL, 6, 110000, 3),
    (@sportBadmintonId,  @courtBadmintonId,  6, '2026-11-07', NULL, 8,  85000, 3),

    -- Sun 08/11
    (@sportPickleballId, @courtPickleballId, 1, '2026-11-08', NULL, 6, 110000, 4),
    (@sportBadmintonId,  @courtBadmintonId,  2, '2026-11-08', 'B1', 8,  85000, 4),
    (@sportPickleballId, @courtPickleballId, 3, '2026-11-08', 'B1', 6, 105000, 2),
    (@sportBadmintonId,  @courtBadmintonId,  4, '2026-11-08', 'B2', 8,  85000, 5),
    (@sportBadmintonId,  @courtBadmintonId,  5, '2026-11-08', 'B3', 6,  95000, 3),
    (@sportPickleballId, @courtPickleballId, 5, '2026-11-08', 'B2', 4, 115000, 2);

-- 3. Insert sessions and demo tickets idempotently
DECLARE @i INT = 1;
DECLARE @total INT = (SELECT COUNT(*) FROM @schedule);

WHILE @i <= @total
BEGIN
    DECLARE @spId BIGINT, @cId BIGINT, @tsId BIGINT, @sDate DATE, @band VARCHAR(100), @cap SMALLINT, @price INT, @occ SMALLINT;
    SELECT
        @spId = sport_id,
        @cId = court_id,
        @tsId = time_slot_id,
        @sDate = session_date,
        @band = band,
        @cap = capacity,
        @price = price_vnd,
        @occ = occupied
    FROM @schedule
    WHERE row_id = @i;

    DECLARE @sessId BIGINT = (
        SELECT id FROM dbo.sessions
        WHERE court_id = @cId AND session_date = @sDate AND time_slot_id = @tsId
    );

    IF @sessId IS NULL
    BEGIN
        INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
        VALUES (@spId, @cId, @tsId, @sDate, @band, @cap, @price, 'Published', @managerId);
        SET @sessId = SCOPE_IDENTITY();
    END;

    -- Populate demo tickets up to @occ if not already populated
    DECLARE @existingCount INT = (
        SELECT COUNT(*) FROM dbo.tickets WHERE session_id = @sessId
    );

    DECLARE @k INT = @existingCount + 1;
    WHILE @k <= @occ AND @k <= 8
    BEGIN
        DECLARE @pUserId BIGINT, @pPlayerId BIGINT;
        SELECT @pUserId = user_id, @pPlayerId = player_id
        FROM @demoPool
        WHERE rn = @k;

        DECLARE @ordCode VARCHAR(100) = CONCAT('ORD-W44-', @sessId, '-', @k);
        DECLARE @tktCode VARCHAR(100) = CONCAT('TKT-W44-', @sessId, '-', @k);

        IF NOT EXISTS (SELECT 1 FROM dbo.social_booking_orders WHERE order_code = @ordCode)
        BEGIN
            INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at)
            VALUES (@ordCode, @pUserId, @price, 'Paid', NULL);
            DECLARE @ordId BIGINT = SCOPE_IDENTITY();

            INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
            VALUES (@ordId, @sessId, @pPlayerId, @tktCode, 'Confirmed', @price);
        END;

        SET @k = @k + 1;
    END;

    SET @i = @i + 1;
END;

COMMIT;

