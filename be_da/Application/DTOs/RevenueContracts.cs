namespace be_da.Application.DTOs;
public sealed record RevenueDay(DateOnly Date, long Social, long Goods, long Refunds)
{
    public long Net => Social + Goods - Refunds;
}

public sealed record RevenueSnapshot(Dictionary<DateOnly, RevenueDay> Rows, long Unposted);
