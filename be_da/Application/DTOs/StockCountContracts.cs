using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
// RP3 UC-43 / UC-44: a physical count never changes stock; a separately approved adjustment is applied once.
// Quantities are in the base unit. StockVersion is the last movement id seen when the count started (the "data version").
public sealed record StockCountSheet(long ProductId, string Sku, string Name, int QuantityOnHand, int ReservedQuantity, long StockVersion, string BaseUnit, IReadOnlyList<ProductUnit> Units);

public sealed record CreateStockCountRequest(
    [property: JsonRequired] long ProductId,
    [property: JsonRequired] int CountedQuantity,
    [property: JsonRequired] long StockVersion,
    string? UnitName = null,
    string? Reason = null,
    string? Evidence = null);

// ExpectedDifference: the difference the approver looked at; a changed proposal needs a new review (UC-44 A3).
public sealed record StockCountDecisionRequest(string? Note = null, int? ExpectedDifference = null);

public sealed record StockCountView(long Id, long ProductId, string Sku, string Name, int SystemQuantity, int CountedQuantity, int Difference,
    string? EnteredUnit, int? EnteredQuantity, string Status, string? Reason, string? Evidence,
    long CountedBy, string CountedByName, DateTimeOffset? CountedAt, long? DecidedBy, string? DecidedByName, DateTimeOffset? DecidedAt,
    string? DecisionNote, long? AppliedMovementId, string BaseUnit, int CurrentOnHand, long? StockVersion);

public sealed record StockCountResult(bool Success, string? ErrorCode, string? Message, StockCountView? Count = null, StockCountSheet? Sheet = null)
{
    public static StockCountResult Ok(StockCountView count) => new(true, null, null, count);
    public static StockCountResult Fail(string code, string message, StockCountSheet? sheet = null) => new(false, code, message, null, sheet);
}

// Supply and stock movement totals of one product over a period, for the revenue/operations report (Lâm).
public sealed record SupplyReportLine(long ProductId, string Sku, string Name, string? Category, string BaseUnit,
    int Received, int SoldOut, int SessionIssued, int SessionReturned, int SessionUsed, int ManualIssued, int AdjustedIn, int AdjustedOut,
    int UnaccountedSupply, int QuantityOnHand, int ReservedQuantity);
public sealed record SupplyReport(DateOnly From, DateOnly To, long? VenueId, IReadOnlyList<SupplyReportLine> Lines);

// Stock = sum of movements and reserved = sum of held order lines must always hold; any row here needs a count.
public sealed record StockReconciliationLine(long ProductId, string Sku, string Name, int QuantityOnHand, int MovementTotal, int ReservedQuantity, int HeldOrderQuantity)
{
    public bool OnHandMatches => QuantityOnHand == MovementTotal;
    public bool ReservedMatches => ReservedQuantity == HeldOrderQuantity;
}
