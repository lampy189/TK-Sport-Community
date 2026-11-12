using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record BatchReceiptResult(bool Success, string? Code, string? Message, bool AlreadyPosted, IReadOnlyList<MergedReceiptLine> Posted, IReadOnlyList<BatchLineError> LineErrors)
{
    public static BatchReceiptResult Ok(IReadOnlyList<MergedReceiptLine> posted, bool alreadyPosted = false) => new(true, null, null, alreadyPosted, posted, []);
    public static BatchReceiptResult Fail(string code, string message, IReadOnlyList<BatchLineError>? lineErrors = null) => new(false, code, message, false, [], lineErrors ?? []);
}
