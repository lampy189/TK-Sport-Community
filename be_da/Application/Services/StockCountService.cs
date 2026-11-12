using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;
using be_da.Application.Validators;

namespace be_da.Application.Services;
public sealed class StockCountService(IStockCountStore repository, TimeProvider clock) : IStockCountService
{
    public const int MaxReportDays = 366;
    public static readonly IReadOnlySet<string> Statuses = new HashSet<string>(StringComparer.Ordinal) { StockCountRules.NoDifference, StockCountRules.Pending, StockCountRules.Applied, StockCountRules.Rejected };

    public Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken cancellationToken) => repository.GetSheetAsync(productId, cancellationToken);
    public Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken cancellationToken) =>
        StockCountRules.ValidateCreate(request) is { } error ? Task.FromResult(StockCountResult.Fail("VALIDATION_FAILED", error)) : repository.CreateAsync(request, staffUserId, cancellationToken);

    public async Task<OperationResult> ListAsync(string? status, long? productId, CancellationToken cancellationToken)
    {
        if (status is not null && !Statuses.Contains(status))
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = "Trạng thái kiểm kê không hợp lệ." });
        if (productId is <= 0)
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = "Mặt hàng không hợp lệ." });
        return OperationResult.Success(await repository.ListAsync(status, productId, cancellationToken));
    }

    public Task<StockCountView?> GetAsync(long id, CancellationToken cancellationToken) => repository.GetAsync(id, cancellationToken);
    public Task<StockCountResult> ApproveAsync(long id, long deciderId, StockCountDecisionRequest request, CancellationToken cancellationToken) =>
        StockCountRules.ValidateApprove(request) is { } error ? Task.FromResult(StockCountResult.Fail("VALIDATION_FAILED", error)) : repository.DecideAsync(id, deciderId, true, request.Note, request.ExpectedDifference, cancellationToken);
    public Task<StockCountResult> RejectAsync(long id, long deciderId, StockCountDecisionRequest request, CancellationToken cancellationToken) =>
        StockCountRules.ValidateReject(request) is { } error ? Task.FromResult(StockCountResult.Fail("VALIDATION_FAILED", error)) : repository.DecideAsync(id, deciderId, false, request.Note, null, cancellationToken);

    // Defaults to the current month (Vietnam time).
    public async Task<OperationResult> GetSupplyReportAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
        var end = to ?? today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);
        if (start > end)
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = "Ngày bắt đầu phải trước ngày kết thúc." });
        if (end.DayNumber - start.DayNumber > MaxReportDays)
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = "Khoảng báo cáo tối đa một năm." });
        return OperationResult.Success(await repository.GetSupplyReportAsync(start, end, cancellationToken));
    }

    public async Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(bool onlyMismatches, CancellationToken cancellationToken)
    {
        var lines = await repository.GetReconciliationAsync(cancellationToken);
        return onlyMismatches ? lines.Where(l => !l.OnHandMatches || !l.ReservedMatches).ToList() : lines;
    }
}
