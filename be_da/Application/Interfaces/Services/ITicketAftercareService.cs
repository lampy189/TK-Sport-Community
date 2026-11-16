using TKSC.Domain.Exceptions;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.DTOs.Persistence;
using System.Data;
using TKSC.Application.DTOs;
using TKSC.Application.Services.Notifications;
using TKSC.Domain.Entities;
using TKSC.Application.Services;

namespace be_da.Application.Interfaces.Services;
public interface ITicketAftercareService
{
    Task<object> SessionsAsync(bool issuesOnly = false);
    Task<object> ReportAsync(long id, long actor, string? reason, bool cancel);
    Task<object> OptionsAsync(long id, long user);
    Task<object> RequestChangeAsync(long id, long targetId, long user);
    Task<object> CancelChangeAsync(long id, long user);
    Task<object> RequestRefundAsync(long id, long user);
    Task<object> ChangesAsync();
    Task<object> CompleteChangeAsync(long id, long actor, string? evidence, string? bankReference);
    Task<object> RefundsAsync();
    Task<object> CompleteRefundAsync(long id, long actor, string? evidence, string? reference);
    Task<object> ReviewsAsync();
    Task<object> ApprovePaymentAsync(long id, long bankId, long actor, string? evidence);
}
