using System.Net;
using System.Net.Http.Json;
using be_da.Application.DTOs;
using be_da.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
namespace TKSC.Api.Tests;
public class OwnerPolicySecurityTests
{
 [Theory][InlineData("Owner","")][InlineData("Manager","")][InlineData("Owner","/1/decision")]
 public async Task PolicyWritesNeedAntiforgeryBeforeCallingBusinessService(string role,string path)
 {
  await using var root=new SkillAuthorizationApp();var stub=new Stub();await using var app=root.WithWebHostBuilder(b=>b.ConfigureServices(s=>{s.RemoveAll<IOwnerPolicyService>();s.AddSingleton<IOwnerPolicyService>(stub);}));using var client=app.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost")});client.DefaultRequestHeaders.Add("Test-Role",role);client.DefaultRequestHeaders.Add("X-TKSC-Request","1");
  object body=path.Length==0?new SubmitPolicyRequest("TEST","Test policy","Valid content","Evidence",DateTimeOffset.UtcNow.AddDays(1),null,1):new DecidePolicyRequest(true,"Independent owner decision");
  var response=await client.PostAsJsonAsync("/api/v1/owner-policies"+path,body);Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Contains("invalid_csrf_token",await response.Content.ReadAsStringAsync());Assert.Equal(0,stub.Writes);
 }
 [Theory][InlineData("Staff")][InlineData("Referee")][InlineData("SystemAdmin")]
 public async Task OtherRolesCannotReadOwnerPriceProposals(string role)
 {
  await using var root=new SkillAuthorizationApp();using var client=root.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new("https://localhost")});client.DefaultRequestHeaders.Add("Test-Role",role);Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/v1/owner-policies")).StatusCode);
 }
 private sealed class Stub:IOwnerPolicyService
 {
  public int Writes;
  public Task<object> OptionsAsync(CancellationToken ct)=>Task.FromResult<object>(new{});
  public Task<IReadOnlyList<PolicyVersion>> HistoryAsync(string? code,long? venue,CancellationToken ct)=>Task.FromResult<IReadOnlyList<PolicyVersion>>([]);
  public Task<OperationResult> SubmitAsync(SubmitPolicyRequest r,long actor,CancellationToken ct){Writes++;return Task.FromResult(OperationResult.Completed());}
  public Task<OperationResult> DecideAsync(long id,DecidePolicyRequest r,long actor,CancellationToken ct){Writes++;return Task.FromResult(OperationResult.Completed());}
 }
}
