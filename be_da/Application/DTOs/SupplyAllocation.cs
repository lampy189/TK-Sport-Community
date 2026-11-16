using be_da.Application.Validators;
using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
// Issued and returned come from posted movements; used = issued - returned once reconciled.
public sealed record SupplyAllocation(long ProductId, string Sku, string Name, int Issued, int Returned, long? RecipientId, string? RecipientName, bool IsReconciled, int? ReportedUsed = null, string BaseUnit = StockUnitRules.DefaultBaseUnit)
{
    public int Outstanding => IsReconciled ? 0 : Issued - Returned;
    public int? Used => IsReconciled ? Issued - Returned : null;
}
