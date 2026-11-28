using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;
using Xunit.Abstractions;

namespace TKSC.Api.Tests.UnitTests.SessionSupply;

// Report 5.1 – SessionSupply (Lê Tuấn Dũng): Staff issues supplies to a session and confirms what came back; the
// allocation shows what is still out and what was used (RP3 UC-41, UC-42; task split 05/11/2026).
// Referee requests and usage reports are in SupplyRequestReportCases.
public sealed class SessionSupplyReportCases(ITestOutputHelper output) : ReportCaseOutput(output)
{
    private const string IssueQuantity = "Số lượng cấp phải từ 1 đến 100.000.";
    private const string ReturnInvalid = "Số lượng trả lại không hợp lệ.";
    private static readonly DateOnly Today = new(2026, 11, 27);

    private static SupplyAllocation Allocation(int issued, int returned, bool closed) => new(50, "VP-SHUTTLE", "Quả cầu lông", issued, returned, 60095, "Trọng tài Vĩnh Phúc 01", closed);

    // ---------------- SupplyRules.ValidateIssue
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff issues 12 shuttlecocks to the session referee")]
    public void UTC001_IssueValid() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=12, RecipientId=60095, UnitName=\"Ống\")",
            (string?)null, () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 12, 60095, "Ống")));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "No supply item selected")]
    public void UTC002_IssueNoProduct() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=0, Quantity=1, RecipientId=60095)", "Chọn vật tư cần cấp.", () => SupplyRules.ValidateIssue(new IssueSupplyRequest(0, 1, 60095)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "No recipient selected")]
    public void UTC003_IssueNoRecipient() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=1, RecipientId=0)", "Chọn người nhận vật tư.", () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 1, 0)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Issue quantity zero")]
    public void UTC004_IssueZero() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=0, RecipientId=60095)", IssueQuantity, () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 0, 60095)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Issue quantity at the maximum")]
    public void UTC005_IssueMaximum() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=100000, RecipientId=60095)", (string?)null, () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 100_000, 60095)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Issue quantity one above the maximum")]
    public void UTC006_IssueAboveMaximum() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=100001, RecipientId=60095)", IssueQuantity, () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 100_001, 60095)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Unit name of 51 characters")]
    public void UTC007_IssueLongUnit() =>
        Check("SupplyRules.ValidateIssue", "request=IssueSupplyRequest(ProductId=50, Quantity=1, RecipientId=60095, UnitName=string('a', 51))",
            "Tên đơn vị tối đa 50 ký tự.", () => SupplyRules.ValidateIssue(new IssueSupplyRequest(50, 1, 60095, new string('a', 51))));

    // ---------------- SupplyRules.ValidateReturn
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff confirms 3 items came back")]
    public void UTC008_ReturnValid() => Check("SupplyRules.ValidateReturn", "request=ReturnSupplyRequest(ReturnedQuantity=3)", (string?)null, () => SupplyRules.ValidateReturn(new ReturnSupplyRequest(3)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Everything was used, nothing came back")]
    public void UTC009_ReturnZero() => Check("SupplyRules.ValidateReturn", "request=ReturnSupplyRequest(ReturnedQuantity=0)", (string?)null, () => SupplyRules.ValidateReturn(new ReturnSupplyRequest(0)));

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Negative returned quantity")]
    public void UTC010_ReturnNegative() => Check("SupplyRules.ValidateReturn", "request=ReturnSupplyRequest(ReturnedQuantity=-1)", ReturnInvalid, () => SupplyRules.ValidateReturn(new ReturnSupplyRequest(-1)));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Returned quantity one above the maximum")]
    public void UTC011_ReturnAboveMaximum() =>
        Check("SupplyRules.ValidateReturn", "request=ReturnSupplyRequest(ReturnedQuantity=100001)", ReturnInvalid, () => SupplyRules.ValidateReturn(new ReturnSupplyRequest(100_001)));

    // ---------------- SupplyRules.ValidateDay
    [Fact, Trait("Type", "Normal"), Trait("Pre", "Staff opens today's sessions")]
    public void UTC012_DayToday() => Check("SupplyRules.ValidateDay", "day=DateOnly(2026, 11, 27); today=DateOnly(2026, 11, 27)", (string?)null, () => SupplyRules.ValidateDay(Today, Today));

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Viewing a day 367 days in the past")]
    public void UTC013_DayTooFarBack() =>
        Check("SupplyRules.ValidateDay", "day=DateOnly(2025, 11, 25); today=DateOnly(2026, 11, 27)", "Ngày xem phải trong vòng một năm so với hôm nay.", () => SupplyRules.ValidateDay(Today.AddDays(-367), Today));

    // ---------------- SupplyAllocation.Outstanding / Used
    [Fact, Trait("Type", "Normal"), Trait("Pre", "12 issued, nothing returned, not closed yet")]
    public void UTC014_OutstandingOpen() => Check("SupplyAllocation.Outstanding", "issued=12; returned=0; isReconciled=false", 12, () => Allocation(12, 0, false).Outstanding);

    [Fact, Trait("Type", "Normal"), Trait("Pre", "12 issued, 3 returned, closed by Staff")]
    public void UTC015_OutstandingClosed() => Check("SupplyAllocation.Outstanding", "issued=12; returned=3; isReconciled=true", 0, () => Allocation(12, 3, true).Outstanding);

    [Fact, Trait("Type", "Normal"), Trait("Pre", "12 issued, 3 returned, closed by Staff")]
    public void UTC016_UsedClosed() => Check("SupplyAllocation.Used", "issued=12; returned=3; isReconciled=true", (int?)9, () => Allocation(12, 3, true).Used);

    [Fact, Trait("Type", "Abnormal"), Trait("Pre", "Allocation still open; use not known yet")]
    public void UTC017_UsedOpen() => Check("SupplyAllocation.Used", "issued=12; returned=3; isReconciled=false", (int?)null, () => Allocation(12, 3, false).Used);

    [Fact, Trait("Type", "Boundary"), Trait("Pre", "Everything came back")]
    public void UTC018_UsedAllReturned() => Check("SupplyAllocation.Used", "issued=12; returned=12; isReconciled=true", (int?)0, () => Allocation(12, 12, true).Used);
}
