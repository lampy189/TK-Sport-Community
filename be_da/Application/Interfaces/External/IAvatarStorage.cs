using be_da.Application.DTOs;

namespace be_da.Application.Interfaces.External;
public interface IAvatarStorage
{
    Task<string?> SaveDataUrlAsync(long userId, string dataUrl, CancellationToken cancellationToken);
    AvatarFile? Find(string fileName);
}
