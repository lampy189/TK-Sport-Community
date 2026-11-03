-- REVIEW BEFORE APPLYING. No automatic startup execution.
-- Source: user-supplied Report 1 Appendix A, selected on 30 Oct 2026.
-- Adds only the badminton pilot rubric; never invents a pickleball scale.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @sport BIGINT = (SELECT id FROM dbo.sports WHERE code = 'badminton');
IF @sport IS NULL THROW 51000, 'Badminton sport is missing.', 1;
IF EXISTS (SELECT 1 FROM dbo.rubrics WHERE sport_id=@sport AND is_active=1 AND version <> 'badminton-pilot-r1')
    THROW 51000, 'Another rubric is active. Review before changing it.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.rubrics WHERE sport_id=@sport AND version='badminton-pilot-r1')
BEGIN
    INSERT dbo.rubrics(sport_id, version, name, is_active)
    VALUES(@sport, 'badminton-pilot-r1', N'Cầu lông — Report 1, Phụ lục A', 1);
    DECLARE @rubric BIGINT = SCOPE_IDENTITY();
    INSERT dbo.rubric_dimensions(rubric_id, code, name, weight_percent) VALUES
        (@rubric, 'D1', N'Thực hiện và đa dạng kỹ thuật đánh cầu', 20),
        (@rubric, 'D2', N'Di chuyển và hồi vị', 20),
        (@rubric, 'D3', N'Tính ổn định và kiểm soát', 20),
        (@rubric, 'D4', N'Nhận thức chiến thuật', 20),
        (@rubric, 'D5', N'Phối hợp đánh đôi', 20);
END;
COMMIT;
