using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record ReturnSupplyRequest([property: JsonRequired] int ReturnedQuantity);
