using be_da.Application.DTOs;

namespace be_da.Application.Validators;
// RP3 UC-43 / UC-44 (task split 13/11/2026): count -> proposed adjustment -> independent decision -> applied once.
public static class StockCountRules
{
    public const string Adjustment = "Adjustment";
    public const string NoDifference = "NoDifference";
    public const string Pending = "Pending";
    public const string Applied = "Applied";
    public const string Rejected = "Rejected";
    public const int MaxTextLength = 500;

    public static string? ValidateCreate(CreateStockCountRequest request) =>
        request.ProductId <= 0 ? "Chọn mặt hàng cần kiểm kê." :
        request.CountedQuantity < 0 || request.CountedQuantity > StockRules.MaxQuantity ? "Số lượng đếm thực tế phải từ 0 đến 100.000." :
        request.StockVersion < 0 ? "Phiên bản số tồn không hợp lệ. Tải lại phiếu kiểm kê." :
        ValidateText(request.Reason, "Lý do") ?? ValidateText(request.Evidence, "Bằng chứng") ?? StockUnitRules.ValidateUnitNameInput(request.UnitName);

    // A difference needs an explanation before it goes to the Manager; a matching count does not.
    public static string? ValidateReason(int difference, string? reason) => difference != 0 && string.IsNullOrWhiteSpace(reason) ? "Nhập lý do chênh lệch trước khi gửi đề xuất điều chỉnh." : null;

    public static string? ValidateApprove(StockCountDecisionRequest request) =>
        request.ExpectedDifference is null ? "Thiếu số chênh lệch đã xem. Tải lại đề xuất trước khi duyệt." : ValidateText(request.Note, "Ghi chú");

    public static string? ValidateReject(StockCountDecisionRequest request) =>
        string.IsNullOrWhiteSpace(request.Note) ? "Nhập lý do từ chối." : ValidateText(request.Note, "Lý do");

    public static int Difference(int counted, int system) => counted - system;
    public static string StatusFor(int difference) => difference == 0 ? NoDifference : Pending;

    // UC-44 A1: nobody approves their own count (also not through a second account of the same person, which the
    // system cannot see: separate Staff and Manager accounts still differ by id, so they are rejected here too).
    public static string? ValidateDecider(long deciderId, long countedBy) => deciderId == countedBy ? "Không được tự duyệt đề xuất do chính mình tạo." : null;

    private static string? ValidateText(string? value, string label) => value is not null && value.Trim().Length > MaxTextLength ? $"{label} tối đa {MaxTextLength} ký tự." : null;
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
