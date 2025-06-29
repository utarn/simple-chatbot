using System.Text.Json.Serialization;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Enums;

namespace ChatbotApi.Application.Webhook.Commands.OpenAIWebhookCommand;

public class OpenAIWebhookCommand : IRequest<OpenAIResponse>
{
    [JsonPropertyName("chatbotId")]
    public int ChatbotId { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; }

    public string? ApiKey { get; set; }

}

public class OpenAIWebhookCommandHandler : IRequestHandler<OpenAIWebhookCommand, OpenAIResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IChatCompletion _chatCompletion;
    private readonly IEnumerable<IOpenAiMessageProcessor> _messageProcessors;
    public OpenAIWebhookCommandHandler(IApplicationDbContext context,
        IEnumerable<IOpenAiMessageProcessor> messageProcessors,
        IChatCompletion chatCompletion)
    {
        _context = context;
        _chatCompletion = chatCompletion;
        _messageProcessors = messageProcessors;
    }

    public async Task<OpenAIResponse> Handle(OpenAIWebhookCommand request, CancellationToken cancellationToken)
    {
        Chatbot chatbot = await _context.Chatbots
            .Include(c => c.ChatbotPlugins)
            .Include(c => c.PredefineMessages)
            .Where(c => c.Id == request.ChatbotId)
            .FirstAsync(cancellationToken);

        if (chatbot.ProtectedApiKey != request.ApiKey)
        {
            throw new ChatCompletionException(401, "Invalid API Key");
        }

        List<string> plugins = chatbot.ChatbotPlugins.Select(c => c.PluginName).ToList();

        List<OpenAIMessage> combinedMessages = new List<OpenAIMessage>();

        foreach (var predefineMessage in chatbot.PredefineMessages.OrderBy(p => p.Order))
        {
            if (!string.IsNullOrEmpty(predefineMessage.UserMessage))
            {
                combinedMessages.Add(new OpenAIMessage { Role = "user", Content = predefineMessage.UserMessage });
            }
            if (!string.IsNullOrEmpty(predefineMessage.AssistantMessage))
            {
                combinedMessages.Add(new OpenAIMessage { Role = "assistant", Content = predefineMessage.AssistantMessage });
            }

        }

        combinedMessages.AddRange(request.Messages);
        foreach (var processor in _messageProcessors)
        {
            if (plugins.Contains(processor.Name))
            {
                OpenAIResponse? processedResponse = await processor.ProcessOpenAiAsync(request.ChatbotId, combinedMessages, cancellationToken);
                if (processedResponse != null)
                {
                    return processedResponse;
                }
            }
        }
        string lastUserMessage = combinedMessages.LastOrDefault(m => m.Role == "user")?.Content ?? "";

        var chatCompletion = await _chatCompletion.ChatCompleteAsync(
            request.ChatbotId, "", // No userId needed for OpenAI
            lastUserMessage,
            request.Messages,
            MessageChannel.OpenAI);

        // Construct the OpenAI-compatible response
        var openAIResponse = new OpenAIResponse()
        {
            Id = Guid.NewGuid().ToString(),
            Object = "chat.completion",
            Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = request.Model,
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice()
                {
                    Index = 0,
                    Message = new OpenAIMessage()
                    {
                        Role = "assistant",
                        Content = chatCompletion.Message,
                        Annotations = chatCompletion.ReferenceItems.Select(r => new Annotation
                        {
                            Type = "url_citation",
                            UrlCitation = new UrlCitation()
                            {
                                StartIndex = r.StartIndex,
                                EndIndex = r.EndIndex,
                                Url = r.Url,
                                Title = r.Title,
                                LogoPath = r.LogoPath,
                                Text = r.Text
                            }
                        }).ToList(),
                    },
                    FinishReason = "stop"
                }
            },
            Usage = new OpenAIUsage()
            {
                PromptTokens = 0, // You'll need to calculate these if needed.
                CompletionTokens = 0,
                TotalTokens = 0  // ChatReport removed, set to 0 or calculate differently if needed
            }
        };

        await _context.SaveChangesAsync(cancellationToken);
        return openAIResponse;
    }

}
