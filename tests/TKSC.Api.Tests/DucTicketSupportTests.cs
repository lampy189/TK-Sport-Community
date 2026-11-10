using Microsoft.EntityFrameworkCore;
using TKSC.Application.Services;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using Xunit;
namespace TKSC.Api.Tests;

public sealed class DucTicketSupportTests
{
 private static async Task<Sep490Context> Database()
 {
  var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
  var player=new User{Id=1,FullName="Holder",Email="holder@example.test",Status="active",Role=new Role{Id=1,Code="Player",Name="Player"}};
  var payer=new User{Id=2,FullName="Payer",Email="payer@example.test",Status="active",Role=player.Role};
  var staff=new User{Id=3,VenueId=1,FullName="Staff",Email="staff@example.test",Status="active",Role=new Role{Id=2,Code="Staff",Name="Staff"}};
  db.Users.AddRange(player,payer,staff);
  db.Tickets.Add(new Ticket{Id=41,Status="confirmed",Player=new Player{Id=1,User=player},Order=new SocialBookingOrder{Id=1,Payer=payer,OrderCode="SOCIAL-41",Status="paid"},Session=new Session{Id=22,Status="Open",Court=new Court{Id=1,Name="Court",Venue=new Venue{Id=1,Name="Venue"}},Sport=new Sport{Id=1,Name="Badminton",Code="BM"},TimeSlot=new TimeSlot{Id=1}}});
  await db.SaveChangesAsync();return db;
 }
 [Theory]
 [InlineData(1,false)] [InlineData(2,false)] [InlineData(3,true)]
 public async Task ListAllowsHolderPayerOrStaffBeforeAnyDisruption(long actor,bool staff)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  Assert.NotNull(await service.List(41,actor,staff));
 }
 [Theory]
 [InlineData(41,99)] [InlineData(999,1)] [InlineData(999,99)]
 public async Task ListHidesOtherTicketsAndMissingTickets(long ticket,long actor)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await Assert.ThrowsAsync<PaymentException>(()=>service.List(ticket,actor,false));
 }
 [Theory]
 [InlineData("court")] [InlineData("flood")] [InlineData("payment")]
 public async Task ReportPersistsActorCategoryAndNotifiesStaff(string category)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await service.Report(41,1,category,"  Sân không thể chơi  ",null);
  var message=await db.TicketSupportMessages.SingleAsync();
  Assert.Equal(1,message.SenderId);Assert.Equal(41,message.TicketId);Assert.False(message.IsStaff);
  Assert.Equal(TicketSupportRules.Report(category,"Sân không thể chơi"),message.Content);
  Assert.Contains(await db.Notifications.ToListAsync(),n=>n.UserId==3&&n.ReferenceType=="ticket_player_message");
 }
 [Theory]
 [InlineData("Staff")] [InlineData("Manager")] [InlineData("manager")]
 public async Task PlayerReportNotifiesOnlyActiveStaffOrManagerInTheTicketVenue(string role)
 {
  await using var db=await Database();
  var reviewerRole=new Role{Id=20,Code=role,Name=role};
  foreach(var (id,venue,status,deleted) in new (long,long?,string,bool)[]{
   (10,1,"Active",false),(11,2,"Active",false),(12,null,"Active",false),
   (13,1,"Inactive",false),(14,1,"Active",true)})
   db.Users.Add(new User{Id=id,VenueId=venue,Email=$"reviewer-{id}@example.test",FullName=$"Reviewer {id}",
    Status=status,DeletedAt=deleted?DateTimeOffset.UtcNow:null,Role=reviewerRole});
  await db.SaveChangesAsync();
  await new TicketSupportService(db,TimeProvider.System).Report(41,1,"court","The court is damaged and cannot be used.",null);
  var recipients=await db.Notifications.Where(n=>n.ReferenceType=="ticket_player_message").Select(n=>n.UserId).OrderBy(id=>id).ToListAsync();
  Assert.Equal(new long[]{3,10},recipients);
 }
 [Theory]
 [InlineData("unknown","Mô tả đầy đủ sự cố",1)]
 [InlineData("court","short",1)]
 [InlineData("court","Mô tả đầy đủ sự cố",99)]
 public async Task ReportRejectsInvalidInputOrWrongActorWithoutWrites(string category,string content,long actor)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await Assert.ThrowsAsync<PaymentException>(()=>service.Report(41,actor,category,content,null));
  Assert.Empty(db.TicketSupportMessages);Assert.Empty(db.Notifications);
 }
 [Theory]
 [InlineData(1,false)] [InlineData(2,false)] [InlineData(3,true)]
 public async Task SendKeepsActorAndStaffFlag(long actor,bool staff)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await service.Send(41,actor,staff,"  Đang kiểm tra sự cố  ",null);
  var message=await db.TicketSupportMessages.SingleAsync();
  Assert.Equal(actor,message.SenderId);Assert.Equal(staff,message.IsStaff);Assert.Equal("Đang kiểm tra sự cố",message.Content);
 }
 [Theory]
 [InlineData("")] [InlineData("   ")] [InlineData(null)]
 public async Task SendRejectsEmptyTextWithoutAttachment(string? text)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await Assert.ThrowsAsync<PaymentException>(()=>service.Send(41,1,false,text,null));
  Assert.Empty(db.TicketSupportMessages);
 }
 [Theory]
 [InlineData(1,false)] [InlineData(2,false)] [InlineData(3,true)]
 public async Task ImageAllowsOnlyPermittedConversationParticipants(long actor,bool staff)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  db.TicketSupportMessages.Add(new(){Id=1,TicketId=41,SenderId=1,ImageData=[1,2,3],ImageType="image/png"});
  await db.SaveChangesAsync();
  var image=await service.Image(41,1,actor,staff);Assert.Equal(new byte[]{1,2,3},image.Bytes);
 }
 [Theory]
 [InlineData(41,1,99)] [InlineData(41,999,1)] [InlineData(999,1,1)]
 public async Task ImageRejectsOtherUserOrMissingImage(long ticket,long message,long actor)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await Assert.ThrowsAsync<PaymentException>(()=>service.Image(ticket,message,actor,false));
 }
 [Theory]
 [InlineData(0)] [InlineData(1)] [InlineData(3)]
 public async Task InboxGroupsByTicketAndUsesLastPlayerAsReporter(int replies)
 {
  await using var db=await Database();var service=new TicketSupportService(db,TimeProvider.System);
  await service.Report(41,2,"refund","Chưa nhận được tiền hoàn",null);
  for(var i=0;i<replies;i++)await service.Send(41,3,true,"Đang xử lý hoàn tiền",null);
  var json=System.Text.Json.JsonSerializer.Serialize(await service.Inbox());
  Assert.Contains("\"LastSender\":\"Payer\"",json);
  Assert.Contains("\"LastIsStaff\":"+(replies>0?"true":"false"),json);
 }
}
