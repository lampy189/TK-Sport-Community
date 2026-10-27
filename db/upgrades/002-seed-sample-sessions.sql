-- Upgrade 002: Seed realistic sample sessions, venues, courts and tickets matching Figma mockup
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

-- 1. Ensure Venues exist
IF NOT EXISTS (SELECT 1 FROM dbo.venues WHERE name = N'Sân An Nhiên - Bình Thạnh')
BEGIN
    INSERT INTO dbo.venues (name, address, is_active)
    VALUES (N'Sân An Nhiên - Bình Thạnh', N'45/2 Nguyễn Gia Trí, Phường 25, Bình Thạnh, TP.HCM', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.venues WHERE name = N'Sân D-Pickle - Quận 2')
BEGIN
    INSERT INTO dbo.venues (name, address, is_active)
    VALUES (N'Sân D-Pickle - Quận 2', N'88 Song Hành, An Phú, Quận 2, TP.HCM', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.venues WHERE name = N'Sân Thảo Điền - Thủ Đức')
BEGIN
    INSERT INTO dbo.venues (name, address, is_active)
    VALUES (N'Sân Thảo Điền - Thủ Đức', N'12 Quốc Hương, Thảo Điền, TP. Thủ Đức, TP.HCM', 1);
END;

-- 2. Ensure Courts exist
DECLARE @venueAnNhienId BIGINT = (SELECT id FROM dbo.venues WHERE name = N'Sân An Nhiên - Bình Thạnh');
DECLARE @venueDPickleId BIGINT = (SELECT id FROM dbo.venues WHERE name = N'Sân D-Pickle - Quận 2');
DECLARE @venueThaoDienId BIGINT = (SELECT id FROM dbo.venues WHERE name = N'Sân Thảo Điền - Thủ Đức');
DECLARE @sportBadmintonId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'badminton');
DECLARE @sportPickleballId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'pickleball');

IF NOT EXISTS (SELECT 1 FROM dbo.courts WHERE venue_id = @venueAnNhienId AND name = N'Sân An Nhiên 1')
    INSERT INTO dbo.courts (venue_id, name, sport_id, is_active) VALUES (@venueAnNhienId, N'Sân An Nhiên 1', @sportBadmintonId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.courts WHERE venue_id = @venueAnNhienId AND name = N'Sân An Nhiên 2')
    INSERT INTO dbo.courts (venue_id, name, sport_id, is_active) VALUES (@venueAnNhienId, N'Sân An Nhiên 2', @sportBadmintonId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.courts WHERE venue_id = @venueDPickleId AND name = N'Sân D-Pickle 1')
    INSERT INTO dbo.courts (venue_id, name, sport_id, is_active) VALUES (@venueDPickleId, N'Sân D-Pickle 1', @sportPickleballId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.courts WHERE venue_id = @venueDPickleId AND name = N'Sân D-Pickle 2')
    INSERT INTO dbo.courts (venue_id, name, sport_id, is_active) VALUES (@venueDPickleId, N'Sân D-Pickle 2', @sportPickleballId, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.courts WHERE venue_id = @venueThaoDienId AND name = N'Sân Thảo Điền 1')
    INSERT INTO dbo.courts (venue_id, name, sport_id, is_active) VALUES (@venueThaoDienId, N'Sân Thảo Điền 1', @sportBadmintonId, 1);

-- 3. Seed additional sample players for realistic participant avatars & slot counts
IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'lananh.player@tksc.local')
BEGIN
    INSERT INTO dbo.users (role_id, email, full_name, password_hash, status)
    VALUES (1, N'lananh.player@tksc.local', N'Phạm Thị Lan Anh', NULL, 'Active');
    DECLARE @uLanAnh BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.players (user_id, date_of_birth, gender, avatar_url)
    VALUES (@uLanAnh, '2001-08-15', 'Female', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=100');
    DECLARE @pLanAnh BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_band, current_score, rating_status)
    VALUES (@pLanAnh, @sportBadmintonId, 'B2', 40, 'Rated'),
           (@pLanAnh, @sportPickleballId, 'B1', 25, 'Rated');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.users WHERE email = N'triet.vo@tksc.local')
BEGIN
    INSERT INTO dbo.users (role_id, email, full_name, password_hash, status)
    VALUES (1, N'triet.vo@tksc.local', N'Võ Minh Triết', NULL, 'Active');
    DECLARE @uTriet BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.players (user_id, date_of_birth, gender, avatar_url)
    VALUES (@uTriet, '1998-04-12', 'Male', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=100');
    DECLARE @pTriet BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_band, current_score, rating_status)
    VALUES (@pTriet, @sportBadmintonId, 'B3', 65, 'Rated'),
           (@pTriet, @sportPickleballId, 'B2', 50, 'Rated');
END;

