using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Webhook.Commands.LineWebhookCommand;
using ChatbotApi.Domain.Enums;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IChatCompletion
{
    // If you create a Processor, use IOpenAiService to call OpenAI API instead
    // This is very specific for VectorChatService only.
    Task<ChatCompletionModel> ChatCompleteAsync(
        int chatbotId,
        string userId,
        string messageText,
        List<OpenAIMessage>? buffered,
        MessageChannel channel,
        int? max_tokens = null,
        decimal? temperature = null,
        CancellationToken cancellationToken = default);

}

public class ChatCompletionModel
{
    public string Message { get; set; }
    public List<ReferenceItem> ReferenceItems { get; set; }
    public List<string> Suggestions { get; set; }
}
