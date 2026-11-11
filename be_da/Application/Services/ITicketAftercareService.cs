namespace TKSC.Application.Services;

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

public interface ITicketSupportService
{
    Task<object> Inbox();
    Task<object> List(long id, long actor, bool staff);
    Task<object> Report(long id, long actor, string? category, string? content, string? image);
    Task<object> Send(long id, long actor, bool staff, string? content, string? image);
    Task<(byte[] Bytes, string Type)> Image(long id, long messageId, long actor, bool staff);
}
