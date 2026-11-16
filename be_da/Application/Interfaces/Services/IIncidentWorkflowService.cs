using TKSC.Application.DTOs;

namespace TKSC.Application.Interfaces.Services;
public interface IIncidentWorkflowService
{
    Task<object> States(long actor);
    Task<object> Detail(long actor, string kind, long id);
    Task<object> Act(long actor, string kind, long id, IncidentAction input);
    Task<object> Cases(long actor);
    Task<object> Request(long actor, OperationalRequest input);
    Task<object> Decide(long actor, long id, OperationalDecision input);
    Task<object> RecordReturn(long actor, long id, RefundPayoutInput input);
    Task<object> VerifyReturn(long actor, long id, string reason);
    Task<object> Execute(long actor, long id, string reason);
    Task<object> Close(long actor, long id, string reason);
}

public interface ITicketRecoveryService
{
    Task<object> List(long actor);
    Task<object> Act(long actor, long id, string action, PaymentRecoveryInput input);
}
