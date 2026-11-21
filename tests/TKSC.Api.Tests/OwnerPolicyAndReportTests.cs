using be_da.Application.DTOs;
using be_da.Application.Interfaces.External;
using be_da.Application.Interfaces.Repositories;
using be_da.Application.Services;
using be_da.Infrastructure.Persistence;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;
using TKSC.Infrastructure.Persistence.Contexts;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class OwnerPolicyAndReportTests
{
    private static readonly DateTimeOffset Now=new(2026,11,15,0,0,0,TimeSpan.Zero);
    private static SubmitPolicyRequest Valid => new("TICKET_1","Giá vé","Áp dụng vé mới","Điều chỉnh",Now.AddDays(1),null,1,"session",1,80000);
    [Fact] public void ValidFuturePriceIsAccepted() => Assert.Null(OwnerPolicyService.Validate(Valid,Now));
    [Fact] public void BackdatedVersionIsRejected() => Assert.NotNull(OwnerPolicyService.Validate(Valid with {EffectiveFrom=Now.AddSeconds(-1)},Now));
    [Fact] public void EmptyEffectiveIntervalIsRejected() => Assert.NotNull(OwnerPolicyService.Validate(Valid with {EffectiveTo=Valid.EffectiveFrom},Now));
    [Theory] [InlineData(-1)] [InlineData(100000001)]
    public void InvalidPriceIsRejected(int price) => Assert.NotNull(OwnerPolicyService.Validate(Valid with {PriceVnd=price},Now));
    [Fact] public void TextPolicyCannotSmuggleAPrice() => Assert.NotNull(OwnerPolicyService.Validate(Valid with {Kind="policy"},Now));
    [Fact] public void TextPolicyNeedsNoTarget() => Assert.Null(OwnerPolicyService.Validate(Valid with {Kind="policy",TargetId=null,PriceVnd=null},Now));
    [Fact] public async Task InvalidSubmissionDoesNotReachPersistence()
    {
        var repo=new PolicyStub();var service=new OwnerPolicyService(repo,new FixedClock());
        var result=await service.SubmitAsync(Valid with {VenueId=0},5,default);
        Assert.Equal(OperationOutcome.Invalid,result.Outcome);Assert.False(repo.Called);
    }
    [Fact] public async Task DecisionNeedsAnAuditReason()
    {
        var repo=new PolicyStub();var service=new OwnerPolicyService(repo,new FixedClock());
        Assert.Equal(OperationOutcome.Invalid,(await service.DecideAsync(1,new(true," "),5,default)).Outcome);
        Assert.False(repo.Called);
    }
    [Theory] [InlineData("Staff",1L,2L)] [InlineData("Manager",null,1L)] [InlineData("Player",1L,1L)] [InlineData("SystemAdmin",1L,1L)]
    public async Task ReportRejectsUnauthorizedScopeBeforeOpeningConnection(string role,long? assigned,long requested)
    {
        var repository=new BusinessReportRepository(new ConfigurationBuilder().Build(),new Scope(role,assigned));
        var error=await Assert.ThrowsAsync<PaymentException>(()=>repository.ReadAsync(new(2026,11,1),new(2026,11,15),requested,default));
        Assert.Contains("quyền",error.Message);
    }
    [Fact] public async Task ManagerCannotApproveEvenThroughRepository()
    {
        var repository=new OwnerPolicyRepository(new ConfigurationBuilder().Build(),new Scope("Manager",1));
        Assert.Equal(OperationOutcome.Denied,(await repository.DecideAsync(1,new(true,"OK"),2,default)).Outcome);
        Assert.Equal(OperationOutcome.Denied,(await repository.SubmitAsync(Valid with {VenueId=2},2,default)).Outcome);
    }
    [Fact] public void IssuedSuppliesRemainUnresolvedUntilReconciled()
    {
        Assert.Equal(7,new SupplyReport(1,1,1,10,3,null).Unresolved);
        Assert.Equal(0,new SupplyReport(1,1,1,10,3,7).Unresolved);
    }
    [Fact] public void ChainTotalMatchesSumOfCenters()
    {
        var rows=new[]{new CenterRevenue(1,"A",100,30,20),new CenterRevenue(2,"B",200,40,50)};
        Assert.Equal(300,rows.Sum(x=>x.Net));
    }
    [Fact] public void EffectivePricesTranslateAndSoldPriceColumnsRemainIndependent()
    {
        using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
        var sql=db.Products.Select(p=>new { Price=OwnerPrice.Effective("product",p.Id,p.UnitPriceVnd,Now),Version=OwnerPrice.Version("product",p.Id,Now) }).ToQueryString();
        Assert.Contains("[dbo].[OwnerEffectivePrice]",sql);Assert.Contains("[dbo].[OwnerPriceVersion]",sql);
        var sold=db.OrderItems.Select(i=>new { i.UnitPriceVnd,i.PriceVersionId }).ToQueryString();
        Assert.DoesNotContain("OwnerEffectivePrice",sold);Assert.Contains("price_version_id",sold);
        Assert.DoesNotContain("OwnerEffectivePrice",db.Tickets.Select(t=>t.AmountVnd).ToQueryString());
    }
    private sealed record Scope(string Role,long? VenueId):IDataAccessContext { public long? RestrictedVenueId=>VenueId; }
    private sealed class FixedClock:TimeProvider { public override DateTimeOffset GetUtcNow()=>Now; }
    private sealed class PolicyStub:IOwnerPolicyRepository
    {
        public Task<object> OptionsAsync(CancellationToken ct) => Task.FromResult<object>(new { });
        public bool Called;
        public Task<OperationResult> SubmitAsync(SubmitPolicyRequest r,long a,CancellationToken ct) { Called=true;return Task.FromResult(OperationResult.Success(null)); }
        public Task<OperationResult> DecideAsync(long id,DecidePolicyRequest r,long a,CancellationToken ct) { Called=true;return Task.FromResult(OperationResult.Success(null)); }
        public Task<IReadOnlyList<PolicyVersion>> HistoryAsync(string? code,long? venue,CancellationToken ct)=>Task.FromResult<IReadOnlyList<PolicyVersion>>([]);
    }
}
