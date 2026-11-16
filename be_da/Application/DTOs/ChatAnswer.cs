using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatAnswer(string Answer, string Mode, IReadOnlyList<ChatSource> Sources, DateTimeOffset? RetrievedAt = null, string? Scope = null, BookingContact? BookingContact = null, ChatContext? Context = null, string? Notice = null);
