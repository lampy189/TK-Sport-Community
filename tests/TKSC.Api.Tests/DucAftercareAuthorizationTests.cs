using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
namespace TKSC.Api.Tests;
public sealed class DucAftercareAuthorizationTests
{
 [Theory]
 [InlineData("Staff")] [InlineData("Manager")] [InlineData("Referee")] [InlineData("Owner")]
 public async Task OnlyPlayersCanSubmitIncidentReports(string role)
 {
  await using var app=new SkillAuthorizationApp();
  using var client=app.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
  client.DefaultRequestHeaders.Add("Test-Role",role);
  var response=await client.PostAsJsonAsync("/api/ticket-aftercare/tickets/41/reports",new{category="court",content="Sân không thể sử dụng"});
  Assert.Equal(HttpStatusCode.Forbidden,response.StatusCode);
 }
 [Theory]
 [InlineData("court")] [InlineData("payment")] [InlineData("refund")]
 public async Task PlayerReportRequiresCsrfRegardlessOfCategory(string category)
 {
  await using var app=new SkillAuthorizationApp();
  using var client=app.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
  client.DefaultRequestHeaders.Add("Test-Role","Player");
  var response=await client.PostAsJsonAsync("/api/ticket-aftercare/tickets/41/reports",new{category,content="Mô tả sự cố của người chơi"});
  Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
 }
 [Theory]
 [InlineData("Player")] [InlineData("Referee")] [InlineData("Owner")]
 public async Task OtherRolesCannotReadCentralVenueIncidents(string role)
 {
  await using var app=new SkillAuthorizationApp();
  using var client=app.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
  client.DefaultRequestHeaders.Add("Test-Role",role);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/ticket-aftercare/incidents")).StatusCode);
 }
 [Theory]
 [InlineData("Player")] [InlineData("Referee")] [InlineData("Owner")]
 public async Task OtherRolesCannotReadStaffInbox(string role)
 {
  await using var app=new SkillAuthorizationApp();
  using var client=app.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost")});
  client.DefaultRequestHeaders.Add("Test-Role",role);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/ticket-aftercare/inbox")).StatusCode);
 }
}
