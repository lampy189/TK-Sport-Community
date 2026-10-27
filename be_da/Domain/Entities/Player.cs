using System;
using System.Collections.Generic;

namespace TKSC.Domain.Entities;

public partial class Player
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? AvatarUrl { get; set; }

    public virtual ICollection<Friendship> FriendshipAddressees { get; set; } = new List<Friendship>();

    public virtual ICollection<Friendship> FriendshipRequesters { get; set; } = new List<Friendship>();

    public virtual ICollection<Invitation> InvitationInvitees { get; set; } = new List<Invitation>();

    public virtual ICollection<Invitation> InvitationInviters { get; set; } = new List<Invitation>();

    public virtual ICollection<PartnerRequest> PartnerRequestPartners { get; set; } = new List<PartnerRequest>();

    public virtual ICollection<PartnerRequest> PartnerRequestRequesters { get; set; } = new List<PartnerRequest>();

    public virtual ICollection<PlayerSportProfile> PlayerSportProfiles { get; set; } = new List<PlayerSportProfile>();

    public virtual ICollection<SessionException> SessionExceptions { get; set; } = new List<SessionException>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual User User { get; set; } = null!;
}
