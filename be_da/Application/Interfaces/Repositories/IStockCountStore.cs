using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Repositories;
// RP3 UC-43 / UC-44 and stock figures for reports. Counting never changes stock; DecideAsync applies an approved
// adjustment exactly once and returns the stored result on a retry.
public interface IStockCountStore
{
    Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken cancellationToken);
    Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockCountView>> ListAsync(string? status, long? productId, CancellationToken cancellationToken);
    Task<StockCountView?> GetAsync(long id, CancellationToken cancellationToken);
    Task<StockCountResult> DecideAsync(long id, long deciderId, bool approve, string? note, int? expectedDifference, CancellationToken cancellationToken);
    Task<SupplyReport> GetSupplyReportAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(CancellationToken cancellationToken);
}
