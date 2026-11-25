using System.Text.Json;
using be_da.Application.DTOs;
using be_da.Application.Services;
using be_da.Application.Interfaces.External;
using be_da.Infrastructure.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TKSC.Domain.Exceptions;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class WorkflowDatabaseFactAttribute:FactAttribute
{
    public WorkflowDatabaseFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TKSC_NHAT_WORKFLOW_TEST_SQL"))) Skip="Requires an isolated TKSC_Nhat_WorkflowTests_* database with upgrade 017; writes synthetic fixtures.";
    }
}
[Collection("SQL Server")]
public sealed class NhatWorkflowDatabaseTests
{
    private sealed record Scope(string Role,long? VenueId):IDataAccessContext { public long? RestrictedVenueId=>Role=="Owner"||Role=="SystemAdmin"?null:VenueId; }
    private sealed class Fixture
    {
        public string Connection { get; }=Environment.GetEnvironmentVariable("TKSC_NHAT_WORKFLOW_TEST_SQL")!;
        public IConfiguration Config=>new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["ConnectionStrings:Sep490"]=Connection }).Build();
        public long Venue,OtherVenue,Court,Sport,Slot,Owner,Owner2,Admin,Manager,Staff,Staff2,Inviter,A,B,InviterPlayer,PlayerA,PlayerB;
        public async Task<long> Sql(string sql,params (string,object?)[] values)
        {
            // Never run writing fixtures against the user's working database.
            Assert.StartsWith("TKSC_Nhat_WorkflowTests_",new SqlConnectionStringBuilder(Connection).InitialCatalog);
            await using var c=new SqlConnection(Connection); await c.OpenAsync();
            await using var cmd=new SqlCommand("SET NOCOUNT ON; EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service'; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL; "+sql,c);
            foreach(var (key,value) in values)cmd.Parameters.AddWithValue(key,value??DBNull.Value);
            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }
        private Task<long> User(string role)=>Sql("INSERT dbo.users(role_id,email,full_name,status,venue_id) SELECT id,@email,@name,'Active',@venue FROM dbo.roles WHERE code=@role; SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@email",$"nhat-{Guid.NewGuid():N}@example.invalid"),("@name","Workflow "+role),("@role",role),("@venue",Venue));
        public async Task Init()
        {
            Venue=await Sql("INSERT dbo.venues(name,is_active) VALUES('Workflow test',1); SELECT CONVERT(bigint,SCOPE_IDENTITY());");
            OtherVenue=await Sql("INSERT dbo.venues(name,is_active) VALUES('Other workflow test',1); SELECT CONVERT(bigint,SCOPE_IDENTITY());");
            Sport=await Sql("SELECT TOP(1) id FROM dbo.sports ORDER BY id;");
            Court=await Sql("INSERT dbo.courts(name,venue_id,sport_id,is_active) VALUES('Workflow court',@venue,@sport,1); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@venue",Venue),("@sport",Sport));
            Slot=await Sql("INSERT dbo.time_slots(start_time,end_time) VALUES('16:00','18:00'); SELECT CONVERT(bigint,SCOPE_IDENTITY());");
            Owner=await User("Owner"); Owner2=await User("Owner"); Admin=await User("SystemAdmin"); Manager=await User("Manager"); Staff=await User("Staff"); Staff2=await User("Staff");
            Inviter=await User("Player"); A=await User("Player"); B=await User("Player");
            InviterPlayer=await Sql("INSERT dbo.players(user_id) VALUES(@u); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@u",Inviter));
            PlayerA=await Sql("INSERT dbo.players(user_id) VALUES(@u); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@u",A));
            PlayerB=await Sql("INSERT dbo.players(user_id) VALUES(@u); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@u",B));
            await Sql("INSERT dbo.friendships(requester_id,addressee_id,status) VALUES(@me,@a,'Accepted'),(@me,@b,'Accepted'); SELECT 1;",("@me",InviterPlayer),("@a",PlayerA),("@b",PlayerB));
        }
        public Task<long> SetInvitationSkillProfiles() => Sql("""
            INSERT dbo.player_sport_profiles(player_id,sport_id,current_score,current_band,rating_status)
            VALUES(@a,@sport,30,'B2','Provisional'),(@b,@sport,30,'B2','Provisional');
            SELECT 1;
            """, ("@a",PlayerA), ("@b",PlayerB), ("@sport",Sport));
        public Task<long> Session(int capacity=8,string? band=null)=>Sql("INSERT dbo.sessions(sport_id,court_id,time_slot_id,session_date,band,capacity,price_vnd,status,created_by) VALUES(@sport,@court,@slot,DATEADD(day,40,CONVERT(date,SYSDATETIMEOFFSET())),@band,@capacity,100000,'Open',@owner); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@sport",Sport),("@court",Court),("@slot",Slot),("@band",band),("@capacity",capacity),("@owner",Owner));
        public InvitationRepository Invitations()=>new(Config,new Scope("Player",Venue));
        public SqlSessionStore Sessions()=>new(Connection,new Scope("Player",Venue));
        public OperationsRepository Operations(string role="Owner")=>new(Config,new Scope(role,Venue));
    }
    [WorkflowDatabaseFact]
    public async Task NewSkillRequestsNotifyTheRightRefereesOnce()
    {
        var fixture = new Fixture();
        await fixture.Init();
        await fixture.SetInvitationSkillProfiles();
        var session = await fixture.Session(band: "B4");
        var referee = await fixture.Sql("""
            INSERT dbo.users(role_id,email,full_name,status,venue_id)
            SELECT id,@email,'R04 test referee','Active',@venue FROM dbo.roles WHERE code='Referee';
            SELECT CONVERT(bigint,SCOPE_IDENTITY());
            """, ("@email",$"r04-{Guid.NewGuid():N}@example.invalid"), ("@venue",fixture.Venue));
        var secondReferee = await fixture.Sql("""
            INSERT dbo.users(role_id,email,full_name,status,venue_id)
            SELECT id,@email,'R04 test referee two','Active',@venue FROM dbo.roles WHERE code='Referee';
            SELECT CONVERT(bigint,SCOPE_IDENTITY());
            """, ("@email",$"r04-{Guid.NewGuid():N}@example.invalid"), ("@venue",fixture.Venue));
        await fixture.Sql("""
            INSERT dbo.session_referees(session_id,referee_id,assigned_by) VALUES(@session,@first,@manager),(@session,@second,@manager);
            SELECT 1;
            """, ("@session",session), ("@first",referee), ("@second",secondReferee), ("@manager",fixture.Manager));
        var assessment = await fixture.Sql("""
            DECLARE @profile bigint=(SELECT id FROM dbo.player_sport_profiles WHERE player_id=@player AND sport_id=@sport);
            DECLARE @rubric bigint=(SELECT id FROM dbo.rubrics WHERE sport_id=@sport AND is_active=1);
            INSERT dbo.assessments(player_sport_profile_id,session_id,rubric_id,assessment_type,assessor_id,status,total_score,band_result)
            VALUES(@profile,@session,@rubric,'on_court',@referee,'published',45,'B3');
            DECLARE @assessment bigint=SCOPE_IDENTITY();
            INSERT dbo.player_rating_history(player_sport_profile_id,new_score,new_band,changed_by,assessment_id)
            VALUES(@profile,45,'B3',@referee,@assessment);
            SELECT @assessment;
            """, ("@player",fixture.PlayerA), ("@sport",fixture.Sport), ("@session",session), ("@referee",referee));
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TKSC.Infrastructure.Persistence.Contexts.Sep490Context>()
            .UseSqlServer(fixture.Connection).Options;
        await using var db = new TKSC.Infrastructure.Persistence.Contexts.Sep490Context(options);
        var service = new TKSC.Application.Services.SkillReviewService(new TKSC.Infrastructure.Persistence.Repositories.SkillReviewRepository(db, TimeProvider.System), TimeProvider.System);
        var reason = new TKSC.Application.DTOs.ReviewInput("Lý do kiểm thử thông báo cho trọng tài.");
        var exception = await service.RequestException(session, fixture.A, reason, default);
        await service.RequestException(session, fixture.A, reason, default);
        var appeal = await service.RequestAppeal(assessment, fixture.A, reason, default);
        await service.RequestAppeal(assessment, fixture.A, reason, default);
        var exceptionId = JsonSerializer.SerializeToElement(exception).GetProperty("Id").GetInt64();
        var appealId = JsonSerializer.SerializeToElement(appeal).GetProperty("Id").GetInt64();
        Assert.Equal(2, await fixture.Sql("""
            SELECT COUNT(*) FROM dbo.notifications WHERE reference_type='referee_skill_exception' AND reference_id=@id AND user_id IN (@first,@second) AND status='unread';
            """, ("@id",exceptionId), ("@first",referee), ("@second",secondReferee)));
        Assert.Equal(referee, await fixture.Sql("""
            SELECT CASE WHEN COUNT(*)=1 THEN MIN(user_id) END FROM dbo.notifications
            WHERE reference_type='referee_rating_appeal' AND reference_id=@id AND status='unread';
            """, ("@id",appealId)));
    }

    private static long Id(OperationResult result)=>JsonSerializer.SerializeToElement(result.Value).GetProperty("id").GetInt64();
    [WorkflowDatabaseFact]
    public async Task InvitationDoesNotHoldUntilConsentAndUsesTheInviteeAsPayerIdempotently()
    {
        var f=new Fixture(); await f.Init(); var session=await f.Session();
        var repo=f.Invitations();
        await f.SetInvitationSkillProfiles();
        var service = new InvitationService(repo, f.Sessions());
        Assert.Equal(OperationOutcome.Invalid, (await service.SendAsync(f.Inviter, session, new(f.PlayerA, "inviter"), default)).Outcome);
        var invitation=Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerA),default));
        await f.Sql("UPDATE dbo.invitations SET payer_user_id=@payer WHERE id=@id; SELECT 1;", ("@payer",f.Inviter), ("@id",invitation));
        Assert.Equal("INVITATION_PAYMENT_UNAVAILABLE", (await f.Sessions().AcceptInvitationAsync(f.A, invitation, default)).ErrorCode);
        await f.Sql("UPDATE dbo.invitations SET payer_user_id=@payer WHERE id=@id; SELECT 1;", ("@payer",f.A), ("@id",invitation));
        Assert.Equal(0,await f.Sql("SELECT COUNT(*) FROM dbo.tickets WHERE session_id=@s",("@s",session)));
        Assert.Equal(invitation,Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerA),default)));
        Assert.Empty(await repo.ListAsync(f.Owner,default));
        Assert.False((await f.Sessions().AcceptInvitationAsync(f.B,invitation,default)).Success);
        var accepted=await f.Sessions().AcceptInvitationAsync(f.A,invitation,default);
        Assert.True(accepted.Success,accepted.Message);
        var retry=await f.Sessions().AcceptInvitationAsync(f.A,invitation,default);
        Assert.Equal(accepted.TicketId,retry.TicketId);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.tickets WHERE session_id=@s",("@s",session)));
        Assert.Equal(f.A,await f.Sql("SELECT payer_id FROM dbo.social_booking_orders WHERE id=@id",("@id",accepted.OrderId)));
        Assert.Equal("accepted",Assert.Single(await repo.ListAsync(f.A,default)).Status);
        var second=Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerB,"separate"),default));
        Assert.Equal(OperationOutcome.Completed,(await repo.DeclineAsync(f.B,second,default)).Outcome);
        Assert.False((await f.Sessions().AcceptInvitationAsync(f.B,second,default)).Success);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.tickets WHERE session_id=@s",("@s",session)));
    }
    [WorkflowDatabaseFact]
    public async Task LastSeatCreatesOnlyOnePayableOrderAndReturnsToSaleAfterExpiry()
    {
        var f = new Fixture();
        await f.Init();
        await f.SetInvitationSkillProfiles();
        var session = await f.Session(capacity: 1);
        var players = new[] { f.A, f.B };
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = players.Select(async user =>
        {
            await start.Task;
            return (User: user, Result: await f.Sessions().RegisterSessionAsync(user, session, default));
        }).ToArray();

        start.SetResult();
        var results = await Task.WhenAll(attempts);
        var winner = Assert.Single(results, item => item.Result.Success);
        var loser = Assert.Single(results, item => !item.Result.Success);
        Assert.Equal("SESSION_FULL", loser.Result.ErrorCode);
        Assert.Null(loser.Result.OrderId);
        Assert.Null(loser.Result.TicketId);
        Assert.Equal(1, await f.Sql("SELECT COUNT(*) FROM dbo.social_booking_orders WHERE payer_id IN (@a,@b)",
            ("@a", f.A), ("@b", f.B)));
        Assert.Equal(winner.User, await f.Sql("SELECT payer_id FROM dbo.social_booking_orders WHERE id=@id",
            ("@id", winner.Result.OrderId)));
        Assert.Equal(1, await f.Sql("SELECT COUNT(*) FROM dbo.tickets WHERE session_id=@session", ("@session", session)));

        await f.Sql("UPDATE dbo.social_booking_orders SET expires_at=DATEADD(second,-1,SYSDATETIMEOFFSET()) WHERE id=@id; SELECT 1;",
            ("@id", winner.Result.OrderId));
        var retry = await f.Sessions().RegisterSessionAsync(loser.User, session, default);
        Assert.True(retry.Success, retry.Message);
        Assert.NotEqual(winner.Result.OrderId, retry.OrderId);
    }

    private sealed class PaidTestGateway : TKSC.Application.Interfaces.External.IPaymentGateway
    {
        private TKSC.Application.DTOs.VerifiedBankEvent payment = null!;
        public void ValidateConfiguration() { }
        public Task<TKSC.Application.DTOs.GatewayCheckout> CreateAsync(long paymentId, long orderId, int amount, DateTimeOffset expiresAt)
        {
            var link = $"test-{paymentId}";
            payment = new(paymentId, amount, link, link, "VND", true, "{}");
            return Task.FromResult(new TKSC.Application.DTOs.GatewayCheckout(paymentId, amount, link, $"https://example.invalid/{link}", "PENDING"));
        }
        public Task<TKSC.Application.DTOs.GatewayStatus> GetAsync(long paymentId) =>
            Task.FromResult(new TKSC.Application.DTOs.GatewayStatus(paymentId, payment.Amount, "PAID", payment.LinkId));
        public Task<TKSC.Application.DTOs.VerifiedBankEvent> VerifyAsync(string payload) => Task.FromResult(payment);
    }

    [WorkflowDatabaseFact]
    public async Task FirstHoldCanPayWhileAnotherPlayerAttemptsTheLastSeat()
    {
        var f = new Fixture();
        await f.Init();
        await f.SetInvitationSkillProfiles();
        var session = await f.Session(capacity: 1);
        var first = await f.Sessions().RegisterSessionAsync(f.A, session, default);
        Assert.True(first.Success, first.Message);
        var options = new DbContextOptionsBuilder<TKSC.Infrastructure.Persistence.Contexts.Sep490Context>()
            .UseSqlServer(f.Connection).Options;
        await using var db = new TKSC.Infrastructure.Persistence.Contexts.Sep490Context(options);
        var service = new TKSC.Application.Services.PaymentService(
            new TKSC.Infrastructure.Persistence.Repositories.PaymentRepository(db), new PaidTestGateway(), TimeProvider.System);
        await service.ProcessAsync(first.OrderId!.Value, f.A);

        var confirming = service.HandleWebhookAsync("test");
        var second = f.Sessions().RegisterSessionAsync(f.B, session, default);
        await Task.WhenAll(confirming, second);
        Assert.Equal("SESSION_FULL", (await second).ErrorCode);
        Assert.Null((await second).OrderId);
        Assert.Equal(1, await f.Sql("""
            SELECT COUNT(*) FROM dbo.tickets t JOIN dbo.social_booking_orders o ON o.id=t.order_id
            WHERE t.session_id=@session AND t.player_id=@player AND LOWER(t.status)='confirmed' AND LOWER(o.status)='paid'
            """, ("@session",session), ("@player",f.PlayerA)));
        Assert.Equal(0, await f.Sql("SELECT COUNT(*) FROM dbo.social_booking_orders WHERE payer_id=@loser", ("@loser", f.B)));
    }

    [WorkflowDatabaseFact]
    public async Task ConcurrentAcceptsNeverOverbookTheLastPlace()
    {
        var f=new Fixture(); await f.Init();await f.SetInvitationSkillProfiles();var session=await f.Session(1);
        var repo=f.Invitations();
        var a=Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerA),default));
        var b=Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerB),default));
        var results=await Task.WhenAll(f.Sessions().AcceptInvitationAsync(f.A,a,default),f.Sessions().AcceptInvitationAsync(f.B,b,default));
        Assert.Single(results,r=>r.Success);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.tickets WHERE session_id=@s",("@s",session)));
    }
    [WorkflowDatabaseFact]
    public async Task AcceptanceRechecksChangedSkillExpiryAndOverlappingBookings()
    {
        var f=new Fixture(); await f.Init();var session=await f.Session(8,"B2");var repo=f.Invitations();
        await f.Sql("INSERT dbo.player_sport_profiles(player_id,sport_id,current_score,current_band,rating_status) VALUES(@p,@s,30,'B2','Provisional'); SELECT 1",("@p",f.PlayerA),("@s",f.Sport));
        var id=Id(await repo.SendAsync(f.Inviter,session,new(f.PlayerA),default));
        await f.Sql("UPDATE dbo.player_sport_profiles SET current_band='B3',current_score=50 WHERE player_id=@p; SELECT 1",("@p",f.PlayerA));
        Assert.False((await f.Sessions().AcceptInvitationAsync(f.A,id,default)).Success);
        await f.Sql("UPDATE dbo.player_sport_profiles SET current_band='B2',current_score=30 WHERE player_id=@p; UPDATE dbo.invitations SET expires_at=DATEADD(minute,-1,SYSDATETIMEOFFSET()) WHERE id=@id; SELECT 1",("@p",f.PlayerA),("@id",id));
        Assert.Equal("INVITATION_EXPIRED",(await f.Sessions().AcceptInvitationAsync(f.A,id,default)).ErrorCode);
        f.Court=await f.Sql("INSERT dbo.courts(name,venue_id,sport_id,is_active) VALUES('Overlapping court',@venue,@sport,1); SELECT CONVERT(bigint,SCOPE_IDENTITY());",("@venue",f.Venue),("@sport",f.Sport));
        var other=await f.Session();
        Assert.True((await f.Sessions().RegisterSessionAsync(f.A,other,default)).Success);
        Assert.Equal(OperationOutcome.Invalid,(await repo.SendAsync(f.Inviter,session,new(f.PlayerA),default)).Outcome);
    }
    [WorkflowDatabaseFact]
    public async Task CourtShutdownAndPersonnelMovesProtectFutureSessionsAndRetainHistory()
    {
        var f=new Fixture(); await f.Init();var session=await f.Session();var ops=f.Operations();
        await Assert.ThrowsAsync<PaymentException>(()=>ops.SaveCourtAsync(f.Owner,f.Court,new(f.Venue,f.Sport,"Renamed",false),default));
        await Assert.ThrowsAsync<PaymentException>(()=>ops.SaveVenueAsync(f.Owner,f.Venue,new("Closed",null,false),default));
        await f.Sql("INSERT dbo.session_staff(session_id,staff_id) VALUES(@s,@staff); SELECT 1",("@s",session),("@staff",f.Staff));
        await Assert.ThrowsAsync<PaymentException>(()=>ops.PersonnelAsync(f.Owner,f.Staff,new(f.OtherVenue),default));
        var legacy=new SportCenterRepository(f.Config,new Scope("Owner",null));
        await Assert.ThrowsAsync<PaymentException>(()=>legacy.AssignAsync(f.Staff,f.OtherVenue,f.Owner,default));
        await ops.PersonnelAsync(f.Owner,f.Staff,new(f.OtherVenue,f.Staff2),default);
        Assert.Equal(f.Staff2,await f.Sql("SELECT staff_id FROM dbo.session_staff WHERE session_id=@s",("@s",session)));
        Assert.Equal(f.OtherVenue,await f.Sql("SELECT venue_id FROM dbo.users WHERE id=@id",("@id",f.Staff)));
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.audit_logs WHERE entity_type='Personnel' AND entity_id=@id AND action='personnel_assigned'",("@id",f.Staff)));
        await Assert.ThrowsAsync<PaymentException>(()=>f.Operations("Manager").SaveCourtAsync(f.Manager,null,new(f.OtherVenue,f.Sport,"Forbidden"),default));
    }
    [WorkflowDatabaseFact]
    public async Task ManagerApprovalsStayInAssignedCenterAndTechnicalAccessCannotEditBusinessRecords()
    {
        var f=new Fixture(); await f.Init();var admin=f.Operations("SystemAdmin");var manager=f.Operations("Manager");
        await Assert.ThrowsAsync<PaymentException>(()=>manager.ProposeAsync(f.Manager,"Manager",new(f.Admin,"Staff",f.Venue,"grant","Cannot demote admin"),default));
        await Assert.ThrowsAsync<PaymentException>(()=>admin.SaveVenueAsync(f.Admin,f.Venue,new("Forbidden",null),default));
        var other=Id(await admin.ProposeAsync(f.Admin,"SystemAdmin",new(f.A,"Staff",f.OtherVenue,"grant","Other center"),default));
        await Assert.ThrowsAsync<PaymentException>(()=>manager.DecideAsync(f.Manager,"Manager",other,new(true,"Outside center"),default));
        var own=Id(await admin.ProposeAsync(f.Admin,"SystemAdmin",new(f.B,"Staff",f.Venue,"grant","Assigned center"),default));
        var visible=JsonSerializer.SerializeToElement(await manager.AccessAsync(f.Manager,"Manager",default)).GetProperty("requests").EnumerateArray().Select(r=>r.GetProperty("id").GetInt64()).ToList();
        Assert.Contains(own,visible); Assert.DoesNotContain(other,visible);
        await manager.DecideAsync(f.Manager,"Manager",own,new(true,"Approved staff"),default);
        await admin.ExecuteAsync(f.Admin,own,default);
        Assert.Equal(f.Venue,await f.Sql("SELECT venue_id FROM dbo.users WHERE id=@id",("@id",f.B)));
    }
    [WorkflowDatabaseFact]
    public async Task ConversationReturnsTheLatestMessagesAndSavesWebNotificationAtomically()
    {
        var f=new Fixture(); await f.Init();
        await f.Sql("DECLARE @i int=0; WHILE @i<85 BEGIN INSERT dbo.messages(sender_id,recipient_id,content,is_read,created_at) VALUES(@sender,@recipient,CONVERT(nvarchar(20),@i),0,DATEADD(second,@i,SYSDATETIMEOFFSET())); SET @i+=1; END; SELECT 1;",("@sender",f.Inviter),("@recipient",f.A));
        var store=new SqlFriendshipStore(f.Connection,new Scope("Player",f.Venue));
        var recent=await store.GetConversationAsync(f.Inviter,f.PlayerA,80,default);
        Assert.Equal(80,recent.Count); Assert.Equal("5",recent[0].Content); Assert.Equal("84",recent[^1].Content);
        var sent=await store.SendMessageAsync(f.Inviter,f.PlayerA,"Durable message",default);
        Assert.NotNull(sent);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.notifications WHERE user_id=@recipient AND reference_type='chat_message' AND reference_id=@message",("@recipient",f.A),("@message",sent.Id)));
        await store.MarkMessagesAsReadAsync(f.A,f.InviterPlayer,default);
        Assert.Equal(0,await f.Sql("SELECT COUNT(*) FROM dbo.notifications WHERE user_id=@recipient AND reference_type='chat_message' AND reference_id=@message AND status='unread'",("@recipient",f.A),("@message",sent.Id)));
        await store.RemoveFriendAsync(f.Inviter,f.PlayerA,default);
        Assert.Null(await store.SendMessageAsync(f.Inviter,f.PlayerA,"Not delivered",default));
    }
    [WorkflowDatabaseFact]
    public async Task ApprovedAccessIsIndependentAndHandoverRevokesOnlyAfterIncomingVerification()
    {
        var f=new Fixture(); await f.Init();var admin=f.Operations("SystemAdmin");var owner=f.Operations();
        var request=Id(await admin.ProposeAsync(f.Admin,"SystemAdmin",new(f.A,"Manager",f.Venue,"grant","Appointment"),default));
        await Assert.ThrowsAsync<PaymentException>(()=>admin.ExecuteAsync(f.Admin,request,default));
        await owner.DecideAsync(f.Owner,"Owner",request,new(true,"Approved appointment"),default);
        await admin.ExecuteAsync(f.Admin,request,default);
        await admin.ExecuteAsync(f.Admin,request,default);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.users u JOIN dbo.roles r ON r.id=u.role_id WHERE u.id=@id AND r.code='Manager'",("@id",f.A)));
        var self=Id(await owner.ProposeAsync(f.Owner,"Owner",new(f.B,"Staff",f.Venue,"grant","Independent approval required"),default));
        await Assert.ThrowsAsync<PaymentException>(()=>owner.DecideAsync(f.Owner,"Owner",self,new(true,"Self approval"),default));
        await owner.DecideAsync(f.Owner2,"Owner",self,new(false,"Close proposal before handover"),default);
        var handover=Id(await admin.ProposeAsync(f.Admin,"SystemAdmin",new(f.B,"SystemAdmin",null,"handover","Separate incoming technical account",f.Admin),default));
        await owner.DecideAsync(f.Owner,"Owner",handover,new(true,"Owner designated operator"),default);
        await admin.ExecuteAsync(f.Admin,handover,default);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.users WHERE id=@id AND status='Active'",("@id",f.Admin)));
        await Assert.ThrowsAsync<PaymentException>(()=>admin.ExecuteAsync(f.Admin,handover,default));
        await Assert.ThrowsAsync<PaymentException>(()=>admin.VerifyAsync(f.Admin,handover,default));
        await admin.VerifyAsync(f.B,handover,default);
        await admin.ExecuteAsync(f.Admin,handover,default);
        Assert.Equal(1,await f.Sql("SELECT COUNT(*) FROM dbo.users WHERE id=@id AND status='Inactive'",("@id",f.Admin)));
    }
}
