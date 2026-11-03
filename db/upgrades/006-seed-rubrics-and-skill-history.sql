-- Upgrade 006: Seed Badminton (VN Club Yếu/TB-/TB/TB+/Khá) & Pickleball (IPTPA/DUPR 2.0-4.0+) Rubrics, Dimensions, Assessments & Rating History
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'Sep490' THROW 50000, 'Select the Sep490 database before applying this upgrade.', 1;

BEGIN TRANSACTION;

DECLARE @sportBadmintonId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'badminton');
DECLARE @sportPickleballId BIGINT = (SELECT id FROM dbo.sports WHERE code = 'pickleball');
DECLARE @refereeUserId BIGINT = ISNULL(
    (SELECT TOP 1 u.id FROM dbo.users u JOIN dbo.roles r ON r.id = u.role_id WHERE r.code = 'Referee' ORDER BY u.id),
    (SELECT TOP 1 id FROM dbo.users ORDER BY id)
);

-- 1. Seed Active Rubrics for Badminton and Pickleball
IF NOT EXISTS (SELECT 1 FROM dbo.rubrics WHERE sport_id = @sportBadmintonId AND version = 'BM-CLB-V1')
BEGIN
    UPDATE dbo.rubrics SET is_active = 0 WHERE sport_id = @sportBadmintonId;
    INSERT INTO dbo.rubrics (sport_id, version, name, is_active)
    VALUES (@sportBadmintonId, 'BM-CLB-V1', N'Khung đánh giá Cầu lông Phong trào VN (Yếu / TB- / TB / TB+ / Khá)', 1);
END
ELSE
BEGIN
    UPDATE dbo.rubrics
    SET name = N'Khung đánh giá Cầu lông Phong trào VN (Yếu / TB- / TB / TB+ / Khá)', is_active = 1
    WHERE sport_id = @sportBadmintonId AND version = 'BM-CLB-V1';
END;

IF NOT EXISTS (SELECT 1 FROM dbo.rubrics WHERE sport_id = @sportPickleballId AND version = 'PK-IPTPA-DUPR-V1')
BEGIN
    UPDATE dbo.rubrics SET is_active = 0 WHERE sport_id = @sportPickleballId;
    INSERT INTO dbo.rubrics (sport_id, version, name, is_active)
    VALUES (@sportPickleballId, 'PK-IPTPA-DUPR-V1', N'Khung đánh giá Pickleball chuẩn IPTPA & DUPR (2.0 – 4.0+)', 1);
END
ELSE
BEGIN
    UPDATE dbo.rubrics
    SET name = N'Khung đánh giá Pickleball chuẩn IPTPA & DUPR (2.0 – 4.0+)', is_active = 1
    WHERE sport_id = @sportPickleballId AND version = 'PK-IPTPA-DUPR-V1';
END;

DECLARE @rubricBadmintonId BIGINT = (SELECT id FROM dbo.rubrics WHERE sport_id = @sportBadmintonId AND version = 'BM-CLB-V1');
DECLARE @rubricPickleballId BIGINT = (SELECT id FROM dbo.rubrics WHERE sport_id = @sportPickleballId AND version = 'PK-IPTPA-DUPR-V1');

-- 2. Seed 5 Rubric Dimensions (20% each) for Badminton
DECLARE @bmDims TABLE (code VARCHAR(100), name NVARCHAR(255), weight_percent SMALLINT);
INSERT INTO @bmDims (code, name, weight_percent) VALUES
    ('BM_SERVE_NET',      N'Giao cầu, Trả giao & Xử lý trên lưới', 20),
    ('BM_CLEAR_BACKHAND', N'Phông cầu cuối sân (Clear) & Trái tay (Backhand)', 20),
    ('BM_SMASH_DEFENSE',  N'Đập cầu (Smash), Chặt cầu (Drop) & Phòng thủ', 20),
    ('BM_FOOTWORK',       N'Bộ pháp di chuyển 6 góc sân & Hồi vị', 20),
    ('BM_DOUBLES',        N'Đội hình Đánh đôi (Công/Thủ) & Độ bền Rally', 20);

UPDATE rd
SET rd.name = d.name, rd.weight_percent = d.weight_percent
FROM dbo.rubric_dimensions rd
JOIN @bmDims d ON d.code = rd.code
WHERE rd.rubric_id = @rubricBadmintonId;

INSERT INTO dbo.rubric_dimensions (rubric_id, code, name, weight_percent)
SELECT @rubricBadmintonId, d.code, d.name, d.weight_percent
FROM @bmDims d
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.rubric_dimensions rd
    WHERE rd.rubric_id = @rubricBadmintonId AND rd.code = d.code
);

