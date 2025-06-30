using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class WebSearchOption
{
    [JsonPropertyName("search_context_size")]
    public string? SearchContextSize { get; set; } = "medium";
}
