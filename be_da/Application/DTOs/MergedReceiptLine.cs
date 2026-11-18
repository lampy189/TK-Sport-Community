using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// EnteredUnit/EnteredQuantity stay null when one product was entered in several units in the same receipt.
public sealed record MergedReceiptLine(long ProductId, string Name, int BaseQuantity, string? EnteredUnit, int? EnteredQuantity);
