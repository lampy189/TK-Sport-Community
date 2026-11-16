using be_da.Application.DTOs;
using be_da.Application.Validators;

namespace be_da.Application.Interfaces.Services;
public interface IStockService
{
    IEnumerable<string> GetCategories();
    Task<OperationResult> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken);
    Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken);
    Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken cancellationToken);
    Task<OperationResult> LookupCodeAsync(string? code, CancellationToken cancellationToken);
    Task<BatchReceiptResult> PostBatchReceiptAsync(BatchReceiptRequest request, long staffUserId, CancellationToken cancellationToken);
}
