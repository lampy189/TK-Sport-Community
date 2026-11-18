using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
// Answer to a scanned barcode or typed SKU. Quantities are counted in BaseUnit; FactorToBase converts UnitName to it.
public sealed record StockCodeLookup(long ProductId, string Sku, string Name, bool IsActive, int UnitPriceVnd, int QuantityOnHand, string BaseUnit, string UnitName, int FactorToBase, string? Barcode, int ReservedQuantity = 0)
{
    public int AvailableQuantity => QuantityOnHand - ReservedQuantity;
}
