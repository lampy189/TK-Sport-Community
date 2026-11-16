using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;
public interface IStoreService
{
    Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct);
    Task<OperationResult> CreateCartAsync(CreateCartRequest request, long userId, bool isPlayer, string? customerName, bool scoped, long? venueId, string? idempotencyKey, CancellationToken ct);
}
