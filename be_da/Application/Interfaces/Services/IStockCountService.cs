using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
// Stock count and adjustment approval (RP3 UC-43 / UC-44). An approval module (for example a Manager approval inbox)
// calls ApproveAsync / RejectAsync with the decider's user id; stock is updated once, inside the same call.
public interface IStockCountService
{
    Task<StockCountSheet?> GetSheetAsync(long productId, CancellationToken cancellationToken);
    Task<StockCountResult> CreateAsync(CreateStockCountRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<OperationResult> ListAsync(string? status, long? productId, CancellationToken cancellationToken);
    Task<StockCountView?> GetAsync(long id, CancellationToken cancellationToken);
    Task<StockCountResult> ApproveAsync(long id, long deciderId, StockCountDecisionRequest request, CancellationToken cancellationToken);
    Task<StockCountResult> RejectAsync(long id, long deciderId, StockCountDecisionRequest request, CancellationToken cancellationToken);
    Task<OperationResult> GetSupplyReportAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockReconciliationLine>> GetReconciliationAsync(bool onlyMismatches, CancellationToken cancellationToken);
}
