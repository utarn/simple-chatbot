using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class SentMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("quoteToken")]
    public string QuoteToken { get; set; } = null!;
}