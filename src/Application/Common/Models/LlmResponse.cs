using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LlmResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("model")]
    public string? Model { get; set; }
    [JsonPropertyName("object")]
    public string? Object { get; set; }
    [JsonPropertyName("created")]
    public long? Created { get; set; }
    [JsonPropertyName("choices")]
    public List<OpenAIChoice> Choices { get; set; } = new List<OpenAIChoice>();
    [JsonPropertyName("usage")]
    public Usage? Usage { get; set; }
}
