namespace be_da.Application.DTOs;
public sealed record SessionDetails(SessionSummary Summary, IReadOnlyList<SessionParticipant> PublicParticipants);
