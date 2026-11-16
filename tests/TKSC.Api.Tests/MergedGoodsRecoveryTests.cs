using System.Reflection;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Services;
using Xunit;
namespace TKSC.Api.Tests;
public class MergedGoodsRecoveryTests
{
 [Theory][InlineData("recheck")][InlineData("approve")][InlineData("close-unpaid")]
 public async Task SharedRecoveryPageKeepsTheTeamGoodsWorkflow(string action)
 {
  var store=new IncidentWorkflowTests.MemoryIncidentStore {Current=new("in_progress",1,"Handler",Version:1)};
  var recovery=DispatchProxy.Create<IPaymentRecoveryService,GoodsProxy>();var proxy=(GoodsProxy)(object)recovery;
  var service=new TicketRecoveryService(store,new TicketRecoveryTests.Gateway(),null!,null!,recovery);
  await service.Act(1,41,action,new("Bank evidence has been verified",11));
  Assert.Equal(action,proxy.Action);Assert.Equal(41,proxy.PaymentId);
 }
 public class GoodsProxy:DispatchProxy
 {
  public string? Action;public long PaymentId;
  protected override object? Invoke(MethodInfo? method,object?[]? args){PaymentId=(long)args![0]!;Action=(string)args[2]!;return Task.FromResult<object>(new {status="review_required"});}
 }
}
