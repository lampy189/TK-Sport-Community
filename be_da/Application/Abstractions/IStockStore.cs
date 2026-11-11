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
<<<<<<< HEAD
=======
    // Finds a barcode or SKU; a SKU answers with the base unit.
    Task<StockCodeLookup?> LookupCodeAsync(string code, CancellationToken cancellationToken);
    // All lines post together or none; repeating the same source reference does not add stock again.
    Task<BatchReceiptResult> PostBatchReceiptAsync(BatchReceiptRequest request, long staffUserId, CancellationToken cancellationToken);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
}
