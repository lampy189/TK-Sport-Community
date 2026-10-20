using System;
using System.Collections.Generic;

namespace be_da.Models;

public partial class User
{
    public long Id { get; set; }

    public long RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Status { get; set; } = null!;

    public DateTimeOffset? DeletedAt { get; set; }

    public string? GoogleSubject { get; set; }

    public virtual ICollection<AccountAuditLog> AccountAuditLogActors { get; set; } = new List<AccountAuditLog>();

    public virtual ICollection<AccountAuditLog> AccountAuditLogUsers { get; set; } = new List<AccountAuditLog>();

    public virtual ICollection<Approval> ApprovalApprovedByNavigations { get; set; } = new List<Approval>();

    public virtual ICollection<Approval> ApprovalRequestedByNavigations { get; set; } = new List<Approval>();

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Message> MessageRecipients { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> PaymentConfirmedByNavigations { get; set; } = new List<Payment>();

    public virtual ICollection<Payment> PaymentPayers { get; set; } = new List<Payment>();

    public virtual Player? Player { get; set; }

    public virtual ICollection<PlayerRatingHistory> PlayerRatingHistories { get; set; } = new List<PlayerRatingHistory>();

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();

    public virtual ICollection<RatingAppeal> RatingAppealReviewedByNavigations { get; set; } = new List<RatingAppeal>();

    public virtual ICollection<RatingAppeal> RatingAppealSubmittedByNavigations { get; set; } = new List<RatingAppeal>();

    public virtual ICollection<Refund> RefundApprovedByNavigations { get; set; } = new List<Refund>();

    public virtual ICollection<Refund> RefundInitiatedByNavigations { get; set; } = new List<Refund>();

    public virtual ICollection<Refund> RefundRecipients { get; set; } = new List<Refund>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SessionException> SessionExceptions { get; set; } = new List<SessionException>();

    public virtual ICollection<SessionReferee> SessionRefereeAssignedByNavigations { get; set; } = new List<SessionReferee>();

    public virtual ICollection<SessionReferee> SessionRefereeReferees { get; set; } = new List<SessionReferee>();

    public virtual ICollection<SessionStaff> SessionStaffs { get; set; } = new List<SessionStaff>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    public virtual ICollection<ShiftReconciliation> ShiftReconciliationApprovedByNavigations { get; set; } = new List<ShiftReconciliation>();

    public virtual ICollection<ShiftReconciliation> ShiftReconciliationStaffs { get; set; } = new List<ShiftReconciliation>();

    public virtual ICollection<SocialBookingOrder> SocialBookingOrders { get; set; } = new List<SocialBookingOrder>();

    public virtual ICollection<StockCount> StockCountApprovedByNavigations { get; set; } = new List<StockCount>();

    public virtual ICollection<StockCount> StockCountCountedByNavigations { get; set; } = new List<StockCount>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual ICollection<UserFcmToken> UserFcmTokens { get; set; } = new List<UserFcmToken>();
}
