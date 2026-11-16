using TKSC.Domain.Exceptions;

namespace TKSC.Application.DTOs;
public sealed record NotificationItem(long Id, string? Title, long? ReferenceId, string? ReferenceType, string? Status);
public sealed record NotificationPage(IReadOnlyList<NotificationItem> Items, int Unread, long Cursor);
