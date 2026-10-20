using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace be_da.Models;

public partial class Sep490Context : DbContext
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
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("server =(local); database = Sep490;uid=sa;pwd=123;TrustServerCertificate=True;Trusted_Connection=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountAuditLog>(entity =>
        {
            entity.ToTable("account_audit_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Actor).WithMany(p => p.AccountAuditLogActors)
                .HasForeignKey(d => d.ActorId)
                .HasConstraintName("FK_account_audit_logs_actor_id");

            entity.HasOne(d => d.User).WithMany(p => p.AccountAuditLogUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_account_audit_logs_user_id");
        });

        modelBuilder.Entity<Approval>(entity =>
        {
            entity.ToTable("approvals");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("entity_type");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ApprovalApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_approvals_approved_by");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.ApprovalRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_approvals_requested_by");
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.ToTable("assessments");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("assessment_type");
            entity.Property(e => e.AssessorId).HasColumnName("assessor_id");
            entity.Property(e => e.BandResult)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("band_result");
            entity.Property(e => e.PlayerSportProfileId).HasColumnName("player_sport_profile_id");
            entity.Property(e => e.RubricId).HasColumnName("rubric_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalScore).HasColumnName("total_score");

            entity.HasOne(d => d.Assessor).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.AssessorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assessments_assessor_id");

            entity.HasOne(d => d.PlayerSportProfile).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.PlayerSportProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assessments_player_sport_profile_id");

            entity.HasOne(d => d.Rubric).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.RubricId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assessments_rubric_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Assessments)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_assessments_session_id");
        });

        modelBuilder.Entity<AssessmentScore>(entity =>
        {
            entity.ToTable("assessment_scores");

            entity.HasIndex(e => new { e.AssessmentId, e.DimensionId }, "UX_assessment_scores_assessment_id_dimension_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.DimensionId).HasColumnName("dimension_id");
            entity.Property(e => e.Score).HasColumnName("score");

            entity.HasOne(d => d.Assessment).WithMany(p => p.AssessmentScores)
                .HasForeignKey(d => d.AssessmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assessment_scores_assessment_id");

            entity.HasOne(d => d.Dimension).WithMany(p => p.AssessmentScores)
                .HasForeignKey(d => d.DimensionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_assessment_scores_dimension_id");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("action");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("entity_type");
            entity.Property(e => e.NewValue).HasColumnName("new_value");
            entity.Property(e => e.OldValue).HasColumnName("old_value");

            entity.HasOne(d => d.Actor).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorId)
                .HasConstraintName("FK_audit_logs_actor_id");
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.ToTable("bank_transactions");

            entity.HasIndex(e => e.ExternalId, "UX_bank_transactions_external_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.ExternalId)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("external_id");
            entity.Property(e => e.MatchStatus)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("match_status");
            entity.Property(e => e.MatchedPaymentId).HasColumnName("matched_payment_id");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_code");

            entity.HasOne(d => d.MatchedPayment).WithMany(p => p.BankTransactions)
                .HasForeignKey(d => d.MatchedPaymentId)
                .HasConstraintName("FK_bank_transactions_matched_payment_id");
        });

        modelBuilder.Entity<Court>(entity =>
        {
            entity.ToTable("courts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.VenueId).HasColumnName("venue_id");

            entity.HasOne(d => d.Sport).WithMany(p => p.Courts)
                .HasForeignKey(d => d.SportId)
                .HasConstraintName("FK_courts_sport_id");

            entity.HasOne(d => d.Venue).WithMany(p => p.Courts)
                .HasForeignKey(d => d.VenueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_courts_venue_id");
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");

            entity.HasIndex(e => e.AddresseeId, "IX_friendships_addressee");

            entity.HasIndex(e => e.RequesterId, "IX_friendships_requester");

            entity.HasIndex(e => new { e.PlayerLowId, e.PlayerHighId }, "UX_friendships_active_pair")
                .IsUnique()
                .HasFilter("([status] IN ('Pending', 'Accepted'))");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddresseeId).HasColumnName("addressee_id");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())")
                .HasColumnName("created_at");
            entity.Property(e => e.PlayerHighId)
                .HasComputedColumnSql("(case when [requester_id]<[addressee_id] then [addressee_id] else [requester_id] end)", true)
                .HasColumnName("player_high_id");
            entity.Property(e => e.PlayerLowId)
                .HasComputedColumnSql("(case when [requester_id]<[addressee_id] then [requester_id] else [addressee_id] end)", true)
                .HasColumnName("player_low_id");
            entity.Property(e => e.RequesterId).HasColumnName("requester_id");
            entity.Property(e => e.RespondedAt)
                .HasPrecision(0)
                .HasColumnName("responded_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Addressee).WithMany(p => p.FriendshipAddressees)
                .HasForeignKey(d => d.AddresseeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_friendships_addressee");

            entity.HasOne(d => d.Requester).WithMany(p => p.FriendshipRequesters)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_friendships_requester");
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.ToTable("invitations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InviteeId).HasColumnName("invitee_id");
            entity.Property(e => e.InviterId).HasColumnName("inviter_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.Invitee).WithMany(p => p.InvitationInvitees)
                .HasForeignKey(d => d.InviteeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invitations_invitee_id");

            entity.HasOne(d => d.Inviter).WithMany(p => p.InvitationInviters)
                .HasForeignKey(d => d.InviterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invitations_inviter_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_invitations_order_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_invitations_session_id");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Invitations)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK_invitations_ticket_id");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasIndex(e => new { e.SenderId, e.RecipientId, e.Id }, "IX_messages_conversation");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");

            entity.HasOne(d => d.Recipient).WithMany(p => p.MessageRecipients)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_messages_recipient_id");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders)
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_messages_sender_id");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Channel)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("channel");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_type");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_notifications_user_id");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");

            entity.HasIndex(e => e.OrderCode, "UX_orders_order_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(255)
                .HasColumnName("customer_name");
            entity.Property(e => e.HandoverState)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("handover_state");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("order_code");
            entity.Property(e => e.PaymentState)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("payment_state");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");

            entity.HasOne(d => d.Staff).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orders_staff_id");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");

            entity.HasIndex(e => new { e.OrderId, e.ProductId }, "UX_order_items_order_id_product_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SubtotalVnd).HasColumnName("subtotal_vnd");
            entity.Property(e => e.UnitPriceVnd).HasColumnName("unit_price_vnd");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_order_id");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_order_items_product_id");
        });

        modelBuilder.Entity<PartnerRequest>(entity =>
        {
            entity.ToTable("partner_requests");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PartnerId).HasColumnName("partner_id");
            entity.Property(e => e.RequesterId).HasColumnName("requester_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Partner).WithMany(p => p.PartnerRequestPartners)
                .HasForeignKey(d => d.PartnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_partner_requests_partner_id");

            entity.HasOne(d => d.Requester).WithMany(p => p.PartnerRequestRequesters)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_partner_requests_requester_id");

            entity.HasOne(d => d.Session).WithMany(p => p.PartnerRequests)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_partner_requests_session_id");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");

            entity.HasIndex(e => e.ReferenceCode, "UX_payments_reference_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.ConfirmedBy).HasColumnName("confirmed_by");
            entity.Property(e => e.GoodsOrderId).HasColumnName("goods_order_id");
            entity.Property(e => e.Method)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("method");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PayerId).HasColumnName("payer_id");
            entity.Property(e => e.ReferenceCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_code");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.ConfirmedByNavigation).WithMany(p => p.PaymentConfirmedByNavigations)
                .HasForeignKey(d => d.ConfirmedBy)
                .HasConstraintName("FK_payments_confirmed_by");

            entity.HasOne(d => d.GoodsOrder).WithMany(p => p.Payments)
                .HasForeignKey(d => d.GoodsOrderId)
                .HasConstraintName("FK_payments_goods_order_id");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_payments_order_id");

            entity.HasOne(d => d.Payer).WithMany(p => p.PaymentPayers)
                .HasForeignKey(d => d.PayerId)
                .HasConstraintName("FK_payments_payer_id");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");

            entity.HasIndex(e => e.UserId, "UX_players_user_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(2048)
                .IsUnicode(false)
                .HasColumnName("avatar_url");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Gender)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("gender");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Player)
                .HasForeignKey<Player>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_players_user_id");
        });

        modelBuilder.Entity<PlayerRatingHistory>(entity =>
        {
            entity.ToTable("player_rating_history");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.NewBand)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("new_band");
            entity.Property(e => e.NewScore).HasColumnName("new_score");
            entity.Property(e => e.OldBand)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("old_band");
            entity.Property(e => e.OldScore).HasColumnName("old_score");
            entity.Property(e => e.PlayerSportProfileId).HasColumnName("player_sport_profile_id");

            entity.HasOne(d => d.Assessment).WithMany(p => p.PlayerRatingHistories)
                .HasForeignKey(d => d.AssessmentId)
                .HasConstraintName("FK_player_rating_history_assessment_id");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.PlayerRatingHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_player_rating_history_changed_by");

            entity.HasOne(d => d.PlayerSportProfile).WithMany(p => p.PlayerRatingHistories)
                .HasForeignKey(d => d.PlayerSportProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_player_rating_history_player_sport_profile_id");
        });

        modelBuilder.Entity<PlayerSportProfile>(entity =>
        {
            entity.ToTable("player_sport_profiles");

            entity.HasIndex(e => new { e.PlayerId, e.SportId }, "UX_player_sport_profiles_player_id_sport_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CurrentBand)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("current_band");
            entity.Property(e => e.CurrentScore).HasColumnName("current_score");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.RatingStatus)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("rating_status");
            entity.Property(e => e.SportId).HasColumnName("sport_id");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerSportProfiles)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_player_sport_profiles_player_id");

            entity.HasOne(d => d.Sport).WithMany(p => p.PlayerSportProfiles)
                .HasForeignKey(d => d.SportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_player_sport_profiles_sport_id");
        });

        modelBuilder.Entity<Policy>(entity =>
        {
            entity.ToTable("policies");

            entity.HasIndex(e => e.Code, "UX_policies_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.EffectiveFrom)
                .HasPrecision(0)
                .HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo)
                .HasPrecision(0)
                .HasColumnName("effective_to");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Policies)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_policies_approved_by");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");

            entity.HasIndex(e => e.Sku, "UX_products_sku").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("category");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Sku)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("sku");
            entity.Property(e => e.UnitPriceVnd).HasColumnName("unit_price_vnd");
        });

        modelBuilder.Entity<RatingAppeal>(entity =>
        {
            entity.ToTable("rating_appeals");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.PlayerSportProfileId).HasColumnName("player_sport_profile_id");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");

            entity.HasOne(d => d.Assessment).WithMany(p => p.RatingAppeals)
                .HasForeignKey(d => d.AssessmentId)
                .HasConstraintName("FK_rating_appeals_assessment_id");

            entity.HasOne(d => d.PlayerSportProfile).WithMany(p => p.RatingAppeals)
                .HasForeignKey(d => d.PlayerSportProfileId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rating_appeals_player_sport_profile_id");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.RatingAppealReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK_rating_appeals_reviewed_by");

            entity.HasOne(d => d.SubmittedByNavigation).WithMany(p => p.RatingAppealSubmittedByNavigations)
                .HasForeignKey(d => d.SubmittedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rating_appeals_submitted_by");
        });

        modelBuilder.Entity<ReconciliationItem>(entity =>
        {
            entity.ToTable("reconciliation_items");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualAmount).HasColumnName("actual_amount");
            entity.Property(e => e.Difference).HasColumnName("difference");
            entity.Property(e => e.ExpectedAmount).HasColumnName("expected_amount");
            entity.Property(e => e.ItemType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("item_type");
            entity.Property(e => e.ReconciliationId).HasColumnName("reconciliation_id");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");

            entity.HasOne(d => d.Reconciliation).WithMany(p => p.ReconciliationItems)
                .HasForeignKey(d => d.ReconciliationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_reconciliation_items_reconciliation_id");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.ToTable("refunds");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.InitiatedBy).HasColumnName("initiated_by");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.RecipientId).HasColumnName("recipient_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.RefundApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_refunds_approved_by");

            entity.HasOne(d => d.InitiatedByNavigation).WithMany(p => p.RefundInitiatedByNavigations)
                .HasForeignKey(d => d.InitiatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_refunds_initiated_by");

            entity.HasOne(d => d.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_refunds_payment_id");

            entity.HasOne(d => d.Recipient).WithMany(p => p.RefundRecipients)
                .HasForeignKey(d => d.RecipientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_refunds_recipient_id");

            entity.HasOne(d => d.Ticket).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.TicketId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_refunds_ticket_id");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");

            entity.HasIndex(e => e.Code, "UX_roles_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.ToTable("rubrics");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Version)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("version");

            entity.HasOne(d => d.Sport).WithMany(p => p.Rubrics)
                .HasForeignKey(d => d.SportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rubrics_sport_id");
        });

        modelBuilder.Entity<RubricDimension>(entity =>
        {
            entity.ToTable("rubric_dimensions");

            entity.HasIndex(e => new { e.RubricId, e.Code }, "UX_rubric_dimensions_rubric_id_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.RubricId).HasColumnName("rubric_id");
            entity.Property(e => e.WeightPercent).HasColumnName("weight_percent");

            entity.HasOne(d => d.Rubric).WithMany(p => p.RubricDimensions)
                .HasForeignKey(d => d.RubricId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_rubric_dimensions_rubric_id");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");

            entity.HasIndex(e => new { e.CourtId, e.SessionDate, e.TimeSlotId }, "UX_sessions_court_id_session_date_time_slot_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Band)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("band");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.CourtId).HasColumnName("court_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PriceVnd).HasColumnName("price_vnd");
            entity.Property(e => e.SessionDate).HasColumnName("session_date");
            entity.Property(e => e.SportId).HasColumnName("sport_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TimeSlotId).HasColumnName("time_slot_id");

            entity.HasOne(d => d.Court).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.CourtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sessions_court_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sessions_created_by");

            entity.HasOne(d => d.Sport).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.SportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sessions_sport_id");

            entity.HasOne(d => d.TimeSlot).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.TimeSlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_sessions_time_slot_id");
        });

        modelBuilder.Entity<SessionException>(entity =>
        {
            entity.ToTable("session_exceptions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.Reason)
                .HasMaxLength(255)
                .HasColumnName("reason");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.SessionExceptions)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_session_exceptions_approved_by");

            entity.HasOne(d => d.Player).WithMany(p => p.SessionExceptions)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_exceptions_player_id");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionExceptions)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_exceptions_session_id");
        });

        modelBuilder.Entity<SessionReferee>(entity =>
        {
            entity.ToTable("session_referees");

            entity.HasIndex(e => new { e.SessionId, e.RefereeId }, "UX_session_referees_session_id_referee_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.RefereeId).HasColumnName("referee_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.SessionRefereeAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_referees_assigned_by");

            entity.HasOne(d => d.Referee).WithMany(p => p.SessionRefereeReferees)
                .HasForeignKey(d => d.RefereeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_referees_referee_id");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionReferees)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_referees_session_id");
        });

        modelBuilder.Entity<SessionStaff>(entity =>
        {
            entity.ToTable("session_staff");

            entity.HasIndex(e => new { e.SessionId, e.StaffId }, "UX_session_staff_session_id_staff_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleInSession)
                .HasMaxLength(255)
                .HasColumnName("role_in_session");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionStaffs)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_staff_session_id");

            entity.HasOne(d => d.Staff).WithMany(p => p.SessionStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_session_staff_staff_id");
        });

        modelBuilder.Entity<ShiftReconciliation>(entity =>
        {
            entity.ToTable("shift_reconciliations");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualBank).HasColumnName("actual_bank");
            entity.Property(e => e.ActualCash).HasColumnName("actual_cash");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Difference).HasColumnName("difference");
            entity.Property(e => e.ExpectedBank).HasColumnName("expected_bank");
            entity.Property(e => e.ExpectedCash).HasColumnName("expected_cash");
            entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.ShiftReconciliationApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_shift_reconciliations_approved_by");

            entity.HasOne(d => d.Staff).WithMany(p => p.ShiftReconciliationStaffs)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_shift_reconciliations_staff_id");
        });

        modelBuilder.Entity<SocialBookingOrder>(entity =>
        {
            entity.ToTable("social_booking_orders");

            entity.HasIndex(e => e.OrderCode, "UX_social_booking_orders_order_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpiresAt)
                .HasPrecision(0)
                .HasColumnName("expires_at");
            entity.Property(e => e.OrderCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("order_code");
            entity.Property(e => e.PayerId).HasColumnName("payer_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");

            entity.HasOne(d => d.Payer).WithMany(p => p.SocialBookingOrders)
                .HasForeignKey(d => d.PayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_social_booking_orders_payer_id");
        });

        modelBuilder.Entity<Sport>(entity =>
        {
            entity.ToTable("sports");

            entity.HasIndex(e => e.Code, "UX_sports_code").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("code");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        modelBuilder.Entity<StockCount>(entity =>
        {
            entity.ToTable("stock_counts");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CountedBy).HasColumnName("counted_by");
            entity.Property(e => e.CountedQuantity).HasColumnName("counted_quantity");
            entity.Property(e => e.Difference).HasColumnName("difference");
            entity.Property(e => e.StockItemId).HasColumnName("stock_item_id");
            entity.Property(e => e.SystemQuantity).HasColumnName("system_quantity");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.StockCountApprovedByNavigations)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_stock_counts_approved_by");

            entity.HasOne(d => d.CountedByNavigation).WithMany(p => p.StockCountCountedByNavigations)
                .HasForeignKey(d => d.CountedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_counts_counted_by");

            entity.HasOne(d => d.StockItem).WithMany(p => p.StockCounts)
                .HasForeignKey(d => d.StockItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_counts_stock_item_id");
        });

        modelBuilder.Entity<StockItem>(entity =>
        {
            entity.ToTable("stock_items");

            entity.HasIndex(e => e.ProductId, "UX_stock_items_product_id").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.QuantityOnHand).HasColumnName("quantity_on_hand");
            entity.Property(e => e.ReorderLevel).HasColumnName("reorder_level");

            entity.HasOne(d => d.Product).WithOne(p => p.StockItem)
                .HasForeignKey<StockItem>(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_items_product_id");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.MovementType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("movement_type");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("reference_type");
            entity.Property(e => e.StockItemId).HasColumnName("stock_item_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_stock_movements_created_by");

            entity.HasOne(d => d.StockItem).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.StockItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_movements_stock_item_id");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");

            entity.HasIndex(e => e.TicketCode, "UX_tickets_ticket_code")
                .IsUnique()
                .HasFilter("([ticket_code] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AmountVnd).HasColumnName("amount_vnd");
            entity.Property(e => e.CheckedInBy).HasColumnName("checked_in_by");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TicketCode)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("ticket_code");

            entity.HasOne(d => d.CheckedInByNavigation).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.CheckedInBy)
                .HasConstraintName("FK_tickets_checked_in_by");

            entity.HasOne(d => d.Order).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tickets_order_id");

            entity.HasOne(d => d.Player).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.PlayerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tickets_player_id");

            entity.HasOne(d => d.Session).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_tickets_session_id");
        });

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.ToTable("time_slots");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.EndTime)
                .HasPrecision(0)
                .HasColumnName("end_time");
            entity.Property(e => e.Label)
                .HasMaxLength(255)
                .HasColumnName("label");
            entity.Property(e => e.StartTime)
                .HasPrecision(0)
                .HasColumnName("start_time");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "UX_users_email").IsUnique();

            entity.HasIndex(e => e.GoogleSubject, "UX_users_google_subject")
                .IsUnique()
                .HasFilter("([google_subject] IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeletedAt)
                .HasPrecision(0)
                .HasColumnName("deleted_at");
            entity.Property(e => e.Email)
                .HasMaxLength(320)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleSubject)
                .HasMaxLength(255)
                .IsUnicode(false)
                .UseCollation("Latin1_General_100_BIN2")
                .HasColumnName("google_subject");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_users_role_id");
        });

        modelBuilder.Entity<UserFcmToken>(entity =>
        {
            entity.ToTable("user_fcm_tokens");

            entity.HasIndex(e => e.UserId, "IX_user_fcm_tokens_user_id");

            entity.HasIndex(e => e.FcmToken, "UX_user_fcm_tokens_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeviceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("device_type");
            entity.Property(e => e.FcmToken)
                .HasMaxLength(512)
                .HasColumnName("fcm_token");
            entity.Property(e => e.UpdatedAt)
                .HasPrecision(0)
                .HasDefaultValueSql("(sysdatetimeoffset())")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserFcmTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_user_fcm_tokens_user");
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.ToTable("venues");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(500)
                .HasColumnName("address");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
