using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TKSC.Domain.Entities;

namespace TKSC.Infrastructure.DbContext;

public partial class Sep490Context : Microsoft.EntityFrameworkCore.DbContext
{
    public Sep490Context()
    {
    }

    public Sep490Context(DbContextOptions<Sep490Context> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountAuditLog> AccountAuditLogs { get; set; }

    public virtual DbSet<Approval> Approvals { get; set; }

    public virtual DbSet<Assessment> Assessments { get; set; }

    public virtual DbSet<AssessmentScore> AssessmentScores { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BankTransaction> BankTransactions { get; set; }

    public virtual DbSet<Court> Courts { get; set; }

    public virtual DbSet<Friendship> Friendships { get; set; }

    public virtual DbSet<Invitation> Invitations { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<PartnerRequest> PartnerRequests { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerRatingHistory> PlayerRatingHistories { get; set; }

    public virtual DbSet<PlayerSportProfile> PlayerSportProfiles { get; set; }

    public virtual DbSet<Policy> Policies { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<RatingAppeal> RatingAppeals { get; set; }

    public virtual DbSet<ReconciliationItem> ReconciliationItems { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Rubric> Rubrics { get; set; }

    public virtual DbSet<RubricDimension> RubricDimensions { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SessionException> SessionExceptions { get; set; }

    public virtual DbSet<SessionReferee> SessionReferees { get; set; }

    public virtual DbSet<SessionStaff> SessionStaffs { get; set; }

    public virtual DbSet<ShiftReconciliation> ShiftReconciliations { get; set; }

    public virtual DbSet<SocialBookingOrder> SocialBookingOrders { get; set; }

    public virtual DbSet<Sport> Sports { get; set; }

    public virtual DbSet<StockCount> StockCounts { get; set; }

    public virtual DbSet<StockItem> StockItems { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<TimeSlot> TimeSlots { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserFcmToken> UserFcmTokens { get; set; }

    public virtual DbSet<Venue> Venues { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            throw new InvalidOperationException("Supply configured DbContextOptions< Sep490Context >.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TKSC.Infrastructure.Configurations.Sep490ModelConfiguration.Configure(modelBuilder);
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
