namespace be_da.Application.Accounts;
public static class LoginPortal
{
    public static bool IsValid(string? portal) => portal is null or "player" or "admin" or "staff";
    public static bool Allows(string? portal, string role) => portal switch
    {
        null => true,
        "player" => role == "Player",
        "admin" => role == "SystemAdmin",
        "staff" => role is "Referee" or "Staff" or "Manager" or "Owner",
        _ => false
    };
    public static string Path(string portal) => portal switch
    {
        "admin" => "/admin/login", "staff" => "/staff/login", _ => "/"
    };
}
