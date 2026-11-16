namespace be_da.Application.DTOs;
public sealed record CenterRevenue(long VenueId, string Venue, long Social, long Goods, long Refunds)
{
    public long Net => Social + Goods - Refunds;
}
public sealed record InventoryReport(long VenueId, long ProductId, string Product, long Opening,
    long Received, long Issued, long Closing, long CurrentOnHand, long UndatedMovements);
public sealed record SupplyReport(long VenueId, long SessionId, long ProductId, long Issued,
    long Returned, long? ReconciledConsumption)
{
    public long Unresolved => ReconciledConsumption.HasValue ? 0 : Issued - Returned;
}
public sealed record OutstandingReport(long VenueId, long ReconciliationId, DateOnly? Date,
    string? Status, long ExpectedBank, long ActualBank, long Difference);
public sealed record PendingMoney(long VenueId, long PaymentId, long? RefundId, string Kind, long Amount);
public sealed record BusinessReport(DateOnly From, DateOnly To, long? VenueId,
    IReadOnlyList<CenterRevenue> Centers, IReadOnlyList<InventoryReport> Inventory,
    IReadOnlyList<SupplyReport> Supplies, IReadOnlyList<OutstandingReport> Reconciliations,
    IReadOnlyList<PendingMoney> CurrentOutstanding);
