using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Repositories;
public interface ITechnicalStore
{
    Task<IReadOnlyList<TechnicalSetting>> SettingsAsync(CancellationToken ct);
    Task SaveSettingAsync(string key, TechnicalSettingChange request, long actor, CancellationToken ct);
    Task<object> QueuesAsync(CancellationToken ct);
    Task<IReadOnlyList<TechnicalIncident>> IncidentsAsync(CancellationToken ct);
    Task SaveIncidentAsync(long? id, TechnicalIncidentChange request, long actor, CancellationToken ct);
}