-- 3. Seed 5 Rubric Dimensions (20% each) for Pickleball
DECLARE @pkDims TABLE (code VARCHAR(100), name NVARCHAR(255), weight_percent SMALLINT);
INSERT INTO @pkDims (code, name, weight_percent) VALUES
    ('PK_SERVE_RETURN',     N'Giao bóng (Serve) & Trả giao bóng sâu (Return)', 20),
    ('PK_THIRD_SHOT',       N'Cú đánh thứ 3 (3rd Shot Drop/Drive) & Lên lưới', 20),
    ('PK_DINK_NVZ',         N'Kỹ thuật Dink & Kiểm soát vạch Kitchen (NVZ)', 20),
    ('PK_VOLLEY_RESET',     N'Bắt Volley, Đấu tay đôi (Hand Battle) & Reset bóng', 20),
    ('PK_DOUBLES_STRATEGY', N'Groundstroke 2 tay & Chiến thuật Đánh đôi (Stacking)', 20);

UPDATE rd
SET rd.name = d.name, rd.weight_percent = d.weight_percent
FROM dbo.rubric_dimensions rd
JOIN @pkDims d ON d.code = rd.code
WHERE rd.rubric_id = @rubricPickleballId;

INSERT INTO dbo.rubric_dimensions (rubric_id, code, name, weight_percent)
SELECT @rubricPickleballId, d.code, d.name, d.weight_percent
FROM @pkDims d
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.rubric_dimensions rd
    WHERE rd.rubric_id = @rubricPickleballId AND rd.code = d.code
);

-- 4. Clean up any invalid placeholder band values in player_sport_profiles
UPDATE dbo.users SET full_name = N'Đỗ Quốc Khánh' WHERE email = N'khanh.do@tksc.local';
UPDATE dbo.users SET full_name = N'Nguyễn Trà My' WHERE email = N'my.nguyen@tksc.local';
UPDATE dbo.users SET full_name = N'Hoàng Gia Huy' WHERE email = N'huy.hoang@tksc.local';

UPDATE dbo.player_sport_profiles
SET current_band = 'B2', current_score = 35, rating_status = 'Provisional'
WHERE current_band NOT IN ('B1', 'B2', 'B3', 'B4', 'B5') AND current_band IS NOT NULL;

-- Ensure all players have sport profiles for both sports
INSERT INTO dbo.player_sport_profiles (player_id, sport_id, current_score, current_band, rating_status)
SELECT p.id, s.id, NULL, NULL, 'Unrated'
FROM dbo.players p
CROSS JOIN dbo.sports s
WHERE s.id IN (@sportBadmintonId, @sportPickleballId)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.player_sport_profiles psp
      WHERE psp.player_id = p.id AND psp.sport_id = s.id
  );

-- 5. Seed realistic Assessment & Rating History for profiles that already have a band/score
DECLARE @bmSessionId BIGINT = (SELECT TOP 1 id FROM dbo.sessions WHERE sport_id = @sportBadmintonId ORDER BY session_date DESC, id DESC);
DECLARE @pkSessionId BIGINT = (SELECT TOP 1 id FROM dbo.sessions WHERE sport_id = @sportPickleballId ORDER BY session_date DESC, id DESC);

DECLARE @bmD1 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricBadmintonId AND code = 'BM_SERVE_NET');
DECLARE @bmD2 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricBadmintonId AND code = 'BM_CLEAR_BACKHAND');
DECLARE @bmD3 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricBadmintonId AND code = 'BM_SMASH_DEFENSE');
DECLARE @bmD4 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricBadmintonId AND code = 'BM_FOOTWORK');
DECLARE @bmD5 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricBadmintonId AND code = 'BM_DOUBLES');

DECLARE @pkD1 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricPickleballId AND code = 'PK_SERVE_RETURN');
DECLARE @pkD2 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricPickleballId AND code = 'PK_THIRD_SHOT');
DECLARE @pkD3 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricPickleballId AND code = 'PK_DINK_NVZ');
DECLARE @pkD4 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricPickleballId AND code = 'PK_VOLLEY_RESET');
DECLARE @pkD5 BIGINT = (SELECT id FROM dbo.rubric_dimensions WHERE rubric_id = @rubricPickleballId AND code = 'PK_DOUBLES_STRATEGY');

DECLARE @profCursor CURSOR;
DECLARE @pspId BIGINT, @pUserId BIGINT, @spId BIGINT, @cScore SMALLINT, @cBand VARCHAR(100);

SET @profCursor = CURSOR FAST_FORWARD FOR
    SELECT psp.id, p.user_id, psp.sport_id, psp.current_score, psp.current_band
    FROM dbo.player_sport_profiles psp
    JOIN dbo.players p ON p.id = psp.player_id
    WHERE psp.current_band IS NOT NULL AND psp.current_score IS NOT NULL;

