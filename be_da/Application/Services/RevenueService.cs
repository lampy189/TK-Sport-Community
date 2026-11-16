using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class RevenueService(IRevenueRepository repository) : IRevenueService
{
    public static bool ValidRange(DateOnly from, DateOnly to) => from != default && to < DateOnly.MaxValue && to >= from && to.DayNumber - from.DayNumber <= 365;
    public async Task<OperationResult> DailyAsync(DateOnly from, DateOnly to, long? venueId, CancellationToken ct)
    {
        if (!ValidRange(from, to))
            return OperationResult.Invalid(new { message = "Chọn khoảng ngày hợp lệ, tối đa 366 ngày." });
        var snapshot = await repository.ReadAsync(from, to, venueId, ct);
        if (snapshot is null)
            return OperationResult.Missing(new { message = "Không tìm thấy Sport Center." });
        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1).Select(i => from.AddDays(i)).Select(d => snapshot.Rows.GetValueOrDefault(d, new RevenueDay(d, 0, 0, 0))).ToArray();
        return OperationResult.Success(new { venueId, from, to, timeZone = "Asia/Ho_Chi_Minh", days, totals = new { social = days.Sum(d => d.Social), goods = days.Sum(d => d.Goods), refunds = days.Sum(d => d.Refunds), net = days.Sum(d => d.Net) }, unpostedPayments = snapshot.Unposted });
    }
}
