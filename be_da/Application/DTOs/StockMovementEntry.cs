using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record StockMovementEntry(long Id, string MovementType, int Quantity, string? ReferenceType, long? ReferenceId, long? CreatedBy, string? CreatedByName, DateTimeOffset? CreatedAt = null, string? EnteredUnit = null, int? EnteredQuantity = null, string? SourceRef = null);
