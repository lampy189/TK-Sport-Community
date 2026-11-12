using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record ProductUnit(long Id, string UnitName, int FactorToBase, string? Barcode)
{
    public bool IsBase => FactorToBase == 1;
}
