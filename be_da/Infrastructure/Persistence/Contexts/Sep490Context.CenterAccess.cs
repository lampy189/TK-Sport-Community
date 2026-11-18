using be_da.Application.Interfaces.External;
using be_da.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TKSC.Domain.Entities;
using TKSC.Domain.Exceptions;

namespace TKSC.Infrastructure.Persistence.Contexts;

public partial class Sep490Context
{
    private readonly IDataAccessContext? dataScope;
    private long? PersonnelVenue => CenterAccess.PersonnelVenue(dataScope);

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var model = modelBuilder;
        // These columns already exist in SQL. Map them so reads AND writes have a server-owned scope.
        model.Entity<Order>().Property<long?>("VenueId").HasColumnName("venue_id");
        model.Entity<Product>().Property<long?>("VenueId").HasColumnName("venue_id");
        model.Entity<StockItem>().Property<long?>("VenueId").HasColumnName("venue_id");
        model.Entity<ShiftReconciliation>().Property<long?>("VenueId").HasColumnName("venue_id");
        model.Entity<User>().Property<long?>("VenueId").HasColumnName("venue_id");

        // Player histories and ownership checks remain global. Personnel are always scoped,
        // including requests missing a center (sentinel -1 means deny, never allow-all).
        model.Entity<Venue>().HasQueryFilter(x => PersonnelVenue == null || x.Id == PersonnelVenue);
        model.Entity<Court>().HasQueryFilter(x => PersonnelVenue == null || x.VenueId == PersonnelVenue);
        model.Entity<Session>().HasQueryFilter(x => PersonnelVenue == null || x.Court.VenueId == PersonnelVenue);
        model.Entity<Product>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x, "VenueId") == PersonnelVenue);
        model.Entity<Order>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x, "VenueId") == PersonnelVenue);
        model.Entity<StockItem>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x, "VenueId") == PersonnelVenue);
        model.Entity<ShiftReconciliation>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x, "VenueId") == PersonnelVenue);
        model.Entity<OrderItem>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x.Order, "VenueId") == PersonnelVenue);
        model.Entity<Ticket>().HasQueryFilter(x => PersonnelVenue == null || x.Session.Court.VenueId == PersonnelVenue);
        model.Entity<SocialBookingOrder>().HasQueryFilter(x => PersonnelVenue == null || x.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<Payment>().HasQueryFilter(x => PersonnelVenue == null ||
            x.GoodsOrder != null && EF.Property<long?>(x.GoodsOrder, "VenueId") == PersonnelVenue ||
            x.Order != null && x.Order.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<BankTransaction>().HasQueryFilter(x => PersonnelVenue == null || x.MatchedPayment != null &&
            (x.MatchedPayment.GoodsOrder != null && EF.Property<long?>(x.MatchedPayment.GoodsOrder, "VenueId") == PersonnelVenue ||
             x.MatchedPayment.Order != null && x.MatchedPayment.Order.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue)));
        model.Entity<Refund>().HasQueryFilter(x => PersonnelVenue == null || x.Ticket.Session.Court.VenueId == PersonnelVenue);
        model.Entity<SessionReferee>().HasQueryFilter(x => PersonnelVenue == null || x.Session.Court.VenueId == PersonnelVenue);
        model.Entity<SessionStaff>().HasQueryFilter(x => PersonnelVenue == null || x.Session.Court.VenueId == PersonnelVenue);
        model.Entity<SessionException>().HasQueryFilter(x => PersonnelVenue == null || x.Session.Court.VenueId == PersonnelVenue);
        model.Entity<Assessment>().HasQueryFilter(x => PersonnelVenue == null || x.Session != null && x.Session.Court.VenueId == PersonnelVenue);
        model.Entity<AssessmentScore>().HasQueryFilter(x => PersonnelVenue == null || x.Assessment.Session != null && x.Assessment.Session.Court.VenueId == PersonnelVenue);
        model.Entity<RatingAppeal>().HasQueryFilter(x => PersonnelVenue == null || x.Assessment != null && x.Assessment.Session != null && x.Assessment.Session.Court.VenueId == PersonnelVenue);
        model.Entity<TicketChange>().HasQueryFilter(x => PersonnelVenue == null || Tickets.Any(t => t.Id == x.TicketId && t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<TicketFunding>().HasQueryFilter(x => PersonnelVenue == null || Tickets.Any(t => t.Id == x.TicketId && t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<TicketSupportMessage>().HasQueryFilter(x => PersonnelVenue == null || Tickets.Any(t => t.Id == x.TicketId && t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<StockMovement>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x.StockItem, "VenueId") == PersonnelVenue);
        model.Entity<StockCount>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x.StockItem, "VenueId") == PersonnelVenue);
        model.Entity<ReconciliationItem>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x.Reconciliation, "VenueId") == PersonnelVenue);
        model.Entity<Player>().HasQueryFilter(x => PersonnelVenue == null || x.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<PlayerSportProfile>().HasQueryFilter(x => PersonnelVenue == null || x.Player.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<PlayerRatingHistory>().HasQueryFilter(x => PersonnelVenue == null || x.PlayerSportProfile.Player.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
        model.Entity<User>().HasQueryFilter(x => PersonnelVenue == null || EF.Property<long?>(x, "VenueId") == PersonnelVenue || x.Player != null && x.Player.Tickets.Any(t => t.Session.Court.VenueId == PersonnelVenue));
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateCenterWritesAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await ValidateCenterWritesAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task ValidateCenterWritesAsync(CancellationToken ct)
    {
        var entries = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added && entry.Entity is Order or Product or StockItem or ShiftReconciliation && dataScope?.VenueId is long center)
                entry.Property("VenueId").CurrentValue = center;
        }
        if (PersonnelVenue is not long venue) return;
        if (venue <= 0) throw Denied();
        foreach (var entry in entries)
        {
            if (entry.Entity is TicketChange change &&
                (!await VisibleAsync(typeof(Ticket), change.TicketId, ct) || !await VisibleAsync(typeof(Session), change.TargetSessionId, ct))) throw Denied();
            if (entry.Entity is TicketFunding funding &&
                (!await VisibleAsync(typeof(Ticket), funding.TicketId, ct) || !await VisibleAsync(typeof(Payment), funding.PaymentId, ct))) throw Denied();
            if (entry.Entity is TicketSupportMessage message && !await VisibleAsync(typeof(Ticket), message.TicketId, ct)) throw Denied();
            if (entry.Metadata.GetQueryFilter() is not null && entry.State != EntityState.Added &&
                !await VisibleAsync(entry.Metadata.ClrType, (long)entry.Property("Id").OriginalValue!, ct))
                throw Denied();
            if (entry.Entity is Venue && (long)entry.Property("Id").CurrentValue! != venue) throw Denied();
            if (entry.Entity is Court or Order or Product or StockItem or ShiftReconciliation &&
                !Equals(entry.Property("VenueId").CurrentValue, venue)) throw Denied();
            // Query filters do not protect attached entities or reassigned foreign keys on SaveChanges.
            // Validate every scoped parent, including new rows pointing at a foreign center.
            foreach (var fk in entry.Metadata.GetForeignKeys().Where(f => f.PrincipalEntityType.GetQueryFilter() is not null && f.Properties.Count == 1))
            {
                if (entry.Property(fk.Properties[0].Name).CurrentValue is not long id) continue;
                var newParent = entries.Any(e => e.State == EntityState.Added && e.Metadata == fk.PrincipalEntityType && Equals(e.Property("Id").CurrentValue, id));
                if (!newParent && !await VisibleAsync(fk.PrincipalEntityType.ClrType, id, ct)) throw Denied();
            }
        }
    }

    private Task<bool> VisibleAsync(Type type, long id, CancellationToken ct) =>
        (Task<bool>)typeof(Sep490Context).GetMethod(nameof(VisibleEntityAsync), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(type).Invoke(this, [id, ct])!;
    private Task<bool> VisibleEntityAsync<T>(long id, CancellationToken ct) where T : class =>
        Set<T>().AsNoTracking().AnyAsync(x => EF.Property<long>(x, "Id") == id, ct);
    private static PaymentException Denied() => new(403, "Không có quyền truy cập dữ liệu của cơ sở này.");
}
