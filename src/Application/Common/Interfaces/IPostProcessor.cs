namespace ChatbotApi.Application.Common.Interfaces;

public interface IPostProcessor
{
    public string Name { get; }
    
    public Task<string> ProcessResponse(string response, Chatbot chatbot, string userId, CancellationToken cancellationToken = default);
}
