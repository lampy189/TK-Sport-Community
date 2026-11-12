using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace be_da.Application.DTOs;
public sealed record BatchReceiptRequest(string? SourceRef, IReadOnlyList<BatchReceiptLine>? Lines);
