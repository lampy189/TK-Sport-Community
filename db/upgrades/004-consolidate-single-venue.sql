-- Upgrade 004: Consolidate all courts and sessions into a single venue (TK Sport Community Center)
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

-- 1. Ensure Venue 1 is the single TK Sport Community Center
UPDATE dbo.venues 
SET name = N'TK Sport Community Center',
    address = N'Cụm sân TK Sport Community',
    is_active = 1
WHERE id = 1;

-- 2. Consolidate Courts 1-4 under Venue 1
-- Court 1: Sân Cầu lông 1 (Badminton)
UPDATE dbo.courts 
SET venue_id = 1, name = N'Sân Cầu lông 1', sport_id = 1, is_active = 1 
WHERE id = 1;

-- Court 2: Sân Pickleball 1 (Pickleball)
UPDATE dbo.courts 
SET venue_id = 1, name = N'Sân Pickleball 1', sport_id = 2, is_active = 1 
WHERE id = 2;

-- Court 3: Sân Cầu lông 2 (Badminton)
UPDATE dbo.courts 
SET venue_id = 1, name = N'Sân Cầu lông 2', sport_id = 1, is_active = 1 
WHERE id = 3;

-- Court 4: Sân Pickleball 2 (Pickleball)
UPDATE dbo.courts 
SET venue_id = 1, name = N'Sân Pickleball 2', sport_id = 2, is_active = 1 
WHERE id = 4;

-- 3. Remap all sessions to Courts 1-4 without any court/slot collision
-- Session 10002: Sân Cầu lông 1 (Court 1), 2026-10-20, Slot 5 (18:00 - 20:00)
UPDATE dbo.sessions SET court_id = 1 WHERE id = 10002;

-- Session 10003: Sân Pickleball 1 (Court 2), 2026-10-20, Slot 1 (08:00 - 10:00)
UPDATE dbo.sessions SET court_id = 2 WHERE id = 10003;

-- Session 10004: Sân Cầu lông 1 (Court 1), 2026-10-20, Slot 4 (16:00 - 18:00)
UPDATE dbo.sessions SET court_id = 1 WHERE id = 10004;

-- Session 10007: Sân Cầu lông 2 (Court 3), 2026-10-20, Slot 5 (18:00 - 20:00)
UPDATE dbo.sessions SET court_id = 3 WHERE id = 10007;

-- Session 10005: Sân Cầu lông 1 (Court 1), 2026-10-21, Slot 6 (20:00 - 22:00)
UPDATE dbo.sessions SET court_id = 1 WHERE id = 10005;

-- Session 10006: Sân Pickleball 2 (Court 4), 2026-10-21, Slot 4 (16:00 - 18:00)
UPDATE dbo.sessions SET court_id = 4 WHERE id = 10006;

-- Session 10008: Sân Pickleball 1 (Court 2), 2026-10-22, Slot 2 (10:00 - 12:00)
UPDATE dbo.sessions SET court_id = 2 WHERE id = 10008;

-- 4. Delete orphaned courts (IDs 5, 6, 7)
DELETE FROM dbo.courts WHERE id IN (5, 6, 7);

-- 5. Delete extraneous venues (IDs > 1)
DELETE FROM dbo.venues WHERE id > 1;

COMMIT;
