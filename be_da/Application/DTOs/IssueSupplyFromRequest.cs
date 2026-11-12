namespace be_da.Application.DTOs;
// Staff may issue less than requested when stock is short; empty body issues the requested quantity.
public sealed record IssueSupplyFromRequest(int? Quantity = null, string? UnitName = null);
