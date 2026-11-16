using be_da.Application.DTOs;
using be_da.Application.Validators;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;
public sealed class StockService(IStockStore repository) : IStockService
{
    public IEnumerable<string> GetCategories() => StockRules.Categories.Order();
    public async Task<OperationResult> GetProductsAsync(StockProductFilter filter, CancellationToken cancellationToken)
    {
        if (filter.Category is not null && !StockRules.Categories.Contains(filter.Category))
            return OperationResult.Invalid(new { code = "INVALID_CATEGORY", message = "Nhóm hàng không hợp lệ." });
        if (StockRules.ValidateSearch(filter.Search)is { } error)
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = error });
        return OperationResult.Success(await repository.GetProductsAsync(filter, cancellationToken));
    }

    public Task<StockProduct?> GetProductAsync(long productId, CancellationToken cancellationToken) => repository.GetProductAsync(productId, cancellationToken);
    public Task<StockResult> CreateProductAsync(CreateProductRequest request, long staffUserId, CancellationToken cancellationToken) => StockRules.ValidateCreate(request)is { } error ? Task.FromResult(StockResult.Fail("VALIDATION_FAILED", error)) : repository.CreateProductAsync(request, staffUserId, cancellationToken);
    public Task<StockResult> UpdateProductAsync(long productId, UpdateProductRequest request, CancellationToken cancellationToken) => (StockRules.ValidateDetails(request.Name, request.Category, request.UnitPriceVnd, request.ReorderLevel) ?? (request.Units is null ? null : StockUnitRules.ValidateUnits(request.Units)))is { } error ? Task.FromResult(StockResult.Fail("VALIDATION_FAILED", error)) : repository.UpdateProductAsync(productId, request, cancellationToken);
    public Task<StockResult> RecordMovementAsync(long productId, StockMovementRequest request, long staffUserId, CancellationToken cancellationToken) => StockRules.ValidateMovement(request)is { } error ? Task.FromResult(StockResult.Fail("VALIDATION_FAILED", error)) : repository.RecordMovementAsync(productId, request, staffUserId, cancellationToken);
    public Task<IReadOnlyList<StockMovementEntry>?> GetMovementsAsync(long productId, CancellationToken cancellationToken) => repository.GetMovementsAsync(productId, cancellationToken);
    public async Task<OperationResult> LookupCodeAsync(string? code, CancellationToken cancellationToken)
    {
        if (!StockUnitRules.IsValidCode(code))
            return OperationResult.Invalid(new { code = "VALIDATION_FAILED", message = "Mã vạch hoặc mã hàng không hợp lệ." });
        var found = await repository.LookupCodeAsync(StockUnitRules.NormalizeBarcode(code)!, cancellationToken);
        return found is null ? OperationResult.Missing(new { code = "CODE_NOT_FOUND", message = "Không tìm thấy mặt hàng có mã này." }) : OperationResult.Success(found);
    }

    public Task<BatchReceiptResult> PostBatchReceiptAsync(BatchReceiptRequest request, long staffUserId, CancellationToken cancellationToken) => StockReceiptRules.ValidateBatch(request)is { } error ? Task.FromResult(BatchReceiptResult.Fail("VALIDATION_FAILED", error)) : repository.PostBatchReceiptAsync(request, staffUserId, cancellationToken);
}
