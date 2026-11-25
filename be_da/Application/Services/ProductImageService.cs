using System.Buffers.Binary;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;

namespace be_da.Application.Services;

public sealed class ProductImageService(IProductImageStore images) : IProductImageService
{
    public Task<ProductImage?> ReadAsync(long productId, long? buyerId, CancellationToken ct)
        => images.ReadAsync(productId, buyerId, ct);

    public Task<ProductImageResult> UploadAsync(long productId, byte[] bytes, CancellationToken ct)
    {
        var contentType = DetectContentType(bytes);
        return contentType is null
            ? Task.FromResult(ProductImageResult.Invalid)
            : images.SetAsync(productId, new ProductImage(bytes, contentType), ct);
    }

    public Task<ProductImageResult> RestoreAsync(long productId, CancellationToken ct)
        => images.SetAsync(productId, null, ct);

    private static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 16 && bytes[..3].SequenceEqual(new byte[] { 0xFF, 0xD8, 0xFF }) &&
            bytes[^2..].SequenceEqual(new byte[] { 0xFF, 0xD9 }))
            return "image/jpeg";
        if (bytes.Length >= 45 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            bytes[12..16].SequenceEqual("IHDR"u8) && bytes[^8..^4].SequenceEqual("IEND"u8))
            return "image/png";
        if (bytes.Length >= 24 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8) &&
            BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..8]) == bytes.Length - 8 &&
            (bytes[12..16].SequenceEqual("VP8 "u8) || bytes[12..16].SequenceEqual("VP8L"u8) || bytes[12..16].SequenceEqual("VP8X"u8)))
            return "image/webp";
        return null;
    }
}
