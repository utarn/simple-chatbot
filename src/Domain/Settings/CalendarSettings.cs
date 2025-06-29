namespace ChatbotApi.Domain.Settings;

public class CalendarSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = new[] { "https://www.googleapis.com/auth/calendar" };
}