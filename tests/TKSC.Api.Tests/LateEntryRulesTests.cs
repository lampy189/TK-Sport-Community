using TKSC.Application.DTOs;
using TKSC.Domain.Payments;
using Xunit;
namespace TKSC.Api.Tests;
public sealed class LateEntryRulesTests
{
    private static readonly DateTimeOffset Start=new(2026,11,9,18,0,0,TimeSpan.FromHours(7));
    private static readonly DateTimeOffset End=Start.AddHours(2);
    [Theory]
    [InlineData(0,true)] [InlineData(61,true)] [InlineData(119,true)] [InlineData(120,false)] [InlineData(-1,false)]
    public void CanSellAnyRemainingDurationUntilExactEnd(int minutes,bool expected)
        =>Assert.Equal(expected,LateEntryRules.Running(Start,End,"Open",Start.AddMinutes(minutes)));
    [Theory]
    [InlineData("cancelled")] [InlineData("suspended")] [InlineData("completed")]
    public void DisruptedSessionsCannotBeSold(string status)
        =>Assert.False(LateEntryRules.Running(Start,End,status,Start.AddMinutes(30)));
    [Fact]
    public void LateHoldNeverOutlivesTheSession()
    {
        Assert.Equal(End,LateEntryRules.HoldExpiry(End.AddMinutes(-1),End));
        Assert.Equal(Start.AddMinutes(35),LateEntryRules.HoldExpiry(Start.AddMinutes(30),End));
    }
    [Theory]
    [InlineData(3,2,80000)] [InlineData(0,2,80000)] [InlineData(1,2,0)] [InlineData(1,2,100000001)]
    public void SaleCannotExceedFreeSeatsOrAcceptInvalidPrices(int seats,int free,int price)
        =>Assert.Throws<PaymentException>(()=>LateEntryRules.CheckSale(Start,End,"Open",Start.AddMinutes(90),seats,free,price));
    [Fact]
    public void LessThanOneHourStillUsesThePublishedPriceAndSessionEnd()
    {
        LateEntryRules.CheckSale(Start,End,"Open",End.AddMinutes(-20),2,2,50000);
        Assert.Equal(End,LateEntryRules.End(new DateOnly(2026,11,9),new TimeOnly(20,0)));
    }
}
