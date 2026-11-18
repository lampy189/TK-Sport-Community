using be_da.Application.DTOs;
namespace be_da.Application.Interfaces.Services;
public interface IOperationsService
{
    Task<object> SetupAsync(CancellationToken ct);
    Task<OperationResult> SaveVenueAsync(long actor,long? id,VenueEdit request,CancellationToken ct);
    Task<OperationResult> SaveCourtAsync(long actor,long? id,CourtEdit request,CancellationToken ct);
    Task<OperationResult> PersonnelAsync(long actor,long id,PersonnelEdit request,CancellationToken ct);
    Task<object> AccessAsync(long actor,string role,CancellationToken ct);
    Task<OperationResult> ProposeAsync(long actor,string role,AccessProposal request,CancellationToken ct);
    Task<OperationResult> DecideAsync(long actor,string role,long id,ApprovalDecision request,CancellationToken ct);
    Task<OperationResult> ExecuteAsync(long actor,long id,CancellationToken ct);
    Task<OperationResult> VerifyAsync(long actor,long id,CancellationToken ct);
    Task<object> OverviewAsync(long actor,CancellationToken ct);
    Task<OperationResult> FeedbackAsync(long actor,TrialFeedback request,CancellationToken ct);
}
