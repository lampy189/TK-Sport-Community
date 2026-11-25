using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class ZaloLiveAiFactAttribute : FactAttribute
{
    public ZaloLiveAiFactAttribute()
    {if(Environment.GetEnvironmentVariable("TKSC_ZALO_LIVE_AI")!="1")Skip="Opt-in synthetic prompts to configured OpenAI API; no SQL data sent.";}
}
public sealed class ZaloLiveAiTests
{
    [ZaloLiveAiFact]
    public async Task Understands_informal_search_and_self_declaration()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root is not null&&!Directory.Exists(Path.Combine(root.FullName,"be_da")))root=root.Parent;
        var config=new ConfigurationBuilder().SetBasePath(Path.Combine(root!.FullName,"be_da"))
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",true)
            .AddUserSecrets<Program>(true).AddJsonFile("appsettings.Local.json",true).AddEnvironmentVariables().Build();
        var options=config.GetSection("Chatbot").Get<ChatbotOptions>()!;
        Assert.False(string.IsNullOrWhiteSpace(options.ApiKey),"No configured AI key.");
        using var http=new HttpClient(new DiagnosticHandler()){Timeout=TimeSpan.FromSeconds(60)};
        var ai=new OpenAiZaloIntentInterpreter(http,Options.Create(options),TimeProvider.System);
        var search=await ai.InterpretAsync("slot cầu lông hôm nay còn ko",new(),default);
        Assert.NotNull(search);Assert.Equal("search",search.Action);Assert.Equal("badminton",search.Sport);
        var profile=await ai.InterpretAsync("Mình tên An, mình tự chấm 50 trên 100 điểm nhé",new(){LastReply="Bạn tên gì và tự chấm bao nhiêu trên thang 0–100?"},default);
        Assert.NotNull(profile);Assert.Equal("profile",profile.Action);Assert.Equal(50,profile.Score);Assert.Contains("An",profile.Name!);
    }
    private sealed class DiagnosticHandler : DelegatingHandler
    {
        public DiagnosticHandler():base(new HttpClientHandler()){}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            var response=await base.SendAsync(request,ct);
            if(!response.IsSuccessStatusCode)
            {
                using var json=System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var code=json.RootElement.TryGetProperty("error",out var error)&&error.TryGetProperty("code",out var value) ? value.ToString():"provider_error";
                var safe=code is "invalid_api_key" or "insufficient_quota" or "model_not_found" or "rate_limit_exceeded" ? code:"provider_error";
                Assert.Fail($"OpenAI HTTP {(int)response.StatusCode}; code={safe}. Response body and credentials intentionally omitted.");
            }
            return response;
        }
    }
}