-- Get court IDs
DECLARE @courtAnNhien1 BIGINT = (SELECT id FROM dbo.courts WHERE venue_id = @venueAnNhienId AND name = N'Sân An Nhiên 1');
DECLARE @courtAnNhien2 BIGINT = (SELECT id FROM dbo.courts WHERE venue_id = @venueAnNhienId AND name = N'Sân An Nhiên 2');
DECLARE @courtDPickle1 BIGINT = (SELECT id FROM dbo.courts WHERE venue_id = @venueDPickleId AND name = N'Sân D-Pickle 1');
DECLARE @courtDPickle2 BIGINT = (SELECT id FROM dbo.courts WHERE venue_id = @venueDPickleId AND name = N'Sân D-Pickle 2');
DECLARE @courtThaoDien1 BIGINT = (SELECT id FROM dbo.courts WHERE venue_id = @venueThaoDienId AND name = N'Sân Thảo Điền 1');

-- Get players
DECLARE @playerAn BIGINT = (SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id = p.user_id WHERE u.email = 'player1@tksc.local');
DECLARE @playerBinh BIGINT = (SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id = p.user_id WHERE u.email = 'player2@tksc.local');
DECLARE @playerCuong BIGINT = (SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id = p.user_id WHERE u.email = 'player3@tksc.local');
DECLARE @playerLanAnh BIGINT = (SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id = p.user_id WHERE u.email = 'lananh.player@tksc.local');
DECLARE @playerTriet BIGINT = (SELECT p.id FROM dbo.players p JOIN dbo.users u ON u.id = p.user_id WHERE u.email = 'triet.vo@tksc.local');

DECLARE @userAn BIGINT = (SELECT id FROM dbo.users WHERE email = 'player1@tksc.local');
DECLARE @userBinh BIGINT = (SELECT id FROM dbo.users WHERE email = 'player2@tksc.local');
DECLARE @userCuong BIGINT = (SELECT id FROM dbo.users WHERE email = 'player3@tksc.local');
DECLARE @userLanAnh BIGINT = (SELECT id FROM dbo.users WHERE email = 'lananh.player@tksc.local');
DECLARE @userTriet BIGINT = (SELECT id FROM dbo.users WHERE email = 'triet.vo@tksc.local');

-- 4. Seed Sessions
-- Session 1: Sân An Nhiên 1 - Hôm nay (2026-10-20), 18:00 - 20:00 (Slot 5), Band B2, Cap 8, Price 85k
-- Figma Card 1: "Cầu lông sau giờ làm", còn 4 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtAnNhien1 AND session_date = '2026-10-20' AND time_slot_id = 5)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportBadmintonId, @courtAnNhien1, 5, '2026-10-20', 'B2', 8, 85000, 'Published', 4);
    DECLARE @s1 BIGINT = SCOPE_IDENTITY();

    -- Add 4 confirmed tickets -> exactly 4 spots remaining
    -- Ticket 1: An
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN1-01', @userAn, 85000, 'Paid', NULL);
    DECLARE @o1_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o1_1, @s1, @playerAn, 'TKSC-AN1-01', 'Confirmed', 85000);

    -- Ticket 2: Binh
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN1-02', @userBinh, 85000, 'Paid', NULL);
    DECLARE @o1_2 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o1_2, @s1, @playerBinh, 'TKSC-AN1-02', 'Confirmed', 85000);

    -- Ticket 3: Cuong
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN1-03', @userCuong, 85000, 'Paid', NULL);
    DECLARE @o1_3 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o1_3, @s1, @playerCuong, 'TKSC-AN1-03', 'Confirmed', 85000);

    -- Ticket 4: Lan Anh
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN1-04', @userLanAnh, 85000, 'Paid', NULL);
    DECLARE @o1_4 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o1_4, @s1, @playerLanAnh, 'TKSC-AN1-04', 'Confirmed', 85000);
END;

-- Session 2: Sân D-Pickle 1 - Hôm nay (2026-10-20), 08:00 - 10:00 (Slot 1), Band B1, Cap 4, Price 110k
-- Figma Card 2: "Pickleball cuối tuần", còn 2 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtDPickle1 AND session_date = '2026-10-20' AND time_slot_id = 1)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportPickleballId, @courtDPickle1, 1, '2026-10-20', 'B1', 4, 110000, 'Published', 4);
    DECLARE @s2 BIGINT = SCOPE_IDENTITY();

    -- Add 2 confirmed tickets -> exactly 2 spots remaining
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-DP1-01', @userAn, 110000, 'Paid', NULL);
    DECLARE @o2_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o2_1, @s2, @playerAn, 'TKSC-DP1-01', 'Confirmed', 110000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-DP1-02', @userBinh, 110000, 'Paid', NULL);
    DECLARE @o2_2 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o2_2, @s2, @playerBinh, 'TKSC-DP1-02', 'Confirmed', 110000);
END;

