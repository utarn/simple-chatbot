namespace ChatbotApi.Domain.Entities;

public class ChatbotPlugin
{
    public int ChatbotId { get; set; }
    public Chatbot Chatbot { get; set; } = default!;
    public string PluginName { get; set; } = default!;
}
