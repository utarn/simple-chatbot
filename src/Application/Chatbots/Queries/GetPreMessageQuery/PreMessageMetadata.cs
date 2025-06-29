using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Queries.GetPreMessageQuery;

public class PreMessageMetadata
{
    [Localize(Value = "คำถาม")]
    public string? UserMessage { get; set; }

    [Localize(Value = "คำตอบ")]
    public string? AssistantMessage { get; set; } = default!;
}
