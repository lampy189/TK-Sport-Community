using TKSC.Domain.Exceptions;
using TKSC.Application.Services;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class DucWorkflowRuleTests
{
 [Theory]
 [InlineData("suspended",true)] [InlineData("CANCELLED",true)] [InlineData("Open",false)] [InlineData("completed",false)]
 public void DisruptedRecognizesOnlyVenueInterruptions(string status,bool expected)
  => Assert.Equal(expected,TicketAftercareRules.Disrupted(status));

 [Theory]
 [InlineData("confirmed",0,true)] [InlineData("checkedin",0,true)]
 [InlineData("pending",1,true)] [InlineData("reserved",0,false)]
 [InlineData("pending",-1,false)] [InlineData("exchanged",1,false)]
 public void OccupiesHonorsExpiryAndRetiredTickets(string status,int minutes,bool expected)
 {
  var now=DateTimeOffset.Parse("2026-11-09T08:00:00Z");
  Assert.Equal(expected,TicketAftercareRules.Occupies(status,now.AddMinutes(minutes),now));
 }
 [Theory]
 [InlineData(4,4)] [InlineData(8,8)] [InlineData(16,8)]
 public void LimitCapsSocialCourtsAtEight(int configured,int expected)
  => Assert.Equal(expected,TicketAftercareRules.Limit(configured));

 [Theory]
 [InlineData(100000,80000,80000,20000)]
 [InlineData(100000,120000,100000,0)]
 [InlineData(100000,0,0,100000)]
 [InlineData(0,100000,0,0)]
 public void SplitConservesFundingForCheaperDearerAndZeroPrice(int original,int target,int transferred,int refunded)
 {
  var parts=TicketAftercareRules.Split([new TicketFunding{Id=1,PaymentId=11,AmountVnd=original}],target);
  Assert.Equal(transferred,parts.Sum(p=>p.Transfer));Assert.Equal(refunded,parts.Sum(p=>p.Refund));
  Assert.Equal(original,parts.Sum(p=>p.Transfer+p.Refund));
 }
 [Fact]
 public void SplitKeepsOriginalPaymentSourcesInOrder()
 {
  var result=TicketAftercareRules.Split([new TicketFunding{Id=2,PaymentId=12,AmountVnd=40000},new TicketFunding{Id=1,PaymentId=11,AmountVnd=80000}],90000);
  Assert.Equal((11L,80000,0),result[0]);Assert.Equal((12L,10000,30000),result[1]);
 }
 [Theory]
 [InlineData("open",1,2,7,false)]
 [InlineData("published",1,2,0,false)]
 [InlineData("suspended",1,2,0,true)]
 [InlineData("open",2,2,0,true)]
 [InlineData("open",1,1,0,true)]
 [InlineData("open",1,2,8,true)]
 public void CheckTargetRejectsWrongSportSameSessionClosedOrFull(string status,long sport,long id,int occupied,bool rejects)
 {
  var source=new Session{Id=1,SportId=1};
  var target=new Session{Id=id,SportId=sport,Status=status,Capacity=16,SessionDate=new(2026,11,10),TimeSlot=new(){StartTime=new(18,0)}};
  var now=DateTimeOffset.Parse("2026-11-09T08:00:00Z");
  if(rejects)Assert.Throws<PaymentException>(()=>TicketAftercareRules.CheckTarget(source,target,occupied,now));
  else TicketAftercareRules.CheckTarget(source,target,occupied,now);
 }
 [Fact]
 public void CheckTargetRejectsAtExactVietnamStartTime()
 {
  var source=new Session{Id=1,SportId=1};
  var target=new Session{Id=2,SportId=1,Status="Open",Capacity=8,SessionDate=new(2026,11,10),TimeSlot=new(){StartTime=new(18,0)}};
  Assert.Throws<PaymentException>(()=>TicketAftercareRules.CheckTarget(source,target,0,DateTimeOffset.Parse("2026-11-10T11:00:00Z")));
 }
 [Theory]
 [InlineData("court","Sân hỏng")] [InlineData("flood","Sân ngập")] [InlineData("payment","Thanh toán lỗi")]
 [InlineData("refund","Chậm hoàn / thiếu quỹ hoàn")] [InlineData("other","Sự cố khác")]
 public void ReportPreservesCategoryAndTrimsDescription(string category,string label)
  => Assert.Equal($"[Báo cáo sự cố · {label}]\nSân không thể chơi",TicketSupportRules.Report(category,"  Sân không thể chơi  "));
 [Theory]
 [InlineData(null)] [InlineData("")] [InlineData("unknown")]
 public void ReportRejectsUnknownCategory(string? category)
  => Assert.Throws<PaymentException>(()=>TicketSupportRules.Report(category,"Sân không thể chơi"));
 [Theory]
 [InlineData(0)] [InlineData(9)] [InlineData(1801)]
 public void ReportRejectsMissingShortOrOversizedDescription(int length)
  => Assert.Throws<PaymentException>(()=>TicketSupportRules.Report("court",new string('x',length)));
 [Theory]
 [InlineData(10)] [InlineData(100)] [InlineData(1800)]
 public void ReportAcceptsDescriptionBoundaries(int length)
  => Assert.EndsWith(new string('x',length),TicketSupportRules.Report("other",new string('x',length)));
 [Theory]
 [InlineData("Open",false,"suspended")] [InlineData("Open",true,"cancelled")]
 [InlineData("suspended",true,"cancelled")] [InlineData("suspended",false,"suspended")]
 [InlineData("CANCELLED",true,"cancelled")]
 public void VenueDecisionHandlesSuspensionCancellationAndRetry(string current,bool cancel,string expected)
  => Assert.Equal(expected,TicketAftercareRules.VenueDecision(current,cancel));
 [Theory]
 [InlineData("completed",false)] [InlineData("completed",true)] [InlineData("cancelled",false)]
 public void VenueDecisionRejectsCompletedOrReopeningCancelledSession(string current,bool cancel)
  => Assert.Throws<PaymentException>(()=>TicketAftercareRules.VenueDecision(current,cancel));
 [Theory]
 [InlineData(10)] [InlineData(100)] [InlineData(1000)]
 public void EvidenceTrimsAndAcceptsValidBoundaries(int length)
  => Assert.Equal(new string('x',length),TicketAftercareRules.Evidence(" "+new string('x',length)+" "));
 [Theory]
 [InlineData(0)] [InlineData(9)] [InlineData(1001)]
 public void EvidenceRejectsMissingShortOrOversizedSettlement(int length)
  => Assert.Throws<PaymentException>(()=>TicketAftercareRules.Evidence(new string('x',length)));
}
