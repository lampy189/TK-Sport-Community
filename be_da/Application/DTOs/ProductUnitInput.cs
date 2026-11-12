using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record ProductUnitInput(string? UnitName, [property: JsonRequired] int FactorToBase, string? Barcode = null);
