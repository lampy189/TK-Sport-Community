using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;

if (!args.Contains("--apply"))
{
 Console.WriteLine("PLAN: 11 accounts ductmtest01..11, dedicated venue/court, 6 sessions, 5 synthetic paid tickets and 2 published assessments.");
 Console.WriteLine("No DB connection or writes. To apply: set TKSC_TEST_SQL_CONNECTION; pass --apply --database <exact-test-database-name>.");
 return;
}
var index=Array.IndexOf(args,"--database");
if(index<0 || index+1>=args.Length)throw new InvalidOperationException("Supply --database with the exact intended test DB name.");
var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")
 ?? throw new InvalidOperationException("Set TKSC_TEST_SQL_CONNECTION locally. Never save credentials in Git.");
var target=new SqlConnectionStringBuilder(connection);
if(target.InitialCatalog!=args[index+1])throw new InvalidOperationException("Connection database does not match --database.");
await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).Options);
await db.Database.OpenConnectionAsync();
await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';");
try
{
 await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
 var emails=Enumerable.Range(1,11).Select(i=>$"ductmtest{i:00}@demo.tksc.invalid").ToArray();
 if(await db.Users.AnyAsync(u=>emails.Contains(u.Email)))
  throw new InvalidOperationException("Some ductmtest accounts already exist. No data reset. Use the existing manifest or another test database.");
 if(!await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID('dbo.users') AND name='venue_id'").AnyAsync(n=>n==1))
  throw new InvalidOperationException("Apply the project's approved Sport Center upgrades first; this script changes no schema.");
 var roles=await db.Roles.ToDictionaryAsync(r=>r.Code.ToLowerInvariant());
 var sport=await db.Sports.SingleAsync(s=>s.Code=="badminton");
 var rubric=await db.Rubrics.Include(r=>r.RubricDimensions).SingleAsync(r=>r.SportId==sport.Id && r.Version=="BM-CLB-V1");
 if(rubric.RubricDimensions.Count!=5)throw new InvalidOperationException("Expected the configured five-dimension BM-CLB-V1 rubric.");
 var today=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
 var venue=new Venue{Name="DUCTM TEST "+today+" "+Guid.NewGuid().ToString("N")[..6],Address="SYNTHETIC QA ONLY",IsActive=true};
 db.Venues.Add(venue);await db.SaveChangesAsync();
 var users=new Dictionary<int,User>();var players=new Dictionary<int,Player>();var profiles=new Dictionary<int,PlayerSportProfile>();
 for(var i=1;i<=11;i++)
 {
  var role=i switch{1=>"manager",2=>"staff",3=>"referee",_=>"player"};
  var user=new User{Email=emails[i-1],FullName=$"ductmtest{i:00}",RoleId=roles[role].Id,Status="active"};
  db.Users.Add(user);await db.SaveChangesAsync();users[i]=user;
  await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.users SET venue_id={venue.Id} WHERE id={user.Id}");
  if(i<4)continue;
  var player=new Player{UserId=user.Id};db.Players.Add(player);await db.SaveChangesAsync();players[i]=player;
  var profile=new PlayerSportProfile{PlayerId=player.Id,SportId=sport.Id,CurrentScore=(short)(i is 4 or 5?30:50),CurrentBand=i is 4 or 5?"B2":"B3",RatingStatus="Rated"};
  db.PlayerSportProfiles.Add(profile);await db.SaveChangesAsync();profiles[i]=profile;
 }
 var court=new Court{VenueId=venue.Id,SportId=sport.Id,Name="DUCTM TEST COURT",IsActive=true};db.Courts.Add(court);await db.SaveChangesAsync();
 var slot=await db.TimeSlots.FirstOrDefaultAsync(t=>t.StartTime==new TimeOnly(18,0)&&t.EndTime==new TimeOnly(20,0));
 if(slot==null){slot=new TimeSlot{StartTime=new(18,0),EndTime=new(20,0),Label="18:00–20:00"};db.TimeSlots.Add(slot);await db.SaveChangesAsync();}
 var sessions=new Dictionary<string,Session>();
 async Task AddSession(string key,int offset,int price=100000,string status="Published")
 {
  var s=new Session{SportId=sport.Id,CourtId=court.Id,TimeSlotId=slot.Id,SessionDate=today.AddDays(offset),Band="B3",Capacity=8,PriceVnd=price,Status=status,CreatedBy=users[1].Id};
  db.Sessions.Add(s);await db.SaveChangesAsync();
  db.SessionReferees.Add(new(){SessionId=s.Id,RefereeId=users[3].Id,AssignedBy=users[1].Id});
  await db.SaveChangesAsync();sessions[key]=s;
 }
 await AddSession("exception",2);await AddSession("venue",3);await AddSession("exchange-source",4);
 await AddSession("exchange-target",5,80000);await AddSession("appeal-future",6);await AddSession("assessment",-1,100000,"Completed");
 var tickets=new List<object>();
 async Task Ticket(int account,string key)
 {
  var code=$"DUCTMTEST-{account:00}-{key}";
  var order=new SocialBookingOrder{OrderCode=code,PayerId=users[account].Id,TotalAmount=100000,Status="paid"};
  order.Tickets.Add(new(){PlayerId=players[account].Id,SessionId=sessions[key].Id,AmountVnd=100000,TicketCode=code,Status="confirmed"});
  order.Payments.Add(new(){PayerId=users[account].Id,AmountVnd=100000,Method="demo-fixture",ReferenceCode=code,Status="paid",ConfirmedBy=users[2].Id});
  db.SocialBookingOrders.Add(order);await db.SaveChangesAsync();
  var ticket=order.Tickets.Single();var payment=order.Payments.Single();
  db.TicketFunding.Add(new(){TicketId=ticket.Id,PaymentId=payment.Id,AmountVnd=100000});
  db.AuditLogs.Add(new(){ActorId=users[1].Id,EntityType="SocialBookingOrder",EntityId=order.Id,Action="synthetic_qa_fixture",NewValue="DUCTM TEST: FAKE PAID; no bank transfer; test venue only"});
  await db.SaveChangesAsync();
  tickets.Add(new{account=users[account].FullName,key,ticketId=ticket.Id,ticketCode=code,sessionId=ticket.SessionId,orderId=order.Id,paymentId=payment.Id});
 }
 await Ticket(6,"venue");await Ticket(7,"venue");await Ticket(8,"exchange-source");await Ticket(9,"appeal-future");await Ticket(10,"appeal-future");
 var assessments=new List<object>();
 foreach(var account in new[]{9,10})
 {
  var assessment=new Assessment{PlayerSportProfileId=profiles[account].Id,SessionId=sessions["assessment"].Id,RubricId=rubric.Id,AssessmentType="OnCourt",AssessorId=users[3].Id,Status="published",TotalScore=50,BandResult="B3"};
  foreach(var dimension in rubric.RubricDimensions)assessment.AssessmentScores.Add(new(){DimensionId=dimension.Id,Score=2});
  db.Assessments.Add(assessment);await db.SaveChangesAsync();
  db.PlayerRatingHistories.Add(new(){PlayerSportProfileId=profiles[account].Id,AssessmentId=assessment.Id,ChangedBy=users[3].Id,NewScore=50,NewBand="B3"});
  db.AuditLogs.Add(new(){ActorId=users[3].Id,EntityType="Assessment",EntityId=assessment.Id,Action="assessment_saved",NewValue=JsonSerializer.Serialize(new{Marks=rubric.RubricDimensions.Select(d=>new{DimensionId=d.Id,Score=2,Evidence="SYNTHETIC DUCTM TEST"})})});
  await db.SaveChangesAsync();
  assessments.Add(new{account=users[account].FullName,assessmentId=assessment.Id});
 }
 var manifest=new{
  createdAt=DateTimeOffset.UtcNow,warning="SYNTHETIC PAID / ASSESSMENT DATA. Not real money or real skill decisions.",
  venue=venue.Name,venueId=venue.Id,
  accounts=users.Select(pair=>new{name=pair.Value.FullName,email=pair.Value.Email,role=pair.Key switch{1=>"Manager",2=>"Staff",3=>"Referee",_=>"Player"},userId=pair.Value.Id}),
  sessions=sessions.Select(pair=>new{key=pair.Key,id=pair.Value.Id,date=pair.Value.SessionDate,start="18:00",price=pair.Value.PriceVnd,status=pair.Value.Status}),tickets,assessments};
 // Serialize before committing so serialization failures roll back fixture writes.
 var json=JsonSerializer.Serialize(manifest,new JsonSerializerOptions{WriteIndented=true});
 var output="ductm-test-manifest.json";var outIndex=Array.IndexOf(args,"--output");
 if(outIndex>=0 && outIndex+1<args.Length)output=args[outIndex+1];
 // Write manifest before commit; consumers must use it only after the OK line.
 await File.WriteAllTextAsync(output,json);
 await tx.CommitAsync();
 Console.WriteLine($"OK: 11 accounts, 6 sessions, 5 synthetic paid tickets, 2 assessments. Manifest: {output}");
}
finally
{
 await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL;");
}
