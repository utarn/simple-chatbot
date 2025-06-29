namespace ChatbotApi.Domain.Entities;

public class MessageHistory
{
    public Chatbot ChatBot { get; set; } = default!;
    public int ChatBotId { get; set; }
    public DateTime Created { get; set; }
    public MessageChannel Channel { get; set; } =default!;
    public string Role { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsProcessed { get; set; }
}
