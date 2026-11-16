namespace be_da.Application.DTOs;
public sealed record TimeSlotSummary(long Id, TimeSpan StartTime, TimeSpan EndTime, string Label)
{
    public int DurationMinutes => Math.Max(0, (int)(EndTime - StartTime).TotalMinutes);
    public bool IsHourlySlot => DurationMinutes > 0 && DurationMinutes <= 60;
}
