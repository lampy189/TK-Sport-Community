using System.Net;
using System.Text;
using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Infrastructure.Services;
using be_da.Infrastructure.ExternalServices;
using be_da.Infrastructure.Persistence.Repositories;
using be_da.Presentation.BackgroundServices;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TKSC.Application.DTOs;
using TKSC.Application.Interfaces.External;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Services;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.Persistence.Repositories;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ZaloDatabaseFactAttribute : FactAttribute
{
    public ZaloDatabaseFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TKSC_ZALO_TEST_SQL_CONNECTION")))
            Skip="Requires disposable database TKSC_ZaloTest_*; never use application database.";
    }
}

public sealed class ZaloBookingIntegrationTests
{
    [ZaloDatabaseFact]
    public async Task ConversationBooksOnceSendsQrAndConfirmsOnlyVerifiedPayment()
    {
        var connection=Environment.GetEnvironmentVariable("TKSC_ZALO_TEST_SQL_CONNECTION")!;
        Assert.StartsWith("TKSC_ZaloTest_",new SqlConnectionStringBuilder(connection).InitialCatalog);
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:Sep490",connection}}).Build();
        var store=new SqlZaloStore(config);
        await using var c=await store.OpenAsync(default);
        var suffix=Guid.NewGuid().ToString("N");
        var date=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        await using var setup=SqlZaloStore.Command(c,"""
            DECLARE @venue bigint,@court bigint,@sport bigint=(SELECT TOP(1) id FROM dbo.sports WHERE LOWER(code)='badminton'),
              @slot bigint=(SELECT TOP(1) id FROM dbo.time_slots WHERE DATEDIFF(minute,start_time,end_time)>60),
              @creator bigint=(SELECT TOP(1) id FROM dbo.users WHERE LOWER(status)='active');
            INSERT dbo.venues(name,address,is_active) VALUES(@name,N'Test address',1); SET @venue=SCOPE_IDENTITY();
            INSERT dbo.courts(venue_id,name,sport_id,is_active) VALUES(@venue,N'Court QA',@sport,1); SET @court=SCOPE_IDENTITY();
            INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by)
              VALUES(@sport,@court,@slot,@date,'B3',1,100000,'Open',@creator);
            SELECT CONVERT(bigint,SCOPE_IDENTITY()),@venue;
            """,("@name","Zalo QA "+suffix),("@date",date.ToDateTime(TimeOnly.MinValue)));
        long session,venue;
        await using(var r=await setup.ExecuteReaderAsync()){Assert.True(await r.ReadAsync());session=r.GetInt64(0);venue=r.GetInt64(1);}
        var zalo=Options.Create(new ZaloOptions {Enabled=true,OaId="qa-"+suffix,AppId="qa",AccessToken="fake",VenueId=venue});
        var intent=new IntentStub();
        await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).Options);
        var gateway=new GatewayStub();
        var payments=new PaymentService(new PaymentRepository(db),gateway,TimeProvider.System);
        var engine=new ZaloBookingService(store,intent,payments,config,zalo,TimeProvider.System);
        long customer=0;
        async Task<ZaloWork> Say(string text,ZaloIntent answer)
        {
            intent.Next=answer;
            var inbound=new ZaloInboundText(zalo.Value.OaId,"customer",Guid.NewGuid().ToString("N"),text,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            await store.EnqueueAsync(inbound,default);await store.EnqueueAsync(inbound,default);
            var work=await store.NextAsync(c,zalo.Value.OaId,default);Assert.NotNull(work);customer=work.CustomerId;
            await engine.ProcessAsync(work,default);
            return work;
        }
        await Say("còn slot cầu lông không",new("search",null,null,null,"badminton",date.ToString("yyyy-MM-dd"),null,null,null));
        Assert.Single((await store.StateAsync(c,customer,default)).Choices);
        await Say("vé đầu tiên",new("select",session,null,null,null,null,null,null,null));
        await Say("Tôi là Khách QA, tự chấm 50/100",new("profile",null,"Khách QA",50,null,null,null,null,null));
        var quote=(await store.StateAsync(c,customer,default)).Selected!;
        var confirm=await Say("đồng ý vé vừa báo",new("confirm",null,null,null,null,null,null,null,null));
        var order=(await store.StateAsync(c,customer,default)).OrderId;
        Assert.NotNull(order);
        await engine.ProcessAsync(confirm,default); // crash/replay after registration commit
        await using(var count=SqlZaloStore.Command(c,"SELECT COUNT(*) FROM dbo.zalo_booking_orders WHERE customer_id=@id",("@id",customer)))
            Assert.Equal(1,Convert.ToInt32(await count.ExecuteScalarAsync()));
        var user=(await store.UserAsync(c,customer,default))!.Value;
        var snapshot=await payments.DisplayAsync(order.Value,user);
        Assert.Equal("Pending",snapshot.Status,ignoreCase:true);
        Assert.Single(snapshot.Tickets);Assert.Equal(100000,snapshot.TotalAmount);
        await store.EnsurePlayerAsync(c,confirm,"Different name",quote.SportId,90,default);
        await using(var score=SqlZaloStore.Command(c,"SELECT current_score FROM dbo.player_sport_profiles p JOIN dbo.players x ON x.id=p.player_id WHERE x.user_id=@u AND p.sport_id=@s",("@u",user),("@s",quote.SportId)))
            Assert.Equal(50,Convert.ToInt32(await score.ExecuteScalarAsync()));
        // A second person cannot buy the last held place, or use the first person's identity.
        var other=new ZaloInboundText(zalo.Value.OaId,"other","other-"+suffix,"hello",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        await store.EnqueueAsync(other,default);
        var otherWork=(await store.NextAsync(c,zalo.Value.OaId,default))!;
        var otherUser=await store.EnsurePlayerAsync(c,otherWork,"Other",quote.SportId,50,default);
        var sessions=new SqlSessionStore(connection);
        var full=await sessions.RegisterZaloAsync(otherUser,new(otherWork.CustomerId,otherWork.MessageId,quote,DateTimeOffset.UtcNow.AddMinutes(5)),default);
        Assert.False(full.Success);Assert.Equal("SESSION_FULL",full.ErrorCode);
        var wrong=await sessions.RegisterZaloAsync(otherUser,new(customer,"forged",quote,DateTimeOffset.UtcNow.AddMinutes(5)),default);
        Assert.Equal("ZALO_IDENTITY_INVALID",wrong.ErrorCode);
        var changed=await sessions.RegisterZaloAsync(otherUser,new(otherWork.CustomerId,"changed-"+suffix,quote with {PriceVnd=1},DateTimeOffset.UtcNow.AddMinutes(5)),default);
        Assert.Equal("QUOTE_CHANGED",changed.ErrorCode);
        var expired=await sessions.RegisterZaloAsync(otherUser,new(otherWork.CustomerId,"expired-"+suffix,quote,DateTimeOffset.UtcNow.AddSeconds(-1)),default);
        Assert.Equal("QUOTE_EXPIRED",expired.ErrorCode);
        await store.CompleteAsync(c,otherWork,new(),new("test done"),default);
        var handler=new ZaloHttpStub();
        var services=new ServiceCollection();
        services.AddSingleton(store);services.AddSingleton<IPaymentService>(payments);services.AddSingleton<IZaloBookingProcessor>(engine);
        services.AddSingleton(new ZaloOaClient(new HttpClient(handler),zalo));
        using var provider=services.BuildServiceProvider();
        var worker=new ZaloBookingWorker(provider.GetRequiredService<IServiceScopeFactory>(),zalo,NullLogger<ZaloBookingWorker>.Instance);
        await worker.TickAsync(default);
        Assert.True(handler.Images>0);Assert.True(handler.Uploads>0);
        var countBefore=handler.Sends;
        await worker.TickAsync(default);Assert.Equal(countBefore,handler.Sends); // no duplicate sends
        gateway.Paid=true;
        await payments.HandleWebhookAsync("test-verified-event");
        await payments.HandleWebhookAsync("test-verified-event");
        var paid=await payments.DisplayAsync(order.Value,user);
        Assert.Equal("paid",paid.Status);Assert.True(Assert.Single(paid.Tickets).Valid);
        await worker.TickAsync(default);
        Assert.Contains(handler.Texts,x=>x.Contains("đã thanh toán"));
        var paidCount=handler.Sends;await worker.TickAsync(default);Assert.Equal(paidCount,handler.Sends);
        await using var duplicates=SqlZaloStore.Command(c,"SELECT COUNT(*) FROM dbo.bank_transactions WHERE matched_payment_id=@id",("@id",gateway.Id));
        Assert.Equal(1,Convert.ToInt32(await duplicates.ExecuteScalarAsync()));
        // An ambiguous network outcome after send must not be dispatched again automatically.
        var sentCount=handler.Sends;
        await using(var uncertain=SqlZaloStore.Command(c,"INSERT dbo.zalo_outbox(customer_id,deduplication_key,payload_json,state,expires_at) VALUES(@customer,'unknown-test',@payload,'sending',DATEADD(hour,1,SYSDATETIMEOFFSET()))",("@customer",customer),("@payload",JsonSerializer.Serialize(new ZaloOutgoing("Do not resend")))))
            await uncertain.ExecuteNonQueryAsync();
        await worker.TickAsync(default);Assert.Equal(sentCount,handler.Sends);
        await using(var uncertain=SqlZaloStore.Command(c,"SELECT state FROM dbo.zalo_outbox WHERE customer_id=@id AND deduplication_key='unknown-test'",("@id",customer)))
            Assert.Equal("unknown",await uncertain.ExecuteScalarAsync());
    }

    private sealed class IntentStub : IZaloIntentInterpreter
    {
        public ZaloIntent Next=new("help",null,null,null,null,null,null,null,null);
        public Task<ZaloIntent?> InterpretAsync(string text,ZaloConversation state,CancellationToken ct)=>Task.FromResult<ZaloIntent?>(Next);
    }
    private sealed class GatewayStub : IPaymentGateway
    {
        public long Id; public int Amount;public bool Paid;
        public void ValidateConfiguration(){}
        public Task<GatewayCheckout> CreateAsync(long paymentId,long orderId,int amount,DateTimeOffset expiresAt)
        {Id=paymentId;Amount=amount;return Task.FromResult(new GatewayCheckout(Id,amount,"qa-"+Id,"https://pay.payos.vn/web/qa-"+Id,"PENDING") {QrCode="00020101021238570010A000000727012700069704220113113366668888020899998888530370454061000005802VN62230819THANH TOAN DON HANG6304BE36"});}
        private VerifiedBankEvent Event=>new(Id,Amount,"zalo-qa-"+Id,"qa-"+Id,"VND",Paid,"test-event");
        public Task<GatewayStatus> GetAsync(long paymentId)=>Task.FromResult(new GatewayStatus(Id,Paid?Amount:0,Paid?"PAID":"PENDING","qa-"+Id,Paid?[Event]:[]));
        public Task<VerifiedBankEvent> VerifyAsync(string payload)=>Task.FromResult(Event);
    }
    private sealed class ZaloHttpStub : HttpMessageHandler
    {
        public int Sends,Images,Uploads; public List<string> Texts=[];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct)
        {
            if(r.RequestUri!.AbsolutePath.EndsWith("/upload/image"))
            {Uploads++;Assert.IsType<MultipartFormDataContent>(r.Content);return Reply("{\"error\":0,\"data\":{\"attachment_id\":\"qa-image\"}}");}
            Sends++;using var json=JsonDocument.Parse(await r.Content!.ReadAsStringAsync(ct));
            var message=json.RootElement.GetProperty("message");
            Texts.Add(message.GetProperty("text").GetString()!);
            if(message.TryGetProperty("attachment",out _)) Images++;
            return Reply("{\"error\":0,\"data\":{\"message_id\":\"qa-"+Sends+"\"}}");
        }
        private static HttpResponseMessage Reply(string text)=>new(HttpStatusCode.OK){Content=new StringContent(text,Encoding.UTF8,"application/json")};
    }
}
