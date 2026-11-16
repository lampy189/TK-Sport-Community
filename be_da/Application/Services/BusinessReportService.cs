using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;
namespace be_da.Application.Services;
public sealed class BusinessReportService(IBusinessReportRepository repository) : IBusinessReportService
{
    public Task<BusinessReport> ReadAsync(DateOnly from,DateOnly to,long? venueId,CancellationToken ct)
    {
        if(!RevenueService.ValidRange(from,to)) throw new ArgumentOutOfRangeException(nameof(from));
        return repository.ReadAsync(from,to,venueId,ct);
    }
}
