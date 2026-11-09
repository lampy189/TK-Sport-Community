using TKSC.Application.Assessments;
using TKSC.Domain.Entities;
namespace TKSC.Application.Services;

// Shared validation for requests, referee reviews and Manager decisions.
public static class SkillReviewRules
{
 public static string Reason(string? value)
  => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 2000
   ? value.Trim() : throw new AssessmentException(400, "Nhập lý do từ 1 đến 2000 ký tự.");
 public static void Future(Session session, DateTimeOffset now)
 {
  var local = now.ToOffset(TimeSpan.FromHours(7)).DateTime;
  if (!(string.Equals(session.Status, "published", StringComparison.OrdinalIgnoreCase) || string.Equals(session.Status, "open", StringComparison.OrdinalIgnoreCase))
   || session.SessionDate.ToDateTime(session.TimeSlot.StartTime) <= local)
   throw new AssessmentException(409, "Chỉ xử lý ngoại lệ cho buổi đã công bố và chưa bắt đầu.");
 }
 public static short CorrectedScore(short? score)
  => score is null or < 0 or > 100
   ? throw new AssessmentException(400, "Điểm điều chỉnh phải từ 0 đến 100.") : score.Value;
}
