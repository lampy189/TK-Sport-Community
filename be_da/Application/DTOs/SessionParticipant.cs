namespace be_da.Application.DTOs;
public sealed record SessionParticipant(long TicketId, long PlayerId, string FullName, string? AvatarUrl, string? CurrentBand, string TicketStatus);
