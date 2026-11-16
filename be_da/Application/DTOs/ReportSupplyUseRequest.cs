using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record ReportSupplyUseRequest([property: JsonRequired] int UsedQuantity);
