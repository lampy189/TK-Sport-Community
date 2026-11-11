using TKSC.Application.Services;
using TKSC.Application.Assessments;
using TKSC.Domain.Entities;
using Xunit;
namespace TKSC.Api.Tests;
public sealed class DucSkillReviewRuleTests
{
 [Theory]
 [InlineData(null)] [InlineData("")] [InlineData("   ")]
 public void ReasonRequiresMeaningfulText(string? reason)
  => Assert.Throws<AssessmentException>(()=>SkillReviewRules.Reason(reason));
 [Theory]
 [InlineData(1)] [InlineData(100)] [InlineData(2000)]
 public void ReasonTrimsAndAcceptsBoundaryLengths(int length)
  => Assert.Equal(new string('x',length),SkillReviewRules.Reason(" "+new string('x',length)+" "));
 [Fact]
 public void ReasonRejectsAboveLimit()
  => Assert.Throws<AssessmentException>(()=>SkillReviewRules.Reason(new string('x',2001)));
 [Theory]
 [InlineData("Open",-1,false)] [InlineData("published",-1,false)]
 [InlineData("Open",0,true)] [InlineData("Open",1,true)]
 [InlineData("suspended",-1,true)] [InlineData("cancelled",-1,true)]
 public void FutureRespectsVietnamTimeAndSessionState(string status,int minutesAfterStart,bool rejected)
 {
  var session=new Session{Status=status,SessionDate=new(2026,11,10),TimeSlot=new(){StartTime=new(18,0)}};
  var now=DateTimeOffset.Parse("2026-11-10T11:00:00Z").AddMinutes(minutesAfterStart);
  if(rejected)Assert.Throws<AssessmentException>(()=>SkillReviewRules.Future(session,now));
  else SkillReviewRules.Future(session,now);
 }
 [Theory]
 [InlineData(null)] [InlineData(-1)] [InlineData(101)]
 public void CorrectedScoreRejectsMissingOrOutOfRange(int? score)
  => Assert.Throws<AssessmentException>(()=>SkillReviewRules.CorrectedScore((short?)score));
 [Theory]
 [InlineData(0)] [InlineData(50)] [InlineData(100)]
 public void CorrectedScoreAcceptsInclusiveRange(short score)
  => Assert.Equal(score,SkillReviewRules.CorrectedScore(score));
}
