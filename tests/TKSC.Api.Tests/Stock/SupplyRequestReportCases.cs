using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.SupplyRequest;

// Report 5.1 – SupplyRequest (Lê Tuấn Dũng): Referee requests session supplies, Staff issues or rejects them,
// the Referee reports use and returns, Staff confirms the close-out (RP3 UC-41, UC-42; task split 13/11/2026).
public sealed class SupplyRequestReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string RequestRange = "Số lượng yêu cầu phải từ 1 đến 100.000.";
    private static readonly DateOnly Today = new(2026, 11, 27);

    // ---------------- SupplyRules.ValidateRequest
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Referee is assigned to the session; item is in stock")]
    public void UTC001_ValidRequest() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=6, Note=\"Cho 2 sân đôi\")",
            (string?)null, () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 6, null, "Cho 2 sân đôi")));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Smallest quantity that can be requested")]
    public void UTC002_MinimumQuantity() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=1)",
            (string?)null, () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 1)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Largest quantity that can be requested")]
    public void UTC003_MaximumQuantity() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=100000)",
            (string?)null, () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 100_000)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Quantity zero")]
    public void UTC004_ZeroQuantity() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=0)",
            RequestRange, () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 0)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Quantity one above the limit")]
    public void UTC005_QuantityAboveMaximum() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=100001)",
            RequestRange, () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 100_001)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "No supply item selected")]
    public void UTC006_MissingProduct() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=0, Quantity=6)",
            "Chọn vật tư cần yêu cầu.", () => SupplyRules.ValidateRequest(new CreateSupplyRequest(0, 6)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Note longer than 300 characters")]
    public void UTC007_NoteTooLong() =>
        Check("SupplyRules.ValidateRequest", "request=CreateSupplyRequest(ProductId=50, Quantity=6, Note=string('a', 301))",
            "Ghi chú tối đa 300 ký tự.", () => SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 6, null, new string('a', 301))));

    // ---------------- SupplyRules.ValidateIssueFromRequest
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff issues the requested quantity")]
    public void UTC008_IssueRequestedQuantity() =>
        Check("SupplyRules.ValidateIssueFromRequest", "request=IssueSupplyFromRequest(Quantity=null, UnitName=null)",
            (string?)null, () => SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest()));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff issues less than requested (5 of 6)")]
    public void UTC009_IssueLessThanRequested() =>
        Check("SupplyRules.ValidateIssueFromRequest", "request=IssueSupplyFromRequest(Quantity=5, UnitName=null)",
            (string?)null, () => SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(5)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Issue quantity zero")]
    public void UTC010_IssueZero() =>
        Check("SupplyRules.ValidateIssueFromRequest", "request=IssueSupplyFromRequest(Quantity=0, UnitName=null)",
            "Số lượng cấp phải từ 1 đến 100.000.", () => SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(0)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Unit chosen without a quantity")]
    public void UTC011_UnitWithoutQuantity() =>
        Check("SupplyRules.ValidateIssueFromRequest", "request=IssueSupplyFromRequest(Quantity=null, UnitName=\"Ống\")",
            "Nhập số lượng khi chọn đơn vị cấp.", () => SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(null, "Ống")));

    // ---------------- SupplyRules.ValidateReject
    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Staff rejects a pending request without a reason")]
    public void UTC012_RejectWithoutReason() =>
        Check("SupplyRules.ValidateReject", "request=SupplyDecisionRequest(Note=\" \")",
            "Nhập lý do từ chối.", () => SupplyRules.ValidateReject(new SupplyDecisionRequest(" ")));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff rejects a pending request with a reason")]
    public void UTC013_RejectWithReason() =>
        Check("SupplyRules.ValidateReject", "request=SupplyDecisionRequest(Note=\"Hết hàng trong kho\")",
            (string?)null, () => SupplyRules.ValidateReject(new SupplyDecisionRequest("Hết hàng trong kho")));

    // ---------------- SupplyRules.ValidateUse
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Referee received 5 and reports 4 used, 1 returned")]
    public void UTC014_ValidUsage() =>
        Check("SupplyRules.ValidateUse", "request=ReportSupplyUseRequest(UsedQuantity=4, ReturnQuantity=1)",
            (string?)null, () => SupplyRules.ValidateUse(new ReportSupplyUseRequest(4, 1)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Negative used quantity")]
    public void UTC015_NegativeUsed() =>
        Check("SupplyRules.ValidateUse", "request=ReportSupplyUseRequest(UsedQuantity=-1, ReturnQuantity=null)",
            "Số lượng đã dùng không hợp lệ.", () => SupplyRules.ValidateUse(new ReportSupplyUseRequest(-1)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Returned quantity above the limit")]
    public void UTC016_ReturnAboveMaximum() =>
        Check("SupplyRules.ValidateUse", "request=ReportSupplyUseRequest(UsedQuantity=4, ReturnQuantity=100001)",
            "Số lượng trả lại không hợp lệ.", () => SupplyRules.ValidateUse(new ReportSupplyUseRequest(4, 100_001)));

    // ---------------- SupplyRules.Difference
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Issued 5, returned 1, reported used 4")]
    public void UTC017_CloseOutBalanced() =>
        Check("SupplyRules.Difference", "issued=5; returned=1; reportedUsed=4", (int?)0, () => SupplyRules.Difference(5, 1, 4));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Issued 6, returned 2, reported used 3")]
    public void UTC018_CloseOutUnaccounted() =>
        Check("SupplyRules.Difference", "issued=6; returned=2; reportedUsed=3", (int?)1, () => SupplyRules.Difference(6, 2, 3));

    [Fact, Trait("Type", "Normal"), Trait("Pre", "Referee has not reported usage yet")]
    public void UTC019_CloseOutNotReported() =>
        Check("SupplyRules.Difference", "issued=5; returned=0; reportedUsed=null", (int?)null, () => SupplyRules.Difference(5, 0, null));

    // ---------------- SupplyRules.ValidateDay
    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Viewing supplies 366 days ahead of 27/11/2026")]
    public void UTC020_DayWithinOneYear() =>
        Check("SupplyRules.ValidateDay", "day=DateOnly(2027, 11, 28); today=DateOnly(2026, 11, 27)",
            (string?)null, () => SupplyRules.ValidateDay(Today.AddDays(366), Today));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Viewing supplies 367 days ahead of 27/11/2026")]
    public void UTC021_DayBeyondOneYear() =>
        Check("SupplyRules.ValidateDay", "day=DateOnly(2027, 11, 29); today=DateOnly(2026, 11, 27)",
            "Ngày xem phải trong vòng một năm so với hôm nay.", () => SupplyRules.ValidateDay(Today.AddDays(367), Today));
}
