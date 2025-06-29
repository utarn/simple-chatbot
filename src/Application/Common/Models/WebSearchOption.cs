using System.Text.Json.Serialization;

namespace OpenAiService.Interfaces;

public class WebSearchOption
{
    [JsonPropertyName("search_context_size")]
    public string? SearchContextSize { get; set; } = "medium";
}
