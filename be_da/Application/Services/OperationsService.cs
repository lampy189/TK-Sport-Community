using be_da.Application.DTOs;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Interfaces.Services;
namespace be_da.Application.Services;
public sealed class OperationsService(IOperationsRepository repository) : IOperationsService
{
    private static OperationResult Invalid(string message)=>OperationResult.Invalid(new { message });
    public Task<object> SetupAsync(CancellationToken ct)=>repository.SetupAsync(ct);
    public Task<OperationResult> SaveVenueAsync(long actor,long? id,VenueEdit r,CancellationToken ct)=>
        string.IsNullOrWhiteSpace(r.Name)||r.Name.Trim().Length>255||(r.Address?.Length??0)>500 ? Task.FromResult(Invalid("Nhập tên cơ sở tối đa 255 ký tự, địa chỉ tối đa 500 ký tự.")) : repository.SaveVenueAsync(actor,id,r with { Name=r.Name.Trim(),Address=r.Address?.Trim() },ct);
    public Task<OperationResult> SaveCourtAsync(long actor,long? id,CourtEdit r,CancellationToken ct)=>
        string.IsNullOrWhiteSpace(r.Name)||r.Name.Trim().Length>255||r.VenueId<=0||r.SportId<=0 ? Task.FromResult(Invalid("Chọn cơ sở, môn chơi và tên sân hợp lệ.")) : repository.SaveCourtAsync(actor,id,r with { Name=r.Name.Trim() },ct);
    public Task<OperationResult> PersonnelAsync(long actor,long id,PersonnelEdit r,CancellationToken ct)=>r.VenueId is <=0||r.ReplacementUserId==id ? Task.FromResult(Invalid("Phân công hoặc người thay thế không hợp lệ.")) : repository.PersonnelAsync(actor,id,r,ct);
    public Task<object> AccessAsync(long actor,string role,CancellationToken ct)=>repository.AccessAsync(actor,role,ct);
    public Task<OperationResult> ProposeAsync(long actor,string role,AccessProposal r,CancellationToken ct)=>
        r.TargetUserId<=0||string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Length>2000||r.Role is not ("Staff" or "Referee" or "Manager" or "SystemAdmin")||r.Action is not ("grant" or "revoke" or "handover")||r.Action=="handover"&&(r.Role!="SystemAdmin"||r.OutgoingUserId is null||r.OutgoingUserId==r.TargetUserId)
        ? Task.FromResult(Invalid("Nhập tài khoản, vai trò, hình thức và lý do hợp lệ.")) : repository.ProposeAsync(actor,role,r,ct);
    public Task<OperationResult> DecideAsync(long actor,string role,long id,ApprovalDecision r,CancellationToken ct)=>string.IsNullOrWhiteSpace(r.Reason)||r.Reason.Length>2000?Task.FromResult(Invalid("Nhập lý do quyết định tối đa 2000 ký tự.")):repository.DecideAsync(actor,role,id,r,ct);
    public Task<OperationResult> ExecuteAsync(long actor,long id,CancellationToken ct)=>repository.ExecuteAsync(actor,id,ct);
    public Task<OperationResult> VerifyAsync(long actor,long id,CancellationToken ct)=>repository.VerifyAsync(actor,id,ct);
    public Task<object> OverviewAsync(long actor,CancellationToken ct)=>repository.OverviewAsync(actor,ct);
    public Task<OperationResult> FeedbackAsync(long actor,TrialFeedback r,CancellationToken ct)=>string.IsNullOrWhiteSpace(r.Content)||r.Content.Length>2000?Task.FromResult(Invalid("Nhập phản hồi tối đa 2000 ký tự.")):repository.FeedbackAsync(actor,r with { Content=r.Content.Trim() },ct);
}
