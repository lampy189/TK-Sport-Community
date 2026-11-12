using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record ResolvedReceiptLine(int LineNo, long ProductId, string Name, string UnitName, int Quantity, int BaseQuantity);
