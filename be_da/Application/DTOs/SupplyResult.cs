using System.Text.Json.Serialization;

namespace be_da.Application.DTOs;
public sealed record SupplyResult(bool Success, string? ErrorCode, string? Message, SessionSupplyDetails? Details = null)
{
    public static SupplyResult Ok(SessionSupplyDetails details) => new(true, null, null, details);
    public static SupplyResult Fail(string code, string message) => new(false, code, message);
}
