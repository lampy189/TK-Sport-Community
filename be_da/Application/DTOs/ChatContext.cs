using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatContext([Required, StringLength(1200)] string Question, [StringLength(400)] string? Clarification = null);
