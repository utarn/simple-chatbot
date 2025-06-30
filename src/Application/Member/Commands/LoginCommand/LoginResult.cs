namespace ChatbotApi.Application.Member.Commands.LoginCommand;

public class LoginResult
{
    public bool Success { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
