using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// Quantity is in UnitName; an empty UnitName means the base unit.
public sealed record StockMovementRequest(string? Type, [property: JsonRequired] int Quantity, string? UnitName = null);
