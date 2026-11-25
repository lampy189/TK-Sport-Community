using be_da.Application.DTOs;
using be_da.Application.Validators;
using Xunit;

namespace TKSC.Api.Tests.Stock;

// Task split 13/11/2026 (Dũng): physical count, proposed adjustment, independent approval (RP3 UC-43, UC-44).
public sealed class StockCountUnitTests
{
    private static CreateStockCountRequest Count(int counted = 89, long version = 10, string? reason = "Thiếu 1", string? unit = null, string? evidence = null) =>
        new(50, counted, version, unit, reason, evidence);

    [UnitCase("UTC001", "ValidateCreate", "Normal", "product 50; counted 89; version 10; reason given", "Return: null")]
    public void Create_Ok() => Assert.Null(StockCountRules.ValidateCreate(Count()));

    [UnitCase("UTC002", "ValidateCreate", "Boundary", "counted 0 (everything missing)", "Return: null")]
    public void Create_Zero() => Assert.Null(StockCountRules.ValidateCreate(Count(counted: 0)));

    [UnitCase("UTC003", "ValidateCreate", "Abnormal", "counted -1", "Return: quantity 0..100000 message")]
    public void Create_Negative() => Assert.NotNull(StockCountRules.ValidateCreate(Count(counted: -1)));

    [UnitCase("UTC004", "ValidateCreate", "Boundary", "counted 100001", "Return: quantity 0..100000 message")]
    public void Create_TooLarge() => Assert.NotNull(StockCountRules.ValidateCreate(Count(counted: 100_001)));

    [UnitCase("UTC005", "ValidateCreate", "Abnormal", "productId 0", "Return: choose an item message")]
    public void Create_NoProduct() => Assert.NotNull(StockCountRules.ValidateCreate(new CreateStockCountRequest(0, 1, 1)));

    [UnitCase("UTC006", "ValidateCreate", "Abnormal", "stock version -1", "Return: reload the count sheet message")]
    public void Create_BadVersion() => Assert.NotNull(StockCountRules.ValidateCreate(Count(version: -1)));

    [UnitCase("UTC007", "ValidateCreate", "Boundary", "reason of 501 characters", "Return: at most 500 characters message")]
    public void Create_LongReason() => Assert.NotNull(StockCountRules.ValidateCreate(Count(reason: new string('a', 501))));

    [UnitCase("UTC008", "ValidateCreate", "Abnormal", "unit name of 51 characters", "Return: unit name too long message")]
    public void Create_LongUnit() => Assert.NotNull(StockCountRules.ValidateCreate(Count(unit: new string('a', 51))));

    [UnitCase("UTC009", "ValidateReason", "Abnormal", "difference -1; reason empty", "Return: enter a reason message")]
    public void Reason_RequiredForDifference() => Assert.NotNull(StockCountRules.ValidateReason(-1, " "));

    [UnitCase("UTC010", "ValidateReason", "Normal", "difference 0; no reason", "Return: null (a matching count needs no reason)")]
    public void Reason_NotNeededWhenMatching() => Assert.Null(StockCountRules.ValidateReason(0, null));

    [UnitCase("UTC011", "Difference", "Normal", "counted 89; system 90", "Return: -1")]
    public void Difference_Missing() => Assert.Equal(-1, StockCountRules.Difference(89, 90));

    [UnitCase("UTC012", "Difference", "Normal", "counted 92; system 90", "Return: +2")]
    public void Difference_Extra() => Assert.Equal(2, StockCountRules.Difference(92, 90));

    [UnitCase("UTC013", "StatusFor", "Normal", "difference 0", "Return: NoDifference (no adjustment request)")]
    public void Status_NoDifference() => Assert.Equal(StockCountRules.NoDifference, StockCountRules.StatusFor(0));

    [UnitCase("UTC014", "StatusFor", "Normal", "difference -3", "Return: Pending (goes to the Manager)")]
    public void Status_Pending() => Assert.Equal(StockCountRules.Pending, StockCountRules.StatusFor(-3));

    [UnitCase("UTC015", "ValidateApprove", "Normal", "expectedDifference -1; note empty", "Return: null")]
    public void Approve_Ok() => Assert.Null(StockCountRules.ValidateApprove(new StockCountDecisionRequest(null, -1)));

    [UnitCase("UTC016", "ValidateApprove", "Abnormal", "expectedDifference missing", "Return: reload the proposal message")]
    public void Approve_NoExpected() => Assert.NotNull(StockCountRules.ValidateApprove(new StockCountDecisionRequest("ok")));

    [UnitCase("UTC017", "ValidateReject", "Normal", "note \"Đếm sai\"", "Return: null")]
    public void Reject_Ok() => Assert.Null(StockCountRules.ValidateReject(new StockCountDecisionRequest("Đếm sai")));

    [UnitCase("UTC018", "ValidateReject", "Abnormal", "note empty", "Return: enter a reason message")]
    public void Reject_NoNote() => Assert.NotNull(StockCountRules.ValidateReject(new StockCountDecisionRequest("  ")));

    [UnitCase("UTC019", "ValidateDecider", "Abnormal", "decider 6 = counter 6", "Return: no self approval message")]
    public void Decider_Self() => Assert.NotNull(StockCountRules.ValidateDecider(6, 6));

    [UnitCase("UTC020", "ValidateDecider", "Normal", "decider 3 (Manager); counter 6 (Staff)", "Return: null")]
    public void Decider_Other() => Assert.Null(StockCountRules.ValidateDecider(3, 6));

    [UnitCase("UTC021", "StockReconciliationLine", "Normal", "on hand 89 = movements 89; reserved 1 = held 1", "Return: OnHandMatches and ReservedMatches true")]
    public void Reconciliation_Matches()
    {
        var line = new StockReconciliationLine(50, "VP-SHUTTLE", "Quả cầu lông", 89, 89, 1, 1);
        Assert.True(line.OnHandMatches && line.ReservedMatches);
    }

    [UnitCase("UTC022", "StockReconciliationLine", "Abnormal", "on hand 100 but movements 90; reserved 2 but held 1", "Return: both flags false")]
    public void Reconciliation_Mismatch()
    {
        var line = new StockReconciliationLine(50, "VP-SHUTTLE", "Quả cầu lông", 100, 90, 2, 1);
        Assert.False(line.OnHandMatches);
        Assert.False(line.ReservedMatches);
    }
}
