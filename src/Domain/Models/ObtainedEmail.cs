namespace ChatbotApi.Domain.Models;

public class ObtainedEmail
{
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
}