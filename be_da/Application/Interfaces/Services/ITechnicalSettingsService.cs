using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Services;
public interface ITechnicalSettingsService
{
    Task<object> ReadAsync(CancellationToken ct);
    Task<bool> EnabledAsync(string key, CancellationToken ct);
    Task SaveAsync(string key, TechnicalSettingChange request, long actor, CancellationToken ct);
    Task<IReadOnlyList<TechnicalIncident>> IncidentsAsync(CancellationToken ct);
    Task SaveIncidentAsync(long? id, TechnicalIncidentChange request, long actor, CancellationToken ct);
}
