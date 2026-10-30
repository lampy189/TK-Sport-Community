-- Upgrade 003: Fix Vietnamese Unicode character encoding in Sep490
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

-- Fix Venues
UPDATE dbo.venues
SET name = N'Sân An Nhiên - Bình Thạnh',
    address = N'45/2 Nguyễn Gia Trí, Phường 25, Bình Thạnh, TP.HCM'
WHERE id = 2 OR name LIKE N'%An Nhi%n%';

UPDATE dbo.venues
SET name = N'Sân D-Pickle - Quận 2',
    address = N'88 Song Hành, An Phú, Quận 2, TP.HCM'
WHERE id = 3 OR name LIKE N'%D-Pickle%';

UPDATE dbo.venues
SET name = N'Sân Thảo Điền - Thủ Đức',
    address = N'12 Quốc Hương, Thảo Điền, TP. Thủ Đức, TP.HCM'
WHERE id = 4 OR name LIKE N'%Th%o Đi%n%';

-- Fix Courts
UPDATE dbo.courts SET name = N'Sân An Nhiên 1' WHERE id = 3;
UPDATE dbo.courts SET name = N'Sân An Nhiên 2' WHERE id = 4;
UPDATE dbo.courts SET name = N'Sân D-Pickle 1' WHERE id = 5;
UPDATE dbo.courts SET name = N'Sân D-Pickle 2' WHERE id = 6;
UPDATE dbo.courts SET name = N'Sân Thảo Điền 1' WHERE id = 7;

-- Fix Users
UPDATE dbo.users SET full_name = N'Phạm Thị Lan Anh' WHERE email = 'lananh.player@tksc.local';
UPDATE dbo.users SET full_name = N'Võ Minh Triết' WHERE email = 'triet.vo@tksc.local';
UPDATE dbo.users SET full_name = N'Nguyễn Văn An (Player 1)' WHERE email = 'player1@tksc.local';
UPDATE dbo.users SET full_name = N'Trần Thị Bình (Player 2)' WHERE email = 'player2@tksc.local';
UPDATE dbo.users SET full_name = N'Lê Hoàng Cường (Player 3)' WHERE email = 'player3@tksc.local';

COMMIT;
