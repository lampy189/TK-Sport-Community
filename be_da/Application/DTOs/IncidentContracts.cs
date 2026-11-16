namespace TKSC.Application.DTOs;
public record IncidentActor(long Id, string Name, string Role, long? VenueId);
public record IncidentResource(string Kind, long Id, long VenueId, string Title, string BusinessStatus, bool CanResolve);
public record IncidentState(string Status = "open", long? AssignedTo = null, string? AssignedName = null, string Note = "", long Version = 0, long? UpdatedBy = null, DateTimeOffset At = default);
public record IncidentAction(string Action, string Reason, long ExpectedVersion, long? AssigneeId = null);
public record OperationalRequest(string Kind, long SubjectId, string Category, string Reason, long? ProductId = null, int? QuantityDelta = null, long? BankTransactionId = null, int? AmountVnd = null, long? RecipientId = null);
public record OperationalCase(long Id, string Kind, long SubjectId, string Category, string Reason, long RequestedBy, string RequesterRole, string Status, long? DecidedBy = null, string? Decision = null, DateTimeOffset RequestedAt = default, DateTimeOffset UpdatedAt = default, long? ProductId = null, int? QuantityDelta = null, long? BankTransactionId = null, int? AmountVnd = null, long? RecipientId = null);
public record OperationalDecision(bool Approve, string Reason);
