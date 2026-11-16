namespace be_da.Application.DTOs;

public sealed record SubmitPolicyRequest(string Code, string Name, string Value, string Reason,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, long VenueId,
    string Kind = "policy", long? TargetId = null, int? PriceVnd = null);
public sealed record DecidePolicyRequest(bool Approve, string Reason);
public sealed record PolicyVersion(long Id, string Code, int Version, string Name, string Value,
    string Kind, long? TargetId, int? PriceVnd, long VenueId, string Status,
    DateTimeOffset EffectiveFrom, DateTimeOffset? EffectiveTo, long SubmittedBy,
    DateTimeOffset SubmittedAt, string SubmitReason, long? DecidedBy, DateTimeOffset? DecidedAt, string? DecisionReason);
