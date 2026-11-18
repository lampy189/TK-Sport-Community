using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatLookup(string Scope, DateOnly? OnDate = null, string? Sport = null, long? RecordId = null, TimeOnly? StartFrom = null, TimeOnly? StartBefore = null);
