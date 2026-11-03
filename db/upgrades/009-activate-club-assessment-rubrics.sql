-- User approved 2026-11-03: use club badminton and internal pickleball scoring.
-- Only active flags change. Existing assessments, scores and history remain intact.
SET XACT_ABORT ON;
IF DB_NAME() <> N'Sep490' THROW 51000, 'Select Sep490.', 1;
BEGIN TRANSACTION;
IF (SELECT COUNT(*) FROM dbo.rubrics r JOIN dbo.sports s ON s.id=r.sport_id
    WHERE s.code='badminton' AND r.version='BM-CLB-V1') <> 1
    THROW 51000, 'Expected one BM-CLB-V1 rubric.', 1;
IF (SELECT COUNT(*) FROM dbo.rubrics r JOIN dbo.sports s ON s.id=r.sport_id
    WHERE s.code='pickleball' AND r.version='PK-IPTPA-DUPR-V1') <> 1
    THROW 51000, 'Expected one PK-IPTPA-DUPR-V1 rubric.', 1;
UPDATE r SET is_active=CASE
    WHEN s.code='badminton' AND r.version='BM-CLB-V1' THEN 1
    WHEN s.code='pickleball' AND r.version='PK-IPTPA-DUPR-V1' THEN 1 ELSE 0 END
FROM dbo.rubrics r JOIN dbo.sports s ON s.id=r.sport_id
WHERE s.code IN ('badminton','pickleball');
COMMIT;
