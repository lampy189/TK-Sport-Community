using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record RespondFriendRequest([Required(ErrorMessage = "Accept boolean is required.")] bool? Accept);
