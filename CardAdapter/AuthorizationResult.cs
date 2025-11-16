namespace CardAdapter;

public class AuthorizationResult
{
    public string Status { get; set; } // Ex: "AUTHORIZED", "DECLINED", "REQUIRES_3DS"
    public string? DeclineReason { get; set; }
    public string? RedirectUrl { get; set; } // URL para o desafio 3DS
}