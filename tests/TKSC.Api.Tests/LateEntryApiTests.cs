using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using be_da.Application.Abstractions;
using be_da.Application.Sessions;
using be_da.Authorization;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
namespace TKSC.Api.Tests;
public sealed class LateEntryApiTests
{
    [Theory]
    [InlineData(null,HttpStatusCode.Unauthorized)]
    [InlineData("Player",HttpStatusCode.Forbidden)]
    [InlineData("Referee",HttpStatusCode.Forbidden)]
    public async Task OnlyAssignedOperationalRolesCanManageOffers(string? role,HttpStatusCode expected)
    {
        await using var app=new App();using var c=app.CreateClient();
        if(role!=null)c.DefaultRequestHeaders.Add("X-Test-Role",role);
        Assert.Equal(expected,(await c.GetAsync("/api/late-entry/manage")).StatusCode);
        Assert.Null(app.Store.Venue);
    }
    [Fact]
    public async Task ManagerScopeComesFromAuthenticatedAssignmentNotCallerVenueParameter()
    {
        await using var app=new App();using var c=app.CreateClient();c.DefaultRequestHeaders.Add("X-Test-Role","Manager");
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/api/late-entry/manage?venueId=999")).StatusCode);
        Assert.Equal(7,app.Store.Venue);
    }
    [Fact]
    public async Task PublishingWithoutCsrfIsRejectedBeforeAnyStoreWrite()
    {
        await using var app=new App();using var c=app.CreateClient();c.DefaultRequestHeaders.Add("X-Test-Role","Staff");
        var response=await c.PutAsJsonAsync("/api/late-entry/1",new{seats=1,priceVnd=80000,expectedRevision=0});
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);Assert.Equal(0,app.Store.Writes);
    }
    [Fact]
    public async Task PublishingWithCsrfUsesOnlyAssignedVenue()
    {
        await using var app=new App();using var c=app.CreateClient();c.DefaultRequestHeaders.Add("X-Test-Role","Manager");
        var csrf=await c.GetFromJsonAsync<JsonElement>("/api/auth/csrf");c.DefaultRequestHeaders.Add("X-CSRF-TOKEN",csrf.GetProperty("token").GetString());
        var response=await c.PutAsJsonAsync("/api/late-entry/1",new{venueId=999,seats=1,priceVnd=80000,expectedRevision=0});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Equal(7,app.Store.Venue);Assert.Equal(1,app.Store.Writes);
    }
    private sealed class App:WebApplicationFactory<Program>
    {
        public FakeStore Store=new();
        public new HttpClient CreateClient()=>base.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false,HandleCookies=true});
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(s=>
            {
                s.RemoveAll<ILateEntryStore>();s.AddSingleton<ILateEntryStore>(Store);
                s.RemoveAll<ISessionStore>();s.AddSingleton<ISessionStore>(new FakeSessionStore());
                s.RemoveAll<IAccountStore>();s.AddSingleton<IAccountStore>(new FakeSessionAccounts());
                s.AddAuthentication(o=>{o.DefaultAuthenticateScheme="late-test";o.DefaultChallengeScheme="late-test";o.DefaultForbidScheme="late-test";}).AddScheme<AuthenticationSchemeOptions,TestAuth>("late-test",_=>{});
            });
        }
    }
    private sealed class FakeStore:ILateEntryStore
    {
        public long? Venue;public int Writes;
        public Task<IReadOnlyList<LateEntryOffer>> List(long? venue,long? session,bool manage,CancellationToken ct){Venue=venue;return Task.FromResult<IReadOnlyList<LateEntryOffer>>([]);}
        public Task<LateEntryOffer> Publish(long id,long venue,long actor,PublishLateEntryRequest input,bool stop,CancellationToken ct)
        {
            Venue=venue;Writes++;return Task.FromResult(new LateEntryOffer(id,venue,"Cơ sở 7","Sân 1","Badminton","B2",DateTimeOffset.UtcNow.AddHours(-1),DateTimeOffset.UtcNow.AddHours(1),8,4,1,1,2,1,input.PriceVnd,!stop,1));
        }
    }
    private sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role=Request.Headers["X-Test-Role"].ToString();
            if(role.Length==0)return Task.FromResult(AuthenticateResult.NoResult());
            var p=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"99"),new Claim(ClaimTypes.Role,role),new Claim(SportCenterConnection.VenueClaim,"7")],Scheme.Name));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(p,Scheme.Name)));
        }
    }
}
