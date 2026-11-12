namespace be_da.Application.DTOs;

public sealed record InviteSessionRequest(long FriendPlayerId, string PaymentChoice = "separate");
public sealed record InvitationReply(bool Accept);
public sealed record InvitationCandidate(long PlayerId, string Name, bool Eligible, string? Reason);
public sealed record SessionInvitation(long Id, long SessionId, long VenueId, string Venue, string Court,
    DateOnly Date, TimeOnly StartsAt, string Sport, int AmountVnd, long InviterUserId, string Inviter,
    long InviteeUserId, string Invitee, long PayerUserId, string Status, DateTimeOffset ExpiresAt,
    long? OrderId, long? TicketId, string? PaymentStatus, DateTimeOffset? PaymentDeadline);
