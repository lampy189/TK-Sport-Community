using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// [JsonRequired] on value types: a missing field must be rejected, not read as 0 or false.
public sealed record CreateProductRequest(string? Sku, string? Name, string? Category, [property: JsonRequired] int UnitPriceVnd, [property: JsonRequired] int ReorderLevel, [property: JsonRequired] int InitialQuantity, IReadOnlyList<ProductUnitInput>? Units = null);
