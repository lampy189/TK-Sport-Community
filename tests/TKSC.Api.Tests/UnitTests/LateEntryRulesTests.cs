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
    [InlineData(120,0,80000)] [InlineData(120,15,70000)] [InlineData(120,30,60000)]
    [InlineData(120,60,40000)] [InlineData(120,90,40000)] [InlineData(120,119,40000)]
    [InlineData(60,15,60000)] [InlineData(60,30,40000)] [InlineData(180,45,60000)]
    public void PriceScalesByElapsedShareOfOriginalDurationWithFiftyPercentFloor(int duration,int elapsed,int expected)
        =>Assert.Equal(expected,LateEntryRules.Quote(Start,Start.AddMinutes(duration),"Open",Start.AddMinutes(elapsed),80000)!.PriceVnd);
    [Fact]
    public void PricesAreStableWithinAMinuteAndChangeRevisionWhenThePayablePriceChanges()
    {
        var quote=LateEntryRules.Quote(Start,End,"Open",Start.AddMinutes(15),80000)!;
        Assert.Equal(quote,LateEntryRules.Quote(Start,End,"Open",Start.AddMinutes(15).AddSeconds(59),80000));
        Assert.NotEqual(quote.Revision,LateEntryRules.Quote(Start,End,"Open",Start.AddMinutes(16),80000)!.Revision);
        Assert.NotEqual(quote.Revision,LateEntryRules.Quote(Start,End,"Open",Start.AddMinutes(15),100000)!.Revision);
    }
    [Theory]
    [InlineData(-1,"Open",80000)] [InlineData(120,"Open",80000)]
    [InlineData(15,"cancelled",80000)] [InlineData(15,"suspended",80000)]
    [InlineData(15,"completed",80000)] [InlineData(15,"Open",0)] [InlineData(15,"Open",1999)]
    public void NoAutomaticQuoteOutsideRunningSaleOrPayablePriceRange(int elapsed,string status,int price)
        =>Assert.Null(LateEntryRules.Quote(Start,End,status,Start.AddMinutes(elapsed),price));
    [Fact]
    public void AutomaticPriceNeverIncreasesOrDropsBelowHalfEvenWithFractionalDong()
    {
        const int original=80001;var previous=original;
        for(var minute=0;minute<120;minute++)
        {
            var price=LateEntryRules.Quote(Start,End,"Open",Start.AddMinutes(minute),original)!.PriceVnd;
            Assert.InRange(price,(int)Math.Ceiling(original/2m),previous);previous=price;
        }
        Assert.Equal(40001,previous);
    }
}
