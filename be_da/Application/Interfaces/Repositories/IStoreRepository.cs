using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using be_da.Application.DTOs;

namespace TKSC.Application.Interfaces.Repositories;
public interface IStoreRepository
{
    Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct);
    Task<long[]?> CenterProductIdsAsync(bool scoped, long? venueId, CancellationToken ct);
    Task<List<Product>> FindProductsAsync(List<long> productIds);
    Task<OperationResult?> FindIdempotentCartAsync(long actorId, string idempotencyKey, string fingerprint, CancellationToken ct);
    Task<OperationResult> SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, long actorId, bool scoped, long? venueId, string idempotencyKey, string fingerprint, DateTimeOffset expiresAt, CancellationToken ct);
}
