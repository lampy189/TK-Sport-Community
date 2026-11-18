using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record ReceiptCatalogItem(long ProductId, string Sku, string Name, bool IsActive, IReadOnlyList<ProductUnit> Units);
