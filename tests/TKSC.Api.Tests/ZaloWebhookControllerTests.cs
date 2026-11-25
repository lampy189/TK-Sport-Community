using System.Security.Cryptography;
using System.Text;
using be_da.Application.DTOs;
using be_da.Infrastructure.ExternalServices;
using be_da.Infrastructure.Persistence.Repositories;
using be_da.Infrastructure.Services;
using be_da.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class ZaloWebhookControllerTests
{
    [Theory]
    [InlineData(-90000)]
    [InlineData(600)]
    public async Task Signed_but_stale_or_future_event_never_reaches_database(int offsetSeconds)
    {
        var timestamp=DateTimeOffset.UtcNow.AddSeconds(offsetSeconds).ToUnixTimeMilliseconds().ToString();
        var body="{\"app_id\":\"app\",\"recipient\":{\"id\":\"oa\"},\"sender\":{\"id\":\"user\"},\"message\":{\"text\":\"hello\",\"msg_id\":\"1\"},\"event_name\":\"user_send_text\",\"timestamp\":\""+timestamp+"\"}";
        var options=Options.Create(new ZaloOptions{Enabled=true,AppId="app",OaId="oa",OaSecretKey="secret"});
        var controller=new ZaloWebhookController(new ZaloWebhookService(new(options),new SqlZaloStore(new ConfigurationBuilder().Build()),options,TimeProvider.System));
        var context=new DefaultHttpContext();context.Request.Body=new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["X-ZEvent-Signature"]="mac="+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("app"+body+timestamp+"secret")));
        controller.ControllerContext=new ControllerContext{HttpContext=context};
        Assert.IsType<BadRequestObjectResult>(await controller.Receive(default));
    }
    [Fact]
    public async Task Disabled_webhook_does_not_acknowledge_and_lose_customer_messages()
    {
        var options=Options.Create(new ZaloOptions());
        var controller=new ZaloWebhookController(new ZaloWebhookService(new(options),new SqlZaloStore(new ConfigurationBuilder().Build()),options,TimeProvider.System));
        controller.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext()};
        Assert.Equal(503,Assert.IsType<StatusCodeResult>(await controller.Receive(default)).StatusCode);
    }
}
