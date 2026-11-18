using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface ITicketSupportService
{
    Task<object> Inbox();
    Task<object> List(long id, long actor, bool staff);
    Task<object> Report(long id, long actor, string? category, string? content, string? image);
    Task<object> Send(long id, long actor, bool staff, string? content, string? image);
    Task<(byte[] Bytes, string Type)> Image(long id, long messageId, long actor, bool staff);
}
