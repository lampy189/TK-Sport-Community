using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface IRevenueService
{
    Task<OperationResult> DailyAsync(DateOnly from, DateOnly to, long? venueId, CancellationToken ct);
}
