using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record FriendshipActionResult(bool Success, string? ErrorCode, string? Message, long? FriendshipId);
