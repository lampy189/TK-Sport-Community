using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Session
{
    public long Id { get; set; }

    public long SportId { get; set; }

    public long CourtId { get; set; }

    public long TimeSlotId { get; set; }

    public DateOnly SessionDate { get; set; }

    public string? Band { get; set; }

    public short Capacity { get; set; }

    public int PriceVnd { get; set; }

    public string Status { get; set; } = null!;

    public long CreatedBy { get; set; }

    public virtual ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();

    public virtual Court Court { get; set; } = null!;

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public virtual ICollection<PartnerRequest> PartnerRequests { get; set; } = new List<PartnerRequest>();

    public virtual ICollection<SessionException> SessionExceptions { get; set; } = new List<SessionException>();

    public virtual ICollection<SessionReferee> SessionReferees { get; set; } = new List<SessionReferee>();

    public virtual ICollection<SessionStaff> SessionStaffs { get; set; } = new List<SessionStaff>();

    public virtual Sport Sport { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual TimeSlot TimeSlot { get; set; } = null!;
}
