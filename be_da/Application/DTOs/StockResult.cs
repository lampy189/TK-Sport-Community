using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record StockResult(bool Success, string? ErrorCode, string? Message, StockProduct? Product = null)
{
    public static StockResult Ok(StockProduct product) => new(true, null, null, product);
    public static StockResult Fail(string code, string message) => new(false, code, message);
}
