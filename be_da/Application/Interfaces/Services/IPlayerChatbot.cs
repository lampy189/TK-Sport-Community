using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using Microsoft.Extensions.Options;
using be_da.Application.Services.Chatbot;

namespace be_da.Application.Interfaces.Services;
public interface IPlayerChatbot
{
    Task<ChatAnswer> AskAsync(ChatQuestion request, long userId, CancellationToken ct);
}
