namespace be_da.Domain.Entities;

public sealed record Account(long Id, string Email, string FullName, string Role,
    string Status, string GoogleSubject, bool IsDeleted)
{
    public bool CanSignIn => !IsDeleted && Status == "Active";
}
