using be_da.Application.Validators;

namespace be_da.Application.DTOs;
// A referee's request for supplies (RP3 UC-41 precondition). Quantity and IssuedQuantity are in the base unit.
public sealed record SupplyRequestView(long Id, long ProductId, string Sku, string Name, int Quantity, string? EnteredUnit, int? EnteredQuantity, string? Note, string Status, int? IssuedQuantity, long RequestedBy, string RequestedByName, string? HandledByName, DateTimeOffset CreatedAt, DateTimeOffset? HandledAt, string? DecisionNote, string BaseUnit = StockUnitRules.DefaultBaseUnit);
