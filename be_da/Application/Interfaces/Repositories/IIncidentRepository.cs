using TKSC.Application.DTOs;
using TKSC.Domain.Entities;

namespace TKSC.Application.Interfaces.Repositories;
public interface IIncidentRepository
{
    Task<T> Transaction<T>(Func<Task<T>> action);
    Task<IncidentActor> Actor(long id);
    Task<IReadOnlyList<IncidentActor>> People(long venueId);
    Task<IncidentResource?> Resource(string kind, long id);
    Task<IncidentState> State(string kind, long id);
    Task<IncidentState> SaveState(string kind, long id, long actor, IncidentState state);
    Task<IReadOnlyList<(string Kind, long Id)>> Keys();
    Task<IReadOnlyList<AuditLog>> History(string type, long id);
    Task<OperationalCase> CreateCase(OperationalCase item);
    Task<OperationalCase?> Case(long id);
    Task<IReadOnlyList<OperationalCase>> Cases();
    Task<OperationalCase> SaveCase(OperationalCase item, long actor, string action);
    Task<IReadOnlyList<PaymentRecoveryItem>> RecoveryPayments();
    Task<Payment?> RecoveryPayment(long id);
    Task RecordRecovery(long id, long actor, string action, string evidence, string result);
    Task CloseUnpaid(long id);
    Task RecordReturnPayout(long caseId, long actor, RefundPayoutInput input);
    Task<RefundPayoutRecord?> ReturnPayout(long caseId);
    Task CompleteReturn(OperationalCase item, long actor);
    Task ExecuteStock(OperationalCase item, long actor);
    Task AddApproval(Approval approval);
}
