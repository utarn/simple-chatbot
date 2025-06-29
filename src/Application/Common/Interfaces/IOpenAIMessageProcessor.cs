using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IOpenAiMessageProcessor
{
    string Name { get; }
    Task<OpenAIResponse?> ProcessOpenAiAsync(int chatbotId, List<OpenAIMessage> messages, CancellationToken cancellationToken = default);
}
