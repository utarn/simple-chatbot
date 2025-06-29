using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetChatbotQuery;

public class ChatbotMetadata
{
    [Localize(Value = "ชื่อบอท")]
    public string? Name { get; set; }
}
