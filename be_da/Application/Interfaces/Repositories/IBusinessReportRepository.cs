using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Repositories;
public interface IBusinessReportRepository
{
    Task<BusinessReport> ReadAsync(DateOnly from, DateOnly to, long? venueId, CancellationToken ct);
}
