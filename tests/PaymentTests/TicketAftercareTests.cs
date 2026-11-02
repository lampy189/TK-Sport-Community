using TKSC.Application.Services;
using TKSC.Application.DTOs;
using TKSC.Domain.Entities;
using Xunit;
public class TicketAftercareTests
{
 [Theory]
 [InlineData(100000,80000,80000,20000)]
 [InlineData(100000,120000,100000,0)]
 [InlineData(100000,100000,100000,0)]
 [InlineData(100000,0,0,100000)]
 public void DifferenceNeverCreatesOrLosesOriginalMoney(int oldPrice,int newPrice,int transfer,int refund)
 {
  var parts=TicketAftercareRules.Split([new TicketFunding{Id=1,PaymentId=3,AmountVnd=oldPrice}],newPrice);
  Assert.Equal(transfer,parts.Sum(p=>p.Transfer)); Assert.Equal(refund,parts.Sum(p=>p.Refund));
  Assert.Equal(oldPrice,parts.Sum(p=>p.Transfer+p.Refund));
 }
 [Fact]
 public void MultiplePaymentSourcesRemainTraceableAcrossCheaperExchange()
 {
  var parts=TicketAftercareRules.Split([new TicketFunding{Id=1,PaymentId=11,AmountVnd=80000},new TicketFunding{Id=2,PaymentId=12,AmountVnd=40000}],90000);
  Assert.Equal((11L,80000,0),parts[0]);Assert.Equal((12L,10000,30000),parts[1]);
 }
 [Fact]
 public void TargetAtEightIsFullEvenIfConfiguredHigher()
 {
  var now=DateTimeOffset.Parse("2026-10-30T08:00:00Z");
  var source=new Session{Id=1,SportId=1};
  var target=new Session{Id=2,SportId=1,Status="Open",Capacity=16,SessionDate=new DateOnly(2026,10,31),TimeSlot=new TimeSlot{StartTime=new TimeOnly(18,0)}};
  TicketAftercareRules.CheckTarget(source,target,7,now);
  Assert.Throws<PaymentException>(()=>TicketAftercareRules.CheckTarget(source,target,8,now));
  target.Status="suspended";Assert.Throws<PaymentException>(()=>TicketAftercareRules.CheckTarget(source,target,0,now));
  target.Status="Open";target.SportId=2;Assert.Throws<PaymentException>(()=>TicketAftercareRules.CheckTarget(source,target,0,now));
 }
 [Fact]
 public void HoldsExpireButPaidAndCheckedInPlacesRemainOccupied()
 {
  var now=DateTimeOffset.UtcNow;
  Assert.True(TicketAftercareRules.Occupies("Pending",now.AddMinutes(1),now));
  Assert.False(TicketAftercareRules.Occupies("Pending",now,now));
  Assert.False(TicketAftercareRules.Occupies("exchanged",null,now));
  Assert.True(TicketAftercareRules.Occupies("checkedin",null,now));
 }
}
