namespace ChatbotApi.Infrastructure.Processors.CheckCheatOnlineProcessor;

public class UserIntention
{
    public bool IsGreeting { get; set; }
    public bool IsStoryTelling { get; set; }
    public bool IsQuestion { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BankAccount { get; set; }
    public string? WebsiteUrl { get; set; }
}
