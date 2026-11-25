using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;

namespace TKSC.Api.Tests.Stock;

// Task split 13/11/2026 (Dũng): referee request -> Staff issue -> use report -> confirmed return (RP3 UC-41, UC-42).
public sealed class SupplyRequestUnitTests
{
    [UnitCase("UTC001", "ValidateRequest", "Normal", "product 50; quantity 12; note \"Ca tối\"", "Return: null")]
    public void Request_Ok() => Assert.Null(SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 12, null, "Ca tối")));

    [UnitCase("UTC002", "ValidateRequest", "Abnormal", "productId 0", "Return: choose an item message")]
    public void Request_NoProduct() => Assert.NotNull(SupplyRules.ValidateRequest(new CreateSupplyRequest(0, 1)));

    [UnitCase("UTC003", "ValidateRequest", "Abnormal", "quantity 0", "Return: quantity 1..100000 message")]
    public void Request_Zero() => Assert.NotNull(SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 0)));

    [UnitCase("UTC004", "ValidateRequest", "Boundary", "quantity 100001", "Return: quantity 1..100000 message")]
    public void Request_TooLarge() => Assert.NotNull(SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 100_001)));

    [UnitCase("UTC005", "ValidateRequest", "Boundary", "note of 301 characters", "Return: note at most 300 characters message")]
    public void Request_LongNote() => Assert.NotNull(SupplyRules.ValidateRequest(new CreateSupplyRequest(50, 1, null, new string('a', 301))));

    [UnitCase("UTC006", "ValidateIssueFromRequest", "Normal", "empty body (issue what was requested)", "Return: null")]
    public void IssueRequest_Empty() => Assert.Null(SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest()));

    [UnitCase("UTC007", "ValidateIssueFromRequest", "Normal", "quantity 6 (less than requested)", "Return: null")]
    public void IssueRequest_Less() => Assert.Null(SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(6)));

    [UnitCase("UTC008", "ValidateIssueFromRequest", "Abnormal", "quantity 0", "Return: quantity 1..100000 message")]
    public void IssueRequest_Zero() => Assert.NotNull(SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(0)));

    [UnitCase("UTC009", "ValidateIssueFromRequest", "Abnormal", "unit Hộp without quantity", "Return: enter a quantity message")]
    public void IssueRequest_UnitWithoutQuantity() => Assert.NotNull(SupplyRules.ValidateIssueFromRequest(new IssueSupplyFromRequest(null, "Hộp")));

    [UnitCase("UTC010", "ValidateReject", "Normal", "note \"Hết hàng\"", "Return: null")]
    public void Reject_Ok() => Assert.Null(SupplyRules.ValidateReject(new SupplyDecisionRequest("Hết hàng")));

    [UnitCase("UTC011", "ValidateReject", "Abnormal", "note empty", "Return: enter a reason message")]
    public void Reject_Empty() => Assert.NotNull(SupplyRules.ValidateReject(new SupplyDecisionRequest("")));

    [UnitCase("UTC012", "ValidateUse", "Normal", "used 9; return 3", "Return: null")]
    public void Use_WithReturn() => Assert.Null(SupplyRules.ValidateUse(new ReportSupplyUseRequest(9, 3)));

    [UnitCase("UTC013", "ValidateUse", "Abnormal", "used 9; return -1", "Return: invalid return message")]
    public void Use_NegativeReturn() => Assert.NotNull(SupplyRules.ValidateUse(new ReportSupplyUseRequest(9, -1)));

    [UnitCase("UTC014", "Difference", "Normal", "issued 12; returned 2; reported used 9", "Return: 1 (one item unaccounted for)")]
    public void Difference_Missing() => Assert.Equal(1, SupplyRules.Difference(12, 2, 9));

    [UnitCase("UTC015", "Difference", "Normal", "issued 12; returned 3; reported used 9", "Return: 0 (fully explained)")]
    public void Difference_Matches() => Assert.Equal(0, SupplyRules.Difference(12, 3, 9));

    [UnitCase("UTC016", "Difference", "Abnormal", "issued 12; returned 5; reported used 9", "Return: -2 (more reported than left the store)")]
    public void Difference_OverReported() => Assert.Equal(-2, SupplyRules.Difference(12, 5, 9));

    [UnitCase("UTC017", "Difference", "Normal", "issued 12; returned 2; no report", "Return: null")]
    public void Difference_NoReport() => Assert.Null(SupplyRules.Difference(12, 2, null));
}
