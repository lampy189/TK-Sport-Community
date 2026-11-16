using be_da.Application.DTOs;
using System.Text.Json.Serialization;

namespace be_da.Application.Validators;
// UC-41 / UC-42 (RP3 SCR-22 FR-22.3, 22.4): issue supplies to one session, then close with actual returns.
public static class SupplyRules
{
    public const string SessionIssue = "SessionIssue";
    public const string SessionReturn = "SessionReturn";
    public static string? ValidateIssue(IssueSupplyRequest request) => request.ProductId <= 0 ? "Chọn vật tư cần cấp." : request.RecipientId <= 0 ? "Chọn người nhận vật tư." : request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? $"Số lượng cấp phải từ 1 đến 100.000." : StockUnitRules.ValidateUnitNameInput(request.UnitName);
    // Supplies are looked up day by day; a far-away date is a typing mistake.
    public static string? ValidateDay(DateOnly day, DateOnly today) => Math.Abs(day.DayNumber - today.DayNumber) > 366 ? "Ngày xem phải trong vòng một năm so với hôm nay." : null;
    public static string? ValidateReturn(ReturnSupplyRequest request) => request.ReturnedQuantity < 0 || request.ReturnedQuantity > StockRules.MaxQuantity ? "Số lượng trả lại không hợp lệ." : null;
    public static string? ValidateUse(ReportSupplyUseRequest request) => request.UsedQuantity < 0 || request.UsedQuantity > StockRules.MaxQuantity ? "Số lượng đã dùng không hợp lệ." : null;
}
