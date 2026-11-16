using System.ComponentModel.DataAnnotations;

namespace be_da.Application.DTOs;
public sealed class ChatbotOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-5-mini";
    // Explicit publication allowlist: approval alone does not make an internal policy public.
    public string[] PublicPolicyCodes { get; set; } = [];
    public string MessengerUrl { get; set; } = "";
    public string ZaloUrl { get; set; } = "";
}
