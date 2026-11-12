using be_da.Application.DTOs;
using System.Text.Json.Serialization;

namespace be_da.Application.Validators;
// UC-41 / UC-42 (RP3 SCR-22 FR-22.3, 22.4): a referee requests supplies, Staff issues them to the session,
// the referee reports use and unused returns, Staff confirms what physically came back.
public static class SupplyRules
{
    public const string SessionIssue = "SessionIssue";
    public const string SessionReturn = "SessionReturn";
    public const int MaxNoteLength = 300;
    public static readonly IReadOnlySet<string> RequestStatuses = new HashSet<string>(StringComparer.Ordinal) { "Pending", "Issued", "Rejected", "Cancelled" };
    public static string? ValidateIssue(IssueSupplyRequest request) => request.ProductId <= 0 ? "Chọn vật tư cần cấp." : request.RecipientId <= 0 ? "Chọn người nhận vật tư." : request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? $"Số lượng cấp phải từ 1 đến 100.000." : StockUnitRules.ValidateUnitNameInput(request.UnitName);
    // Supplies are looked up day by day; a far-away date is a typing mistake.
    public static string? ValidateDay(DateOnly day, DateOnly today) => Math.Abs(day.DayNumber - today.DayNumber) > 366 ? "Ngày xem phải trong vòng một năm so với hôm nay." : null;
    public static string? ValidateReturn(ReturnSupplyRequest request) => request.ReturnedQuantity < 0 || request.ReturnedQuantity > StockRules.MaxQuantity ? "Số lượng trả lại không hợp lệ." : null;

    public static string? ValidateUse(ReportSupplyUseRequest request) =>
        request.UsedQuantity < 0 || request.UsedQuantity > StockRules.MaxQuantity ? "Số lượng đã dùng không hợp lệ." :
        request.ReturnQuantity is < 0 or > StockRules.MaxQuantity ? "Số lượng trả lại không hợp lệ." : null;

    public static string? ValidateRequest(CreateSupplyRequest request) =>
        request.ProductId <= 0 ? "Chọn vật tư cần yêu cầu." :
        request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? "Số lượng yêu cầu phải từ 1 đến 100.000." :
        ValidateNote(request.Note) ?? StockUnitRules.ValidateUnitNameInput(request.UnitName);

    public static string? ValidateIssueFromRequest(IssueSupplyFromRequest request) =>
        request.Quantity is <= 0 or > StockRules.MaxQuantity ? "Số lượng cấp phải từ 1 đến 100.000." :
        request.Quantity is null && !string.IsNullOrWhiteSpace(request.UnitName) ? "Nhập số lượng khi chọn đơn vị cấp." :
        StockUnitRules.ValidateUnitNameInput(request.UnitName);

    public static string? ValidateReject(SupplyDecisionRequest request) =>
        string.IsNullOrWhiteSpace(request.Note) ? "Nhập lý do từ chối." : ValidateNote(request.Note);

    public static string? ValidateNote(string? note) => note is not null && note.Trim().Length > MaxNoteLength ? $"Ghi chú tối đa {MaxNoteLength} ký tự." : null;
    public static string? NormalizeNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    // Close-out difference: what left the store and did not come back, minus what the referee says was used.
    // Positive = unaccounted for; negative = more reported used than actually left the store; null = no report.
    public static int? Difference(int issued, int returned, int? reportedUsed) => reportedUsed is null ? null : issued - returned - reportedUsed.Value;
}
