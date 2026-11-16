using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record RegisterSessionResult(bool Success, string? ErrorCode, string? Message, string? Reason = null, long? OrderId = null, string? OrderCode = null, long? TicketId = null, string? TicketCode = null, int? AmountVnd = null, DateTimeOffset? ExpiresAt = null);
