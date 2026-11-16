using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record SendFriendRequest([Required(ErrorMessage = "Target player ID is required.")] long? TargetPlayerId);
