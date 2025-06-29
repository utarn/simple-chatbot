namespace ChatbotApi.Application.Common.Models;

public class ContentResult
{
    public byte[] Content { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}