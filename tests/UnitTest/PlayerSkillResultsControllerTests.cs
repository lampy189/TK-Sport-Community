using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TKSC.Controllers;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using Xunit;

namespace TKSC.Api.Tests.UnitTests;

public sealed class PlayerSkillResultsControllerTests
{
    private const long PlayerUserId = 42;
    private const long BadmintonSportId = 5;

    [Fact]
    public async Task Get_ReturnsOnlyPublishedResultsForCurrentPlayerAndRequestedSportInDescendingOrder()
    {
        await using var db = CreateDatabase();
        await SeedRatingHistoryAsync(db);
        var controller = CreateController(db);

        var result = await controller.Get(BadmintonSportId, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        var rows = Assert.IsAssignableFrom<IEnumerable<object>>(response.Value).ToArray();
        Assert.Equal(2, rows.Length);
        Assert.Equal(12L, GetProperty<long>(rows[0], "Id"));
        Assert.Equal(11L, GetProperty<long>(rows[1], "Id"));
        Assert.All(rows, row => Assert.Equal(BadmintonSportId, GetProperty<long>(row, "SportId")));
        Assert.Equal((short)80, GetProperty<short?>(rows[0], "Score"));
        Assert.Equal("B4", GetProperty<string>(rows[0], "Band"));
        Assert.DoesNotContain("InternalEvidence", rows[0]!.GetType().GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task Get_WhenNoResultsMatchRequestedSport_ReturnsEmptyList()
    {
        await using var db = CreateDatabase();
        await SeedRatingHistoryAsync(db);
        var controller = CreateController(db);

        var result = await controller.Get(sportId: 0, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<object>>(response.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-user-id")]
    public async Task Get_WhenUserIdClaimIsMissingOrInvalid_ReturnsUnauthorized(string? userIdClaim)
    {
        await using var db = CreateDatabase();
        var controller = CreateController(db, userIdClaim);

        var result = await controller.Get(BadmintonSportId, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static Sep490Context CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<Sep490Context>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new Sep490Context(options);
    }

    private static async Task SeedRatingHistoryAsync(Sep490Context db)
    {
        var playerUser = new User
        {
            Id = PlayerUserId,
            RoleId = 1,
            Email = "player@example.test",
            FullName = "Test Player",
            Status = "active"
        };
        var anotherUser = new User
        {
            Id = 84,
            RoleId = 1,
            Email = "another-player@example.test",
            FullName = "Another Player",
            Status = "active"
        };
        var badminton = new Sport { Id = BadmintonSportId, Code = "BM", Name = "Badminton" };
        var pickleball = new Sport { Id = 8, Code = "PK", Name = "Pickleball" };
        var player = new Player { Id = 1, UserId = PlayerUserId, User = playerUser };
        var anotherPlayer = new Player { Id = 2, UserId = anotherUser.Id, User = anotherUser };
        var badmintonProfile = new PlayerSportProfile
        {
            Id = 10, PlayerId = player.Id, SportId = badminton.Id, Player = player, Sport = badminton
        };
        var pickleballProfile = new PlayerSportProfile
        {
            Id = 20, PlayerId = player.Id, SportId = pickleball.Id, Player = player, Sport = pickleball
        };
        var anotherPlayerProfile = new PlayerSportProfile
        {
            Id = 30, PlayerId = anotherPlayer.Id, SportId = badminton.Id, Player = anotherPlayer, Sport = badminton
        };
        var badmintonRubric = new Rubric
        {
            Id = 1, SportId = badminton.Id, Sport = badminton, Version = "BM-CLB-V1", Name = "Badminton rubric"
        };
        var pickleballRubric = new Rubric
        {
            Id = 2, SportId = pickleball.Id, Sport = pickleball, Version = "PK-IPTPA-DUPR-V1", Name = "Pickleball rubric"
        };

        Assessment Assessment(long id, PlayerSportProfile profile, Rubric rubric, string status, long? sessionId) =>
            new()
            {
                Id = id,
                PlayerSportProfileId = profile.Id,
                PlayerSportProfile = profile,
                RubricId = rubric.Id,
                Rubric = rubric,
                AssessorId = playerUser.Id,
                Assessor = playerUser,
                Status = status,
                SessionId = sessionId
            };

        var assessments = new[]
        {
            Assessment(101, badmintonProfile, badmintonRubric, "published", 1001),
            Assessment(102, badmintonProfile, badmintonRubric, "published", 1002),
            Assessment(103, badmintonProfile, badmintonRubric, "draft", 1003),
            Assessment(104, badmintonProfile, badmintonRubric, "published", null),
            Assessment(105, pickleballProfile, pickleballRubric, "published", 1005),
            Assessment(106, anotherPlayerProfile, badmintonRubric, "published", 1006)
        };

        var histories = new[]
        {
            RatingHistory(11, badmintonProfile, playerUser, assessments[0], 70, "B3"),
            RatingHistory(12, badmintonProfile, playerUser, assessments[1], 80, "B4"),
            RatingHistory(13, badmintonProfile, playerUser, assessments[2], 90, "B5"),
            RatingHistory(14, badmintonProfile, playerUser, assessments[3], 95, "B5"),
            RatingHistory(15, pickleballProfile, playerUser, assessments[4], 60, "B3"),
            RatingHistory(16, anotherPlayerProfile, anotherUser, assessments[5], 75, "B4")
        };

        db.AddRange(playerUser, anotherUser, badminton, pickleball, player, anotherPlayer,
            badmintonProfile, pickleballProfile, anotherPlayerProfile, badmintonRubric, pickleballRubric);
        db.AddRange(assessments);
        db.AddRange(histories);
        await db.SaveChangesAsync();
    }

    private static PlayerRatingHistory RatingHistory(
        long id,
        PlayerSportProfile profile,
        User changedBy,
        Assessment assessment,
        short score,
        string band) =>
        new()
        {
            Id = id,
            PlayerSportProfileId = profile.Id,
            PlayerSportProfile = profile,
            ChangedBy = changedBy.Id,
            ChangedByNavigation = changedBy,
            AssessmentId = assessment.Id,
            Assessment = assessment,
            NewScore = score,
            NewBand = band,
            OldScore = (short)(score - 5),
            OldBand = "B2"
        };

    private static PlayerSkillResultsController CreateController(Sep490Context db, string? userIdClaim = "42")
    {
        var context = new DefaultHttpContext();
        if (userIdClaim is not null)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userIdClaim)], "UnitTest"));
        return new PlayerSkillResultsController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private static T? GetProperty<T>(object value, string name) =>
        (T?)value.GetType().GetProperty(name)?.GetValue(value);
}
