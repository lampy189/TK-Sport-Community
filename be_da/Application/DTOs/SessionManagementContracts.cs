using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;

public sealed record SessionSportOption(long Id, string Name);
public sealed record SessionCourtOption(long Id, string Name, long? SportId);
public sealed record SessionPersonnelOption(long Id, string Name, string Role);
public sealed record SessionManagementOptions(
    IReadOnlyList<SessionSportOption> Sports,
    IReadOnlyList<SessionCourtOption> Courts,
    IReadOnlyList<TimeSlotSummary> TimeSlots,
    IReadOnlyList<SessionPersonnelOption> Personnel);
public sealed record SessionAssignee(long Id, string Name);
public sealed record ManagedSession(
    long Id,
    long SportId,
    string SportName,
    long CourtId,
    string CourtName,
    long TimeSlotId,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly SessionDate,
    string? Band,
    short Capacity,
    int PriceVnd,
    string Status,
    short OccupiedPlaces,
    IReadOnlyList<SessionAssignee> Referees,
    IReadOnlyList<SessionAssignee> Staff);

public sealed record SaveManagedSessionRequest(
    [property: Range(1, long.MaxValue)] long SportId,
    [property: Range(1, long.MaxValue)] long CourtId,
    [property: Range(1, long.MaxValue)] long TimeSlotId,
    DateOnly SessionDate,
    [property: StringLength(100)] string? Band,
    [property: Range(1, 8)] short Capacity,
    [property: Range(0, int.MaxValue)] int PriceVnd);

public sealed record SessionAssignmentsRequest(long[]? RefereeIds, long[]? StaffIds);