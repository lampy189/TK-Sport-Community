using be_da.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Interfaces.Repositories;
public interface IPlayerChatData
{
    Task<ChatData> ReadAsync(ChatLookup lookup, long userId, CancellationToken ct);
}
