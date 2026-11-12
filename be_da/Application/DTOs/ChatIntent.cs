using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatIntent(string Intent, string? Date, string? Sport, long? RecordId, string Question, string? Clarification, string? StartFrom = null, string? StartBefore = null);
