using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// Code is a barcode or SKU.
public sealed record BatchReceiptLine(string? Code, string? UnitName, [property: JsonRequired] int Quantity);
