using be_da.Application.Validators;
using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
// Issued and returned come from posted movements; used = issued - returned once reconciled.
// ReturnOffered is what the referee says will come back; Difference = (issued - returned) - reported use,
// recorded at close-out: positive means items are unaccounted for (lost/damaged), for a stock count (RP3 UC-42 step 6).
public sealed record SupplyAllocation(long ProductId, string Sku, string Name, int Issued, int Returned, long? RecipientId, string? RecipientName, bool IsReconciled, int? ReportedUsed = null, string BaseUnit = StockUnitRules.DefaultBaseUnit, int? ReturnOffered = null, int? Difference = null)
{
    public int Outstanding => IsReconciled ? 0 : Issued - Returned;
    public int? Used => IsReconciled ? Issued - Returned : null;
}
