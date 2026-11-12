using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record CreateSupplyRequest([property: JsonRequired] long ProductId, [property: JsonRequired] int Quantity, string? UnitName = null, string? Note = null);
