using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed record ChatMessageItem(long Id, long SenderPlayerId, long SenderUserId, string SenderFullName, long RecipientPlayerId, long RecipientUserId, string Content, bool IsRead, DateTimeOffset CreatedAt, bool IsMine);
