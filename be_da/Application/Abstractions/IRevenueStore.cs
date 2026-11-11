using be_da.Application.Revenue;

namespace be_da.Application.Abstractions;

public interface IRevenueStore
{
    Task<RevenueReportData> GetDailyAsync(
        DateOnly from,
        DateOnly to,
        long? venueId,
        CancellationToken cancellationToken);
}
