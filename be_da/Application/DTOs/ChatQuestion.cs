using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatQuestion([Required, StringLength(1200, MinimumLength = 2)] string Question, [RegularExpression("^(auto|sessions|tickets|payments|refunds|policies)$")] string Scope = "auto", DateOnly? OnDate = null, [RegularExpression("^(badminton|pickleball)$")] string? Sport = null, [Range(1, long.MaxValue)] long? RecordId = null, ChatContext? Context = null);
