using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Repositories;

public interface IProductImageStore
{
    Task<ProductImage?> ReadAsync(long productId, long? buyerId, CancellationToken ct);
    Task<ProductImageResult> SetAsync(long productId, ProductImage? image, CancellationToken ct);
}
