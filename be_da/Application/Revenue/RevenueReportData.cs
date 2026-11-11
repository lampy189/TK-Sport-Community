namespace be_da.Application.Revenue;

public sealed record RevenueEntry(DateOnly Date, long Social, long Goods, long Refunds);

public sealed record RevenueReportData(
    bool VenueExists,
    IReadOnlyList<RevenueEntry> Entries,
    long UnpostedPayments);
