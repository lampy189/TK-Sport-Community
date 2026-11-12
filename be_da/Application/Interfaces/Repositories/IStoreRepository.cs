using TKSC.Domain.Exceptions;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Interfaces.Repositories;
public interface IStoreRepository
{
    Task<IReadOnlyList<StoreProductItem>> ProductsAsync(bool scoped, long? venueId, CancellationToken ct);
    Task<long[]?> CenterProductIdsAsync(bool scoped, long? venueId, CancellationToken ct);
    Task<List<Product>> FindProductsAsync(List<long> productIds);
    // Saves the cart and reserves every line in one transaction. Returns the id of a product that is no longer
    // available (nothing is saved), or null when the order was saved with its stock held.
    Task<long?> SaveCartAsync(Order order, List<StoreCartItemResult> orderItems, List<Product> products, bool scoped, long? venueId);
}
