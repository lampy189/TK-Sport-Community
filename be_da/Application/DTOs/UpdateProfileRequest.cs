using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record UpdateProfileRequest([Required(ErrorMessage = "Full name is required.")][StringLength(255, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 255 characters.")] string? FullName, DateOnly? DateOfBirth, [StringLength(100, ErrorMessage = "Gender must not exceed 100 characters.")] string? Gender, string? AvatarUrl, [StringLength(1000, ErrorMessage = "Bio must not exceed 1000 characters.")] string? Bio = null);
