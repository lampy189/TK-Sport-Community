using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;
using TKSC.Infrastructure.Persistence.Contexts;
namespace be_da.Infrastructure.Persistence;

public static class OwnerPrice
{
    // SQL-only function: baseline prices remain usable until an approved version becomes effective.
    [DbFunction("OwnerEffectivePrice", "dbo")]
    public static int Effective(string kind, long id, int baseline, DateTimeOffset at) => throw new NotSupportedException();
    [DbFunction("OwnerPriceVersion", "dbo")]
    public static long? Version(string kind, long id, DateTimeOffset at) => throw new NotSupportedException();

    public static async Task<Session?> Resolve(Sep490Context db, Task<Session?> load)
    {
        var session=await load;
        if(session is null || !db.Database.IsSqlServer()) return session;
        var at=DateTimeOffset.UtcNow;
        var quote=await db.Sessions.Where(s=>s.Id==session.Id).Select(s=>new { Price=Effective("session",s.Id,s.PriceVnd,at), Version=Version("session",s.Id,at) }).SingleAsync();
        var price=quote.Price;
        session.QuotedPriceVersionId=quote.Version;
        var property=db.Entry(session).Property(s=>s.PriceVnd);
        session.PriceVnd=price; property.OriginalValue=price; property.IsModified=false;
        return session;
    }
}
