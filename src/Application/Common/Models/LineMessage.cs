using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public abstract class LineMessage
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("quoteToken")]
    public string? QuoteToken { get; set; }
}