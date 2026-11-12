namespace be_da.Application.Interfaces.Services;
public interface ITicketEmailService
{
    Task<List<long>> PendingAsync(CancellationToken ct);
    Task DeliverAsync(long id, CancellationToken stoppingToken);
}
