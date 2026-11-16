using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
namespace be_da.Application.Services;

public sealed class OwnerPolicyService(IOwnerPolicyRepository repository, TimeProvider clock) : be_da.Application.Interfaces.Services.IOwnerPolicyService
{
    public static string? Validate(SubmitPolicyRequest r, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(r.Code) || r.Code.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(r.Code, "^[A-Za-z0-9_-]+$")
            || string.IsNullOrWhiteSpace(r.Name) || r.Name.Length > 255 || string.IsNullOrWhiteSpace(r.Value) || r.Value.Length > 20000
            || string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length > 1000 || r.VenueId <= 0)
            return "Nhập mã, tên, nội dung, cơ sở và lý do hợp lệ.";
        if (r.EffectiveFrom < now || r.EffectiveTo <= r.EffectiveFrom)
            return "Ngày hiệu lực phải từ hiện tại; ngày kết thúc phải sau ngày bắt đầu.";
        if (r.Kind is not ("policy" or "product" or "session")
            || (r.Kind == "policy" ? r.TargetId is not null || r.PriceVnd is not null : r.TargetId is null or <= 0 || r.PriceVnd is null or < 0 or > 100000000))
            return "Chọn loại chính sách, mặt hàng/buổi chơi và giá hợp lệ.";
        return null;
    }
    public Task<OperationResult> SubmitAsync(SubmitPolicyRequest r, long actor, CancellationToken ct)
        => Validate(r, clock.GetUtcNow()) is { } error ? Task.FromResult(OperationResult.Invalid(new { message = error })) : repository.SubmitAsync(r, actor, ct);
    public Task<OperationResult> DecideAsync(long id, DecidePolicyRequest r, long actor, CancellationToken ct)
        => string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length > 1000
            ? Task.FromResult(OperationResult.Invalid(new { message = "Nhập lý do quyết định, tối đa 1000 ký tự." })) : repository.DecideAsync(id, r, actor, ct);
    public Task<IReadOnlyList<PolicyVersion>> HistoryAsync(string? code, long? venueId, CancellationToken ct) => repository.HistoryAsync(code, venueId, ct);
}
