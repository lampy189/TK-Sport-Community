using be_da.Application.Validators;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record StockProduct(long Id, string Sku, string Name, string? Category, int UnitPriceVnd, bool IsActive, int QuantityOnHand, int ReorderLevel, string BaseUnit = StockUnitRules.DefaultBaseUnit, IReadOnlyList<ProductUnit>? Units = null, bool UnitsLocked = false)
{
    public bool IsLowStock => IsActive && QuantityOnHand <= ReorderLevel;
    public string QuantityBreakdown => StockUnitRules.FormatBreakdown(QuantityOnHand, Units ?? [], BaseUnit);
}
