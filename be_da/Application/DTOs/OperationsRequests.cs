namespace be_da.Application.DTOs;
public sealed record VenueEdit(string Name, string? Address, bool Active = true);
public sealed record CourtEdit(long VenueId, long SportId, string Name, bool Active = true);
public sealed record PersonnelEdit(long? VenueId, long? ReplacementUserId = null);
public sealed record AccessProposal(long TargetUserId, string Role, long? VenueId, string Action, string Reason, long? OutgoingUserId = null);
public sealed record ApprovalDecision(bool Approve, string Reason);
public sealed record TrialFeedback(string Content);
