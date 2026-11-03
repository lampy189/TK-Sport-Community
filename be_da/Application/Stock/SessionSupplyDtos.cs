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
    bool IsReconciled
)
{
    public int Outstanding => IsReconciled ? 0 : Issued - Returned;
    public int? Used => IsReconciled ? Issued - Returned : null;
}

public sealed record SessionSupplyDetails(SupplySession Session, IReadOnlyList<SupplyRecipient> Recipients, IReadOnlyList<SupplyAllocation> Allocations);

public sealed record IssueSupplyRequest(long ProductId, int Quantity, long RecipientId);

public sealed record ReturnSupplyRequest(int ReturnedQuantity);

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
        : request.Quantity <= 0 || request.Quantity > StockRules.MaxQuantity ? $"Số lượng cấp phải từ 1 đến {StockRules.MaxQuantity:N0}."
        : null;

    public static string? ValidateReturn(ReturnSupplyRequest request) =>
        request.ReturnedQuantity < 0 || request.ReturnedQuantity > StockRules.MaxQuantity ? "Số lượng trả lại không hợp lệ." : null;
}
