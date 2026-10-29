using be_da.Application.Stock;

namespace be_da.Application.Abstractions;

public interface IStockStore
{
    Task<IReadOnlyList<StockProduct>> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken);
    Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken);
    // Atomic: product, its stock item and any opening-balance movement are saved together.
    Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken);
    // Atomic: posts one movement and changes on-hand stock; never goes below zero.
    Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken cancellationToken);
}
