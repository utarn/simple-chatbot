namespace ChatbotApi.Application.Common.Interfaces;

public interface ISuggestionProcessor
{
    public string Name { get; }

    Task<List<string>> ProcessResponse(string response, Chatbot chatbot, CancellationToken cancellationToken = default);
}
