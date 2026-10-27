-- Upgrade 004: Strictly restore original database courts and venues without adding external venues
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

-- 1. Restore exact original venue
UPDATE dbo.venues 
SET name = N'TK Sport Community Center',
    address = NULL,
    is_active = 1
WHERE id = 1;

-- 2. Restore exact original two courts
-- Court 1: Court 1 - Badminton
UPDATE dbo.courts 
SET venue_id = 1, name = N'Court 1 - Badminton', sport_id = 1, is_active = 1 
WHERE id = 1;

-- Court 2: Court 2 - Pickleball
UPDATE dbo.courts 
SET venue_id = 1, name = N'Court 2 - Pickleball', sport_id = 2, is_active = 1 
WHERE id = 2;

-- 3. Remap all sessions to ONLY Court 1 and Court 2 without time collision
-- Badminton sessions -> Court 1
UPDATE dbo.sessions SET court_id = 1, time_slot_id = 4 WHERE id = 10004; -- 2026-10-20, 16:00 - 18:00
UPDATE dbo.sessions SET court_id = 1, time_slot_id = 5 WHERE id = 10002; -- 2026-10-20, 18:00 - 20:00
UPDATE dbo.sessions SET court_id = 1, time_slot_id = 6 WHERE id = 10007; -- 2026-10-20, 20:00 - 22:00 (Full)
UPDATE dbo.sessions SET court_id = 1, time_slot_id = 6 WHERE id = 10005; -- 2026-10-21, 20:00 - 22:00

-- Pickleball sessions -> Court 2
UPDATE dbo.sessions SET court_id = 2, time_slot_id = 1 WHERE id = 10003; -- 2026-10-20, 08:00 - 10:00
UPDATE dbo.sessions SET court_id = 2, time_slot_id = 4 WHERE id = 10006; -- 2026-10-21, 16:00 - 18:00
UPDATE dbo.sessions SET court_id = 2, time_slot_id = 2 WHERE id = 10008; -- 2026-10-22, 10:00 - 12:00

-- 4. Delete all other courts (strictly keep only original Court 1 and Court 2)
DELETE FROM dbo.courts WHERE id NOT IN (1, 2);

-- 5. Delete all other venues (strictly keep only original Venue 1)
DELETE FROM dbo.venues WHERE id <> 1;

COMMIT;
