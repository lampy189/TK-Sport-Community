<<<<<<< HEAD
=======
using System.Text.Json.Serialization;

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
namespace be_da.Application.Stock;

public sealed record SupplySession(
    long Id,
    DateOnly SessionDate,
    string TimeLabel,
    string SportName,
    string CourtName,
    string Status,
    int OutstandingItems,
    int ReconciledItems
);

public sealed record SupplyRecipient(long UserId, string FullName, string Role);

// Issued and returned come from posted movements; used = issued - returned once reconciled.
public sealed record SupplyAllocation(
    long ProductId,
    string Sku,
    string Name,
    int Issued,
    int Returned,
    long? RecipientId,
    string? RecipientName,
    bool IsReconciled,
<<<<<<< HEAD
    int? ReportedUsed = null
=======
    int? ReportedUsed = null,
    string BaseUnit = StockUnitRules.DefaultBaseUnit
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
)
{
    public int Outstanding => IsReconciled ? 0 : Issued - Returned;
    public int? Used => IsReconciled ? Issued - Returned : null;
}

public sealed record SessionSupplyDetails(SupplySession Session, IReadOnlyList<SupplyRecipient> Recipients, IReadOnlyList<SupplyAllocation> Allocations);

<<<<<<< HEAD
public sealed record IssueSupplyRequest(long ProductId, int Quantity, long RecipientId);

public sealed record ReturnSupplyRequest(int ReturnedQuantity);

public sealed record ReportSupplyUseRequest(int UsedQuantity);
=======
// Quantity is in UnitName; an empty UnitName means the base unit.
public sealed record IssueSupplyRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] int Quantity,
    [property: JsonRequired] long RecipientId,
    string? UnitName = null);

public sealed record ReturnSupplyRequest([property: JsonRequired] int ReturnedQuantity);

public sealed record ReportSupplyUseRequest([property: JsonRequired] int UsedQuantity);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

public sealed record SupplyResult(bool Success, string? ErrorCode, string? Message, SessionSupplyDetails? Details = null)
{
    public static SupplyResult Ok(SessionSupplyDetails details) => new(true, null, null, details);
    public static SupplyResult Fail(string code, string message) => new(false, code, message);
}

// UC-41 / UC-42 (RP3 SCR-22 FR-22.3, 22.4): issue supplies to one session, then close with actual returns.
public static class SupplyRules
{
    public const string SessionIssue = "SessionIssue";
    public const string SessionReturn = "SessionReturn";

    public static string? ValidateIssue(IssueSupplyRequest request) =>
        request.ProductId <= 0 ? "Chọn vật tư cần cấp."
        : request.RecipientId <= 0 ? "Chọn người nhận vật tư."
<<<<<<< HEAD
        : request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? $"Số lượng cấp phải từ 1 đến {StockRules.MaxQuantity:N0}."
        : null;
=======
        : request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? $"Số lượng cấp phải từ 1 đến 100.000."
        : StockUnitRules.ValidateUnitNameInput(request.UnitName);

    // Supplies are looked up day by day; a far-away date is a typing mistake.
    public static string? ValidateDay(DateOnly day, DateOnly today) =>
        Math.Abs(day.DayNumber - today.DayNumber) > 366 ? "Ngày xem phải trong vòng một năm so với hôm nay." : null;
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

    public static string? ValidateReturn(ReturnSupplyRequest request) =>
        request.ReturnedQuantity < 0 || request.ReturnedQuantity > StockRules.MaxQuantity ? "Số lượng trả lại không hợp lệ." : null;

    public static string? ValidateUse(ReportSupplyUseRequest request) =>
        request.UsedQuantity < 0 || request.UsedQuantity > StockRules.MaxQuantity ? "Số lượng đã dùng không hợp lệ." : null;
}
