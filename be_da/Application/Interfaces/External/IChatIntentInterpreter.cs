using be_da.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace be_da.Application.Interfaces.External;
public interface IChatIntentInterpreter
{
    Task<ChatIntent?> InterpretAsync(ChatQuestion request, DateTimeOffset now, CancellationToken ct);
}
