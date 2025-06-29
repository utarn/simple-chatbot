namespace ChatbotApi.Domain.Models;

public class GmailEmailInfo
{
    public string Id { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedDateTime { get; set; }
    public bool IsRead { get; set; }
    public List<string> To { get; set; } = new();
    public List<string> Cc { get; set; } = new();
    public string Snippet { get; set; } = string.Empty;
}