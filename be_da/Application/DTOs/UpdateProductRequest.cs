using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// Units = null keeps the current units.
public sealed record UpdateProductRequest(string? Name, string? Category, [property: JsonRequired] int UnitPriceVnd, [property: JsonRequired] int ReorderLevel, [property: JsonRequired] bool IsActive, IReadOnlyList<ProductUnitInput>? Units = null);