OPEN @profCursor;
FETCH NEXT FROM @profCursor INTO @pspId, @pUserId, @spId, @cScore, @cBand;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.assessments WHERE player_sport_profile_id = @pspId)
    BEGIN
        DECLARE @rubId BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @rubricBadmintonId ELSE @rubricPickleballId END;
        DECLARE @sessId BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmSessionId ELSE @pkSessionId END;
        DECLARE @d1 BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmD1 ELSE @pkD1 END;
        DECLARE @d2 BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmD2 ELSE @pkD2 END;
        DECLARE @d3 BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmD3 ELSE @pkD3 END;
        DECLARE @d4 BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmD4 ELSE @pkD4 END;
        DECLARE @d5 BIGINT = CASE WHEN @spId = @sportBadmintonId THEN @bmD5 ELSE @pkD5 END;

        -- Step 1: Initial SelfSurvey assessment (slightly lower initial score to show progression)
        DECLARE @initScore SMALLINT = CASE WHEN @cScore >= 15 THEN @cScore - 10 ELSE @cScore END;
        DECLARE @initBand VARCHAR(100) = CASE
            WHEN @initScore < 20 THEN 'B1'
            WHEN @initScore < 40 THEN 'B2'
            WHEN @initScore < 60 THEN 'B3'
            WHEN @initScore < 80 THEN 'B4'
            ELSE 'B5'
        END;

        INSERT INTO dbo.assessments (player_sport_profile_id, session_id, rubric_id, assessment_type, assessor_id, status, total_score, band_result)
        VALUES (@pspId, NULL, @rubId, 'SelfSurvey', @pUserId, 'Published', @initScore, @initBand);
        DECLARE @initAssessmentId BIGINT = SCOPE_IDENTITY();

        INSERT INTO dbo.assessment_scores (assessment_id, dimension_id, score)
        VALUES
            (@initAssessmentId, @d1, 1),
            (@initAssessmentId, @d2, 1),
            (@initAssessmentId, @d3, 1),
            (@initAssessmentId, @d4, 1),
            (@initAssessmentId, @d5, 1);

        INSERT INTO dbo.player_rating_history (player_sport_profile_id, old_score, new_score, old_band, new_band, changed_by, assessment_id)
        VALUES (@pspId, NULL, @initScore, NULL, @initBand, @pUserId, @initAssessmentId);

        -- Step 2: Referee OnCourt assessment bringing the player to their current_score & current_band
        DECLARE @targetTotalUnits INT = ROUND(@cScore / 5.0, 0);
        DECLARE @baseMark SMALLINT = @targetTotalUnits / 5;
        DECLARE @rem INT = @targetTotalUnits % 5;
        DECLARE @m1 SMALLINT = CASE WHEN @baseMark + (CASE WHEN @rem >= 1 THEN 1 ELSE 0 END) > 4 THEN 4 ELSE @baseMark + (CASE WHEN @rem >= 1 THEN 1 ELSE 0 END) END;
        DECLARE @m2 SMALLINT = CASE WHEN @baseMark + (CASE WHEN @rem >= 2 THEN 1 ELSE 0 END) > 4 THEN 4 ELSE @baseMark + (CASE WHEN @rem >= 2 THEN 1 ELSE 0 END) END;
        DECLARE @m3 SMALLINT = CASE WHEN @baseMark + (CASE WHEN @rem >= 3 THEN 1 ELSE 0 END) > 4 THEN 4 ELSE @baseMark + (CASE WHEN @rem >= 3 THEN 1 ELSE 0 END) END;
        DECLARE @m4 SMALLINT = CASE WHEN @baseMark + (CASE WHEN @rem >= 4 THEN 1 ELSE 0 END) > 4 THEN 4 ELSE @baseMark + (CASE WHEN @rem >= 4 THEN 1 ELSE 0 END) END;
        DECLARE @m5 SMALLINT = CASE WHEN @baseMark > 4 THEN 4 ELSE @baseMark END;

        INSERT INTO dbo.assessments (player_sport_profile_id, session_id, rubric_id, assessment_type, assessor_id, status, total_score, band_result)
        VALUES (@pspId, @sessId, @rubId, 'OnCourt', @refereeUserId, 'Published', @cScore, @cBand);
        DECLARE @refAssessmentId BIGINT = SCOPE_IDENTITY();

        INSERT INTO dbo.assessment_scores (assessment_id, dimension_id, score)
        VALUES
            (@refAssessmentId, @d1, @m1),
            (@refAssessmentId, @d2, @m2),
            (@refAssessmentId, @d3, @m3),
            (@refAssessmentId, @d4, @m4),
            (@refAssessmentId, @d5, @m5);

        INSERT INTO dbo.player_rating_history (player_sport_profile_id, old_score, new_score, old_band, new_band, changed_by, assessment_id)
        VALUES (@pspId, @initScore, @cScore, @initBand, @cBand, @refereeUserId, @refAssessmentId);
    END;

    FETCH NEXT FROM @profCursor INTO @pspId, @pUserId, @spId, @cScore, @cBand;
END;

CLOSE @profCursor;
DEALLOCATE @profCursor;

COMMIT;
