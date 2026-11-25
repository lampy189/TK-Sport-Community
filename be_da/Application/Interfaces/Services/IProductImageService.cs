using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.Services;

public interface IProductImageService
{
    Task<ProductImage?> ReadAsync(long productId, long? buyerId, CancellationToken ct);
    Task<ProductImageResult> UploadAsync(long productId, byte[] bytes, CancellationToken ct);
    Task<ProductImageResult> RestoreAsync(long productId, CancellationToken ct);
}
