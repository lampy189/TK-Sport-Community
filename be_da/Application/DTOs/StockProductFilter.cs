using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record StockProductFilter(string? Search = null, string? Category = null, bool IncludeInactive = true);
