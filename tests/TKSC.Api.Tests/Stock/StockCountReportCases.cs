using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.StockCount;

// Report 5.1 – StockCount (Lê Tuấn Dũng): physical count, adjustment proposal, independent approval, supply report
// and stock reconciliation (RP3 UC-43, UC-44; task split 13/11/2026).
public sealed class StockCountReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string QuantityRange = "Số lượng đếm thực tế phải từ 0 đến 100.000.";

    private static CreateStockCountRequest Count(long productId = 50, int counted = 89, long version = 10, string? reason = "Thiếu 1 quả") =>
        new(productId, counted, version, null, reason, null);

    // ---------------- StockCountRules.ValidateCreate
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff counts an item that exists in the venue stock")]
    public void UTC001_ValidCount() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=89, StockVersion=10, Reason=\"Thiếu 1 quả\")",
            (string?)null, () => StockCountRules.ValidateCreate(Count()));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Every unit of the item is missing on the shelf")]
    public void UTC002_CountedZero() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=0, StockVersion=10)",
            (string?)null, () => StockCountRules.ValidateCreate(Count(counted: 0)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Counted quantity at the upper limit")]
    public void UTC003_CountedMaximum() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=100000, StockVersion=10)",
            (string?)null, () => StockCountRules.ValidateCreate(Count(counted: 100_000)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Negative counted quantity")]
    public void UTC004_CountedNegative() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=-1, StockVersion=10)",
            QuantityRange, () => StockCountRules.ValidateCreate(Count(counted: -1)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Counted quantity one above the upper limit")]
    public void UTC005_CountedAboveMaximum() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=100001, StockVersion=10)",
            QuantityRange, () => StockCountRules.ValidateCreate(Count(counted: 100_001)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "No item selected on the count sheet")]
    public void UTC006_MissingProduct() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=0, CountedQuantity=5, StockVersion=10)",
            "Chọn mặt hàng cần kiểm kê.", () => StockCountRules.ValidateCreate(Count(productId: 0, counted: 5)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Count sheet sent with an invalid stock version")]
    public void UTC007_InvalidStockVersion() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=89, StockVersion=-1)",
            "Phiên bản số tồn không hợp lệ. Tải lại phiếu kiểm kê.", () => StockCountRules.ValidateCreate(Count(version: -1)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Reason longer than 500 characters")]
    public void UTC008_ReasonTooLong() =>
        Check("StockCountRules.ValidateCreate", "request=CreateStockCountRequest(ProductId=50, CountedQuantity=89, StockVersion=10, Reason=string('a', 501))",
            "Lý do tối đa 500 ký tự.", () => StockCountRules.ValidateCreate(Count(reason: new string('a', 501))));

    // ---------------- StockCountRules.ValidateReason
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Count differs from the system quantity")]
    public void UTC009_DifferenceWithoutReason() =>
        Check("StockCountRules.ValidateReason", "difference=-2; reason=\" \"",
            "Nhập lý do chênh lệch trước khi gửi đề xuất điều chỉnh.", () => StockCountRules.ValidateReason(-2, " "));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Count matches the system quantity")]
    public void UTC010_NoDifferenceNoReason() =>
        Check("StockCountRules.ValidateReason", "difference=0; reason=null", (string?)null, () => StockCountRules.ValidateReason(0, null));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Count differs and Staff explains why")]
    public void UTC011_DifferenceWithReason() =>
        Check("StockCountRules.ValidateReason", "difference=3; reason=\"Nhập bổ sung chưa ghi phiếu\"",
            (string?)null, () => StockCountRules.ValidateReason(3, "Nhập bổ sung chưa ghi phiếu"));

    // ---------------- StockCountRules.Difference / StatusFor
    [Fact, Trait("Type", "Normal"), Trait("Pre", "System quantity 90, counted 89")]
    public void UTC012_DifferenceShortage() =>
        Check("StockCountRules.Difference", "counted=89; system=90", -1, () => StockCountRules.Difference(89, 90));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Counted quantity equals the system quantity")]
    public void UTC013_StatusNoDifference() =>
        Check("StockCountRules.StatusFor", "difference=0", "NoDifference", () => StockCountRules.StatusFor(0));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Counted quantity differs from the system quantity")]
    public void UTC014_StatusPending() =>
        Check("StockCountRules.StatusFor", "difference=-1", "Pending", () => StockCountRules.StatusFor(-1));

    // ---------------- StockCountRules.ValidateDecider
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "The person who counted tries to approve the count")]
    public void UTC015_SelfApproval() =>
        Check("StockCountRules.ValidateDecider", "deciderId=6; countedBy=6",
            "Không được tự duyệt đề xuất do chính mình tạo.", () => StockCountRules.ValidateDecider(6, 6));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "A Manager approves a count made by Staff")]
    public void UTC016_IndependentApprover() =>
        Check("StockCountRules.ValidateDecider", "deciderId=3; countedBy=6", (string?)null, () => StockCountRules.ValidateDecider(3, 6));

    // ---------------- StockCountRules.ValidateApprove / ValidateReject
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Approval sent without the difference the Manager reviewed")]
    public void UTC017_ApproveWithoutExpectedDifference() =>
        Check("StockCountRules.ValidateApprove", "request=StockCountDecisionRequest(Note=\"OK\", ExpectedDifference=null)",
            "Thiếu số chênh lệch đã xem. Tải lại đề xuất trước khi duyệt.", () => StockCountRules.ValidateApprove(new StockCountDecisionRequest("OK", null)));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Manager approves after reviewing a difference of -1")]
    public void UTC018_ApproveValid() =>
        Check("StockCountRules.ValidateApprove", "request=StockCountDecisionRequest(Note=\"Đã kiểm tra\", ExpectedDifference=-1)",
            (string?)null, () => StockCountRules.ValidateApprove(new StockCountDecisionRequest("Đã kiểm tra", -1)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Manager rejects without a reason")]
    public void UTC019_RejectWithoutReason() =>
        Check("StockCountRules.ValidateReject", "request=StockCountDecisionRequest(Note=\"\")",
            "Nhập lý do từ chối.", () => StockCountRules.ValidateReject(new StockCountDecisionRequest("")));

    // ---------------- StockCountService.GetSupplyReportAsync
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Supply data exists for November 2026")]
    public Task UTC020_ReportOneMonth() =>
        CheckAsync("StockCountService.GetSupplyReportAsync", "from=DateOnly(2026, 11, 1); to=DateOnly(2026, 11, 30)",
            "Success: 2026-11-01..2026-11-30", () => Report(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Start date after end date")]
    public Task UTC021_ReportInvertedRange() =>
        CheckAsync("StockCountService.GetSupplyReportAsync", "from=DateOnly(2026, 11, 20); to=DateOnly(2026, 11, 10)",
            "Invalid: Ngày bắt đầu phải trước ngày kết thúc.", () => Report(new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 10)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Range of exactly 366 days")]
    public Task UTC022_ReportMaximumRange() =>
        CheckAsync("StockCountService.GetSupplyReportAsync", "from=DateOnly(2025, 11, 26); to=DateOnly(2026, 11, 27)",
            "Success: 2025-11-26..2026-11-27", () => Report(new DateOnly(2025, 11, 26), new DateOnly(2026, 11, 27)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Range of 367 days")]
    public Task UTC023_ReportRangeTooLong() =>
        CheckAsync("StockCountService.GetSupplyReportAsync", "from=DateOnly(2025, 11, 25); to=DateOnly(2026, 11, 27)",
            "Invalid: Khoảng báo cáo tối đa một năm.", () => Report(new DateOnly(2025, 11, 25), new DateOnly(2026, 11, 27)));

    // ---------------- StockCountService.GetReconciliationAsync
    [Fact, Trait("Type", "Normal"), Trait("Pre", "3 items: 1 matches, 1 on-hand mismatch, 1 held-quantity mismatch")]
    public Task UTC024_ReconciliationOnlyMismatches() =>
        CheckAsync("StockCountService.GetReconciliationAsync", "onlyMismatches=true; lines=[BALL ok, TOWEL onHand 2 vs movements 3, WATER held 2 vs orders 1]",
            "TOWEL, WATER", async () => string.Join(", ", (await new StockCountService(new FakeStore(), Clock).GetReconciliationAsync(true, default)).Select(l => l.Sku)));

    private static readonly TimeProvider Clock = new FixedClock(new DateTimeOffset(2026, 11, 27, 3, 0, 0, TimeSpan.Zero));

    private static async Task<string> Report(DateOnly from, DateOnly to)
    {
        var result = await new StockCountService(new FakeStore(), Clock).GetSupplyReportAsync(from, to, default);
        return result.Value is StockSupplyReport report
            ? $"{result.Outcome}: {report.From:yyyy-MM-dd}..{report.To:yyyy-MM-dd}"
            : $"{result.Outcome}: {result.Value!.GetType().GetProperty("message")!.GetValue(result.Value)}";
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : IStockCountStore
    {
        public Task<StockSupplyReport> GetSupplyReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
            Task.FromResult(new StockSupplyReport(from, to, 1, []));

        public Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockReconciliationLine>>([
                new(51, "BALL", "Bóng Pickleball", 100, 100, 0, 0),
                new(52, "TOWEL", "Khăn thể thao", 2, 3, 0, 0),
                new(49, "WATER", "Nước suối 500ml", 100, 100, 2, 1)]);

        public Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<StockCountView>> ListAsync(string? status, long? productId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<StockCountView?> GetAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<StockCountResult> DecideAsync(long id, long deciderId, bool approve, string? note, int? expectedDifference, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
