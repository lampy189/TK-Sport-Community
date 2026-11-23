using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using TKSC.Domain.Exceptions;
namespace be_da.Application.Services;
public sealed class TechnicalSettingsService(ITechnicalStore store, IConfiguration config) : be_da.Application.Interfaces.Services.ITechnicalSettingsService
{
    public static async Task<bool> RuntimeEnabledAsync(IServiceScopeFactory scopes, string key, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        try { return await scope.ServiceProvider.GetRequiredService<be_da.Application.Interfaces.Services.ITechnicalSettingsService>().EnabledAsync(key, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) {
            scope.ServiceProvider.GetRequiredService<ILogger<TechnicalSettingsService>>().LogWarning("Cannot read technical setting {Key} ({ErrorType}); delivery/processing paused.", key, ex.GetType().Name);
            return false;
        }
    }
    public static readonly string[] AllowedKeys = ["Email:Enabled", "Firebase:Enabled", "GoodsReservations:ExpiryWorkerEnabled"];
    public static string? Validate(string key, TechnicalSettingChange request) => !AllowedKeys.Contains(key) ? "Thiết lập không được phép." : string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 1000 || request.Revision < 0 ? "Nhập lý do tối đa 1000 ký tự và phiên bản hợp lệ." : null;
    public async Task<bool> EnabledAsync(string key, CancellationToken ct)
    {
        if (!AllowedKeys.Contains(key)) throw new ArgumentException("Unsupported setting", nameof(key));
        var saved = (await store.SettingsAsync(ct)).FirstOrDefault(x => x.Key == key);
        return saved?.Enabled ?? config.GetValue<bool>(key);
    }
    public async Task<object> ReadAsync(CancellationToken ct)
    {
        var saved = await store.SettingsAsync(ct);
        return new { settings = AllowedKeys.Select(key => saved.FirstOrDefault(s => s.Key == key) ?? new TechnicalSetting(key, config.GetValue<bool>(key), 0)), queues = await store.QueuesAsync(ct), emailReady = !string.IsNullOrWhiteSpace(config["Email:Password"]), firebaseReady = !string.IsNullOrWhiteSpace(config["Firebase:ProjectId"]), payosReady = !string.IsNullOrWhiteSpace(config["PayOS:ClientId"]) && !string.IsNullOrWhiteSpace(config["PayOS:ApiKey"]) && !string.IsNullOrWhiteSpace(config["PayOS:ChecksumKey"]) };
    }
    public Task SaveAsync(string key, TechnicalSettingChange request, long actor, CancellationToken ct)
    {
        if (Validate(key, request) is { } error) throw new PaymentException(400, error);
        if (request.Enabled && key == "Email:Enabled" && string.IsNullOrWhiteSpace(config["Email:Password"])) throw new PaymentException(409, "Cần cấu hình dịch vụ email trên máy chủ trước khi bật.");
        if (request.Enabled && key == "Firebase:Enabled" && string.IsNullOrWhiteSpace(config["Firebase:ProjectId"])) throw new PaymentException(409, "Cần cấu hình Firebase trên máy chủ trước khi bật.");
        return store.SaveSettingAsync(key, request, actor, ct);
    }
    public Task<IReadOnlyList<TechnicalIncident>> IncidentsAsync(CancellationToken ct) => store.IncidentsAsync(ct);
    public Task SaveIncidentAsync(long? id, TechnicalIncidentChange request, long actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 || string.IsNullOrWhiteSpace(request.Note) || request.Note.Length > 2000 || request.Status is not ("open" or "in_progress" or "resolved") || request.Revision < 0) throw new PaymentException(400, "Nhập tiêu đề, nội dung và trạng thái sự cố hợp lệ.");
        return store.SaveIncidentAsync(id, request with { Title = request.Title.Trim(), Note = request.Note.Trim() }, actor, ct);
    }
}
