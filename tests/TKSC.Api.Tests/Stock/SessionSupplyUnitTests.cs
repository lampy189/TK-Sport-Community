using be_da.Application.Stock;
using Xunit;

namespace TKSC.Api.Tests.Stock;

public sealed class SessionSupplyUnitTests
{
    private static readonly DateOnly Today = new(2026, 11, 6);
    private static SupplyAllocation Allocation(int issued, int returned, bool closed) =>
        new(1, "SKU", "Item", issued, returned, 9, "Referee", closed);

    [UnitCase("UTC001", "ValidateIssue", "Normal", "product=1; quantity=12; recipient=9; unit=Tube", "Return: null (valid)")]
    public void ValidateIssue_Valid() => Assert.Null(SupplyRules.ValidateIssue(new(1, 12, 9, "Tube")));

    [UnitCase("UTC002", "ValidateIssue", "Abnormal", "product=0", "Return: choose item message")]
    public void ValidateIssue_NoProduct() => Assert.Contains("vật tư", SupplyRules.ValidateIssue(new(0, 1, 9)));

    [UnitCase("UTC003", "ValidateIssue", "Abnormal", "recipient=0", "Return: choose recipient message")]
    public void ValidateIssue_NoRecipient() => Assert.Contains("người nhận", SupplyRules.ValidateIssue(new(1, 1, 0)));

    [UnitCase("UTC004", "ValidateIssue", "Boundary", "quantity=0", "Return: quantity message")]
    public void ValidateIssue_Zero() => Assert.Contains("Số lượng", SupplyRules.ValidateIssue(new(1, 0, 9)));

    [UnitCase("UTC005", "ValidateIssue", "Boundary", "quantity=100000", "Return: null (valid)")]
    public void ValidateIssue_Max() => Assert.Null(SupplyRules.ValidateIssue(new(1, 100_000, 9)));

    [UnitCase("UTC006", "ValidateIssue", "Boundary", "quantity=100001", "Return: quantity message")]
    public void ValidateIssue_AboveMax() => Assert.Contains("Số lượng", SupplyRules.ValidateIssue(new(1, 100_001, 9)));

    [UnitCase("UTC007", "ValidateIssue", "Abnormal", "unit name has 51 characters", "Return: unit name message")]
    public void ValidateIssue_LongUnit() => Assert.Contains("đơn vị", SupplyRules.ValidateIssue(new(1, 1, 9, new string('a', 51))));

    [UnitCase("UTC008", "ValidateReturn", "Normal", "returned=3", "Return: null (valid)")]
    public void ValidateReturn_Valid() => Assert.Null(SupplyRules.ValidateReturn(new(3)));

    [UnitCase("UTC009", "ValidateReturn", "Boundary", "returned=0 (everything used)", "Return: null (valid)")]
    public void ValidateReturn_Zero() => Assert.Null(SupplyRules.ValidateReturn(new(0)));

    [UnitCase("UTC010", "ValidateReturn", "Abnormal", "returned=-1", "Return: invalid quantity message")]
    public void ValidateReturn_Negative() => Assert.NotNull(SupplyRules.ValidateReturn(new(-1)));

    [UnitCase("UTC011", "ValidateReturn", "Boundary", "returned=100001", "Return: invalid quantity message")]
    public void ValidateReturn_AboveMax() => Assert.NotNull(SupplyRules.ValidateReturn(new(100_001)));

    [UnitCase("UTC012", "ValidateUse", "Normal", "used=2", "Return: null (valid)")]
    public void ValidateUse_Valid() => Assert.Null(SupplyRules.ValidateUse(new(2)));

    [UnitCase("UTC013", "ValidateUse", "Boundary", "used=0 (nothing used)", "Return: null (valid)")]
    public void ValidateUse_Zero() => Assert.Null(SupplyRules.ValidateUse(new(0)));

    [UnitCase("UTC014", "ValidateUse", "Abnormal", "used=-1", "Return: invalid quantity message")]
    public void ValidateUse_Negative() => Assert.NotNull(SupplyRules.ValidateUse(new(-1)));

    [UnitCase("UTC015", "ValidateUse", "Boundary", "used=100001", "Return: invalid quantity message")]
    public void ValidateUse_AboveMax() => Assert.NotNull(SupplyRules.ValidateUse(new(100_001)));

    [UnitCase("UTC016", "ValidateDay", "Normal", "day=today", "Return: null (valid)")]
    public void ValidateDay_Today() => Assert.Null(SupplyRules.ValidateDay(Today, Today));

    [UnitCase("UTC017", "ValidateDay", "Boundary", "day=today+366", "Return: null (valid)")]
    public void ValidateDay_Limit() => Assert.Null(SupplyRules.ValidateDay(Today.AddDays(366), Today));

    [UnitCase("UTC018", "ValidateDay", "Boundary", "day=today+367", "Return: out of range message")]
    public void ValidateDay_AfterLimit() => Assert.NotNull(SupplyRules.ValidateDay(Today.AddDays(367), Today));

    [UnitCase("UTC019", "ValidateDay", "Boundary", "day=today-367", "Return: out of range message")]
    public void ValidateDay_BeforeLimit() => Assert.NotNull(SupplyRules.ValidateDay(Today.AddDays(-367), Today));

    [UnitCase("UTC020", "SupplyAllocation.Outstanding", "Normal", "issued=12; returned=0; open", "Return: 12")]
    public void Outstanding_Open() => Assert.Equal(12, Allocation(12, 0, false).Outstanding);

    [UnitCase("UTC021", "SupplyAllocation.Outstanding", "Normal", "issued=12; returned=3; closed", "Return: 0")]
    public void Outstanding_Closed() => Assert.Equal(0, Allocation(12, 3, true).Outstanding);

    [UnitCase("UTC022", "SupplyAllocation.Outstanding", "Boundary", "issued=0; returned=0; open", "Return: 0")]
    public void Outstanding_Nothing() => Assert.Equal(0, Allocation(0, 0, false).Outstanding);

    [UnitCase("UTC023", "SupplyAllocation.Used", "Normal", "issued=12; returned=3; closed", "Return: 9")]
    public void Used_Closed() => Assert.Equal(9, Allocation(12, 3, true).Used);

    [UnitCase("UTC024", "SupplyAllocation.Used", "Abnormal", "issued=12; returned=3; still open", "Return: null (not known yet)")]
    public void Used_Open() => Assert.Null(Allocation(12, 3, false).Used);

    [UnitCase("UTC025", "SupplyAllocation.Used", "Boundary", "issued=12; returned=12; closed", "Return: 0")]
    public void Used_AllReturned() => Assert.Equal(0, Allocation(12, 12, true).Used);
}
