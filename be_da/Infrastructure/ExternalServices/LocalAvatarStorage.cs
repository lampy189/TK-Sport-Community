using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;

namespace be_da.Infrastructure.ExternalServices;
public sealed class LocalAvatarStorage : IAvatarStorage
{
    private static readonly string AvatarDirectory = Path.Combine(AppContext.BaseDirectory, "App_Data", "avatars");
    public async Task<string?> SaveDataUrlAsync(long userId, string dataUrl, CancellationToken cancellationToken)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex <= 0)
            return null;
        var header = dataUrl[..commaIndex].ToLowerInvariant();
        var base64Data = dataUrl[(commaIndex + 1)..];
        if (!header.Contains(";base64"))
            return null;
        var ext = header.Contains("image/png") ? ".png" : header.Contains("image/webp") ? ".webp" : header.Contains("image/gif") ? ".gif" : ".jpg";
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch
        {
            return null;
        }

        if (bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
            return null;
        Directory.CreateDirectory(AvatarDirectory);
        var fileName = $"u{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
        var fullPath = Path.Combine(AvatarDirectory, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        return $"/api/profile/avatars/{fileName}";
    }

    public AvatarFile? Find(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName))
            return null;
        var fullPath = Path.Combine(AvatarDirectory, safeName);
        if (!System.IO.File.Exists(fullPath))
            return null;
        var ext = Path.GetExtension(safeName).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };
        return new AvatarFile(fullPath, contentType);
    }
}
