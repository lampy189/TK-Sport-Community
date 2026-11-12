using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
// ReturnQuantity: unused items the referee hands back; Staff confirms what was physically received.
public sealed record ReportSupplyUseRequest([property: JsonRequired] int UsedQuantity, int? ReturnQuantity = null);
