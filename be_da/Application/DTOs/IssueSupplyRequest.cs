using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
// Quantity is in UnitName; an empty UnitName means the base unit.
public sealed record IssueSupplyRequest([property: JsonRequired] long ProductId, [property: JsonRequired] int Quantity, [property: JsonRequired] long RecipientId, string? UnitName = null);
