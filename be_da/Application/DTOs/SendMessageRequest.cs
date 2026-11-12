using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record SendMessageRequest([Required(ErrorMessage = "Recipient player ID is required.")] long? RecipientPlayerId, [Required(ErrorMessage = "Content is required.")][StringLength(2000, MinimumLength = 1, ErrorMessage = "Message content must be between 1 and 2000 characters.")] string? Content);
