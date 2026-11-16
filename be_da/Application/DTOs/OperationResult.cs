namespace be_da.Application.DTOs;
public enum OperationOutcome
{
    Success,
    Invalid,
    Missing,
    Conflict,
    Denied,
    Created,
    Completed
}

public sealed record OperationResult(OperationOutcome Outcome, object? Value)
{
    public static OperationResult Success(object? value) => new(OperationOutcome.Success, value);
    public static OperationResult Invalid(object? value) => new(OperationOutcome.Invalid, value);
    public static OperationResult Missing(object? value) => new(OperationOutcome.Missing, value);
    public static OperationResult Conflict(object? value) => new(OperationOutcome.Conflict, value);
    public static OperationResult Denied(object? value = null) => new(OperationOutcome.Denied, value);
    public static OperationResult Created(object? value) => new(OperationOutcome.Created, value);
    public static OperationResult Completed() => new(OperationOutcome.Completed, null);
}
