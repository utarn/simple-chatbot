namespace ChatbotApi.Domain.Settings;

public class DriveSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = new[] { "https://www.googleapis.com/auth/drive" };
}