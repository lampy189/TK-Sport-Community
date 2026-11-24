-- Backfill open referee work after deploying R04. Re-running preserves read state.
SET XACT_ABORT ON;
SET NOCOUNT ON;
IF DB_NAME() NOT LIKE N'Sep490%' THROW 50000, 'Select a TKSC Sep490 database first.', 1;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRANSACTION;

DECLARE @today date = CONVERT(date, SWITCHOFFSET(SYSDATETIMEOFFSET(), '+07:00'));

WITH assigned AS (
    SELECT s.id AS session_id, s.session_date, s.status, sr.referee_id
    FROM dbo.session_referees sr
    JOIN dbo.sessions s ON s.id=sr.session_id
    JOIN dbo.courts c ON c.id=s.court_id
    JOIN dbo.users u ON u.id=sr.referee_id AND u.venue_id=c.venue_id
    JOIN dbo.roles r ON r.id=u.role_id
    WHERE r.code='Referee' AND LOWER(u.status)='active' AND u.deleted_at IS NULL
),
work_items AS (
    SELECT referee_id, 'referee_shift_assigned' AS reference_type, session_id AS reference_id,
           N'Bạn được phân công ca trọng tài' AS title
    FROM assigned
    WHERE session_date>=@today AND LOWER(status) NOT IN ('cancelled','canceled','completed','ended')
    UNION
    SELECT s.referee_id, 'referee_skill_exception', e.id, N'Có yêu cầu ngoại lệ trình độ cho buổi bạn phụ trách'
    FROM assigned s JOIN dbo.session_exceptions e ON e.session_id=s.session_id
    WHERE s.session_date>=@today AND LOWER(s.status) IN ('open','published')
      AND e.approved_by IS NULL AND NOT EXISTS (
          SELECT 1 FROM dbo.audit_logs l
          WHERE l.entity_type='SessionException' AND l.entity_id=e.id AND l.action='exception_decided'
      )
    UNION
    SELECT s.referee_id, 'referee_rating_appeal', ra.id, N'Có khiếu nại về phiếu đánh giá bạn đã chấm'
    FROM dbo.rating_appeals ra JOIN dbo.assessments a ON a.id=ra.assessment_id
    JOIN assigned s ON s.session_id=a.session_id AND s.referee_id=a.assessor_id
    WHERE LOWER(ra.status)='pending'
)
INSERT dbo.notifications(user_id,channel,title,reference_type,reference_id,status)
SELECT w.referee_id,'web',w.title,w.reference_type,w.reference_id,'unread'
FROM work_items w
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.notifications n WITH (UPDLOCK,HOLDLOCK)
    WHERE n.user_id=w.referee_id AND n.channel='web'
      AND n.reference_type=w.reference_type AND n.reference_id=w.reference_id
);
SELECT @@ROWCOUNT AS created_notifications;
COMMIT;
