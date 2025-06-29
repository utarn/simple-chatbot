namespace ChatbotApi.Domain.Entities;

public class Chatbot
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LineChannelAccessToken { get; set; }
    public string? GoogleChatServiceAccount { get; set; }
    public string? FacebookVerifyToken { get; set; }
    public string? FacebookAccessToken { get; set; }
    public string? LlmKey { get; set; }
    public string? SystemRole { get; set; }
    public string? Logo { get; set; }
    public string? ProtectedApiKey { get; set; }
    public int? MaxChunkSize { get; set; }
    public int? MaxOverlappingSize { get; set; }
    public int? TopKDocument { get; set; }
    public double? MaximumDistance { get; set; }
    public bool? ShowReference { get; set; }
    public int? HistoryMinute { get; set; }
    public bool AllowOutsideKnowledge { get; set; }
    public bool ResponsiveAgent { get; set; }
    public string? ModelName { get; set; }
    public bool EnableWebSearchTool { get; set; }
    public virtual ICollection<FlexMessage> FlexMessages { get; }
    public virtual ICollection<PreMessage> PredefineMessages { get; }
    public virtual ICollection<MessageHistory> MessageHistories { get; }
    public virtual ICollection<ChatbotPlugin> ChatbotPlugins { get; }
    public virtual ICollection<ImportError> ImportErrors { get; }
    public Chatbot()
    {
        PredefineMessages = new List<PreMessage>();
        MessageHistories = new List<MessageHistory>();
        FlexMessages = new List<FlexMessage>();
        ChatbotPlugins = new List<ChatbotPlugin>();
        ImportErrors = new List<ImportError>();
    }
}
