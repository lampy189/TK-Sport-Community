using System.Net;
using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class ZaloAiPrivacyTests
{
    [Fact]
    public async Task Previous_order_details_and_customer_identity_are_not_sent_to_model()
    {
        var handler=new Capture();using var http=new HttpClient(handler);
        var ai=new OpenAiZaloIntentInterpreter(http,Options.Create(new ChatbotOptions{ApiKey="test-key"}),TimeProvider.System);
        await ai.InterpretAsync("kiểm tra giúp",new(){Name="Private Customer",OrderId=567891234,
            LastReply="Secret-ticket-code https://pay.payos.vn/private-link",Score=50},default);
        Assert.DoesNotContain("Private Customer",handler.Body);
        Assert.DoesNotContain("Secret-ticket-code",handler.Body);
        Assert.DoesNotContain("private-link",handler.Body);
        Assert.DoesNotContain("567891234",handler.Body);
        Assert.DoesNotContain("test-key",handler.Body);
    }
    private sealed class Capture : HttpMessageHandler
    {
        public string Body="";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Body=await request.Content!.ReadAsStringAsync(ct);
            return new(HttpStatusCode.TooManyRequests){Content=new StringContent("{}")};
        }
    }
}
