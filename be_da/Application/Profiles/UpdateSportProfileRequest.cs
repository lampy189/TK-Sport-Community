using System.ComponentModel.DataAnnotations;
using be_da.Domain.Entities;

namespace be_da.Application.Profiles;

public sealed record UpdateSportProfileRequest(
    [Required(ErrorMessage = "Score is required.")]
    [Range(0, 100, ErrorMessage = "Score must be between 0 and 100.")]
    short? Score,

    [StringLength(100, ErrorMessage = "Band must not exceed 100 characters.")]
    string? Band,

    [StringLength(1000, ErrorMessage = "Experience notes must not exceed 1000 characters.")]
    string? ExperienceNotes
);

public sealed record SportProfileResult(
    bool Success,
    string? ErrorCode,
    string? Message,
    SportProfileItem? Profile
);

