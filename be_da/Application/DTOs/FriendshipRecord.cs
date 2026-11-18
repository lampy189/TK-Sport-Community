namespace be_da.Application.DTOs;
public sealed record FriendshipRecord(long Id, long RequesterPlayerId, long AddresseePlayerId, string Status, DateTimeOffset CreatedAt, DateTimeOffset? RespondedAt);
