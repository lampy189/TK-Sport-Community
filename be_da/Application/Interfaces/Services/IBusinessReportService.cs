using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Services;
public interface IBusinessReportService
{
    Task<BusinessReport> ReadAsync(DateOnly from,DateOnly to,long? venueId,CancellationToken ct);
}
