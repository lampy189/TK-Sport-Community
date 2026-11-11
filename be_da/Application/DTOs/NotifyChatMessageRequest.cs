using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record NotifyChatMessageRequest([Required(ErrorMessage = "Recipient player ID is required.")] long? RecipientPlayerId, [Required(ErrorMessage = "Message preview is required.")][StringLength(500, MinimumLength = 1, ErrorMessage = "Message preview must be between 1 and 500 characters.")] string? MessagePreview);