-- Session 3: Sân Thảo Điền 1 - Hôm nay (2026-10-20), 16:00 - 18:00 (Slot 4), Band B2, Cap 8, Price 75k
-- Figma Card 3: "Giao lưu cầu lông", còn 6 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtThaoDien1 AND session_date = '2026-10-20' AND time_slot_id = 4)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportBadmintonId, @courtThaoDien1, 4, '2026-10-20', 'B2', 8, 75000, 'Published', 4);
    DECLARE @s3 BIGINT = SCOPE_IDENTITY();

    -- Add 2 confirmed tickets -> exactly 6 spots remaining
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-01', @userCuong, 75000, 'Paid', NULL);
    DECLARE @o3_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o3_1, @s3, @playerCuong, 'TKSC-TD1-01', 'Confirmed', 75000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-02', @userLanAnh, 75000, 'Paid', NULL);
    DECLARE @o3_2 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o3_2, @s3, @playerLanAnh, 'TKSC-TD1-02', 'Confirmed', 75000);
END;

-- Session 4: Sân An Nhiên 2 - Ngày mai (2026-10-21), 20:00 - 22:00 (Slot 6), Band B1, Cap 6, Price 80k
-- "Cầu lông phong trào tối", còn 3 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtAnNhien2 AND session_date = '2026-10-21' AND time_slot_id = 6)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportBadmintonId, @courtAnNhien2, 6, '2026-10-21', 'B1', 6, 80000, 'Published', 4);
    DECLARE @s4 BIGINT = SCOPE_IDENTITY();

    -- Add 3 confirmed tickets -> 3 spots remaining
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN2-01', @userAn, 80000, 'Paid', NULL);
    DECLARE @o4_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o4_1, @s4, @playerAn, 'TKSC-AN2-01', 'Confirmed', 80000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN2-02', @userBinh, 80000, 'Paid', NULL);
    DECLARE @o4_2 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o4_2, @s4, @playerBinh, 'TKSC-AN2-02', 'Confirmed', 80000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-AN2-03', @userCuong, 80000, 'Paid', NULL);
    DECLARE @o4_3 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o4_3, @s4, @playerCuong, 'TKSC-AN2-03', 'Confirmed', 80000);
END;

-- Session 5: Sân D-Pickle 2 - Ngày mai (2026-10-21), 16:00 - 18:00 (Slot 4), Band NULL (Tự do), Cap 4, Price 95k
-- "Pickleball giao lưu mở", còn 4 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtDPickle2 AND session_date = '2026-10-21' AND time_slot_id = 4)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportPickleballId, @courtDPickle2, 4, '2026-10-21', NULL, 4, 95000, 'Published', 4);
END;

-- Session 6: Sân Thảo Điền 1 - Hôm nay (2026-10-20), 18:00 - 20:00 (Slot 5), Band B3, Cap 4, Price 90k
-- "Buổi chơi Cầu lông ĐÃ HẾT CHỖ" (Dùng để test kịch bản SESSION_FULL trên FE)
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtThaoDien1 AND session_date = '2026-10-20' AND time_slot_id = 5)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportBadmintonId, @courtThaoDien1, 5, '2026-10-20', 'B3', 4, 90000, 'Published', 4);
    DECLARE @s6 BIGINT = SCOPE_IDENTITY();

    -- Fill all 4 slots -> 0 spots remaining (isFull = true)
    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-FULL-01', @userAn, 90000, 'Paid', NULL);
    DECLARE @o6_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o6_1, @s6, @playerAn, 'TKSC-TD1-FULL-01', 'Confirmed', 90000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-FULL-02', @userBinh, 90000, 'Paid', NULL);
    DECLARE @o6_2 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o6_2, @s6, @playerBinh, 'TKSC-TD1-FULL-02', 'Confirmed', 90000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-FULL-03', @userCuong, 90000, 'Paid', NULL);
    DECLARE @o6_3 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o6_3, @s6, @playerCuong, 'TKSC-TD1-FULL-03', 'Confirmed', 90000);

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-TD1-FULL-04', @userTriet, 90000, 'Paid', NULL);
    DECLARE @o6_4 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o6_4, @s6, @playerTriet, 'TKSC-TD1-FULL-04', 'Confirmed', 90000);
END;

-- Session 7: Sân D-Pickle 1 - Ngày kia (2026-10-22), 10:00 - 12:00 (Slot 2), Band B2, Cap 6, Price 120k
-- "Pickleball giao lưu ngày 22/10", còn 5 chỗ
IF NOT EXISTS (SELECT 1 FROM dbo.sessions WHERE court_id = @courtDPickle1 AND session_date = '2026-10-22' AND time_slot_id = 2)
BEGIN
    INSERT INTO dbo.sessions (sport_id, court_id, time_slot_id, session_date, band, capacity, price_vnd, status, created_by)
    VALUES (@sportPickleballId, @courtDPickle1, 2, '2026-10-22', 'B2', 6, 120000, 'Published', 4);
    DECLARE @s7 BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.social_booking_orders (order_code, payer_id, total_amount, status, expires_at) VALUES ('ORD-DP1-03', @userAn, 120000, 'Paid', NULL);
    DECLARE @o7_1 BIGINT = SCOPE_IDENTITY();
    INSERT INTO dbo.tickets (order_id, session_id, player_id, ticket_code, status, amount_vnd)
    VALUES (@o7_1, @s7, @playerAn, 'TKSC-DP1-03', 'Confirmed', 120000);
END;

COMMIT;
