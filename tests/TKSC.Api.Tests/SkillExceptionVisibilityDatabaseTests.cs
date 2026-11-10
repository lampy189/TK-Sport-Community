using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.DbContext;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class SkillExceptionVisibilityDatabaseTests
{
 [DatabaseFact]
 public async Task NoTicketPlayerIsVisibleOnlyForReviewInTheRequestVenue()
 {
  var connection=Environment.GetEnvironmentVariable("TKSC_TEST_SQL_CONNECTION")!;
  await using var db=new Sep490Context(new DbContextOptionsBuilder<Sep490Context>().UseSqlServer(connection).Options);
  await db.Database.OpenConnectionAsync();
  await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=N'service';");
  await using var tx=await db.Database.BeginTransactionAsync();
  try
  {
   var tag=Guid.NewGuid().ToString("N");
   var managerRole=await db.Roles.FirstAsync(r=>r.Code=="Manager");
   var refereeRole=await db.Roles.FirstAsync(r=>r.Code=="Referee");
   var playerRole=await db.Roles.FirstAsync(r=>r.Code=="Player");
   var manager=new User{Email=$"mgr-{tag}@example.invalid",FullName="Visibility Manager",RoleId=managerRole.Id,Status="Active"};
   var referee=new User{Email=$"ref-{tag}@example.invalid",FullName="Visibility Referee",RoleId=refereeRole.Id,Status="Active"};
   var subject=new Player{User=new User{Email=$"subject-{tag}@example.invalid",FullName="No ticket exception Player",RoleId=playerRole.Id,Status="Active"}};
   var unrelated=new Player{User=new User{Email=$"other-{tag}@example.invalid",FullName="No request Player",RoleId=playerRole.Id,Status="Active"}};
   var venue=new Venue{Name="Exception visibility "+tag,IsActive=true};
   var otherVenue=new Venue{Name="Other center "+tag,IsActive=true};
   db.Users.AddRange(manager,referee);db.Players.AddRange(subject,unrelated);db.Venues.AddRange(venue,otherVenue);
   await db.SaveChangesAsync();
   var sport=await db.Sports.FirstAsync();var slot=await db.TimeSlots.FirstAsync();
   var session=new Session{Court=new Court{VenueId=venue.Id,SportId=sport.Id,Name="Visibility court",IsActive=true},
    SportId=sport.Id,TimeSlotId=slot.Id,SessionDate=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),Band="B3",Status="Published",Capacity=8,CreatedBy=manager.Id};
   db.Sessions.Add(session);await db.SaveChangesAsync();
   db.SessionReferees.Add(new(){SessionId=session.Id,RefereeId=referee.Id,AssignedBy=manager.Id});
   var profile=new PlayerSportProfile{PlayerId=subject.Id,SportId=sport.Id,CurrentScore=30,CurrentBand="B2",RatingStatus="Rated"};
   db.PlayerSportProfiles.Add(profile);
   var request=new SessionException{SessionId=session.Id,PlayerId=subject.Id,Reason="Request before registration"};
   db.SessionExceptions.Add(request);await db.SaveChangesAsync();
   db.PlayerRatingHistories.Add(new(){PlayerSportProfileId=profile.Id,ChangedBy=manager.Id,NewScore=30,NewBand="B2"});
   await db.SaveChangesAsync();
   var subjectId=subject.Id;var userId=subject.UserId;var unrelatedId=unrelated.Id;var profileId=profile.Id;
   var sessionId=session.Id;var refereeId=referee.Id;
   Assert.False(await db.Tickets.AnyAsync(t=>t.PlayerId==subjectId));
   db.ChangeTracker.Clear();
   async Task Context(string role,long center)
   {
    await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_role',@value={role}");
    await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_set_session_context @key=N'tksc_venue',@value={center}");
   }
   foreach(var role in new[]{"Referee","Manager"})
   {
    await Context(role,venue.Id);
    var rows=await db.SessionExceptions.AsNoTracking()
     .Where(e=>e.SessionId==sessionId && (role=="Manager" || e.Session.SessionReferees.Any(r=>r.RefereeId==refereeId)))
     .Select(e=>new{e.Id,Name=e.Player.User.FullName,Band=e.Player.PlayerSportProfiles.Where(p=>p.SportId==sport.Id).Select(p=>p.CurrentBand).FirstOrDefault()})
     .ToListAsync();
    var row=Assert.Single(rows);
    Assert.Equal("No ticket exception Player",row.Name);Assert.Equal("B2",row.Band);
    Assert.True(await db.PlayerRatingHistories.AsNoTracking().AnyAsync(h=>h.PlayerSportProfileId==profileId));
    Assert.False(await db.Players.AsNoTracking().AnyAsync(p=>p.Id==unrelatedId));
    await Context(role,otherVenue.Id);
    Assert.False(await db.SessionExceptions.AsNoTracking().AnyAsync(e=>e.Id==request.Id));
    Assert.False(await db.Players.AsNoTracking().AnyAsync(p=>p.Id==subjectId));
    Assert.False(await db.Users.AsNoTracking().AnyAsync(u=>u.Id==userId));
    Assert.False(await db.PlayerSportProfiles.AsNoTracking().AnyAsync(p=>p.Id==profileId));
   }
   await Context("Staff",venue.Id);
   Assert.False(await db.Players.AsNoTracking().AnyAsync(p=>p.Id==subjectId));
  }
  finally
  {
   await tx.RollbackAsync();
   await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_set_session_context @key=N'tksc_role',@value=NULL; EXEC sys.sp_set_session_context @key=N'tksc_venue',@value=NULL;");
  }
 }
}
