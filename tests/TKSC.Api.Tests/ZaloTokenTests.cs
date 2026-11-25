using System.Net;
using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class ZaloTokenTests
{
    [Fact]
    public async Task Rotated_tokens_survive_provider_restart_and_are_not_plaintext()
    {
        var file=Path.Combine(Path.GetTempPath(),"zalo-tokens-test-"+Guid.NewGuid().ToString("N"));
        var options=Options.Create(new ZaloOptions{Enabled=true,AppId="app",OaId="oa",AppSecret="test-secret",RefreshToken="initial-refresh",TokenFile=file});
        var handler=new TokenHandler();var factory=new Factory(handler);
        var protection=new EphemeralDataProtectionProvider();
        try
        {
            var provider=new ZaloTokenProvider(factory,protection,options);
            var tokens=await Task.WhenAll(provider.GetAsync(default),provider.GetAsync(default));
            Assert.All(tokens,x=>Assert.Equal("rotated-access",x));Assert.Equal(1,handler.Calls);
            var stored=await File.ReadAllTextAsync(file);
            Assert.DoesNotContain("rotated-access",stored);Assert.DoesNotContain("rotated-refresh",stored);
            Assert.Equal("rotated-access",await new ZaloTokenProvider(factory,protection,options).GetAsync(default));
            Assert.Equal(1,handler.Calls);
        }
        finally {File.Delete(file);File.Delete(file+".lock");File.Delete(file+".tmp");}
    }
    private sealed class Factory(TokenHandler handler):IHttpClientFactory
    {public HttpClient CreateClient(string name)=>new(handler,false);}
    private sealed class TokenHandler:HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Calls++;
            Assert.Equal("https://oauth.zaloapp.com/v4/oa/access_token",request.RequestUri!.AbsoluteUri);
            Assert.Equal("test-secret",request.Headers.GetValues("secret_key").Single());
            Assert.Contains("refresh_token=initial-refresh",await request.Content!.ReadAsStringAsync(ct));
            return new(HttpStatusCode.OK){Content=new StringContent("{\"access_token\":\"rotated-access\",\"refresh_token\":\"rotated-refresh\",\"expires_in\":\"90000\"}")};
        }
    }
}
