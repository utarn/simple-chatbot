using System.Text.Json.Serialization;


namespace ChatbotApi.Application.Common.Models;

public class OpenAiRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("messages")]
    public List<OpenAIMessage> Messages { get; set; }
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }
    [JsonPropertyName("temperature")]
    public decimal? Temperature { get; set; }

    [JsonPropertyName("files")]
    public List<FileAttachment> Files { get; set; }
    [JsonPropertyName("web_search_options")]
    public WebSearchOption? WebSearchOptions { get; set; }
}
