namespace be_da.Domain.Entities;

public sealed record Account(long Id, string Email, string FullName, string Role,
<<<<<<< HEAD
    string Status, string GoogleSubject, bool IsDeleted)
=======
    string Status, string GoogleSubject, bool IsDeleted, long? VenueId = null)
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
{
    public bool CanSignIn => !IsDeleted && Status == "Active";
}
