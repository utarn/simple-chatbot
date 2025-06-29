using ChatbotApi.Application.Common.Models;
using OpenAiService.Interfaces;
using Pgvector;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IOpenAiService
{
    // Get OpenAI response based on the request, usually used for chatbots.
    // apiKey and model Name are required and usually related to each Chatbot's LlmKey and ModelName.
    Task<OpenAIResponse?> GetOpenAiResponseAsync(OpenAiRequest request, string apiKey,
        CancellationToken cancellationToken = default, string? model = null);
    // Perform embedding on text, usually used for vector search or similarity.
    // The apiKey is required for authentication is the same as Chatbot's LlmKey
    Task<Vector?> CallEmbeddingsAsync(string text, string apiKey, CancellationToken cancellationToken);

    Task<List<string>> GetTextChunksFromFileAsync(string base64, string mimeType, string apiKey, string modelName,
        string? prompt = null,
        int? chunkSize = null, int? chunkOverlap = null, CancellationToken cancellationToken = default);

    List<string> SplitTextAsync(string text, string apiKey, int? chunkSize = null, int? chunkOverlap = null,
        CancellationToken cancellationToken = default);

    Task<string> CallSummaryAsync(string base64, string mimeType, string apiKey, string modelName,
        CancellationToken cancellationToken = default);

    Task<string> GetHtmlContentAsync(string url, string apiKey, string modelName,
        CancellationToken cancellationToken = default);

    Task<List<(string Text, string Value)>> GetModelsAsync(CancellationToken cancellationToken = default);
}
