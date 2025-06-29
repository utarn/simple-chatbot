using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Usage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
    [JsonPropertyName("estimated_cost")]
    public decimal EstimatedCost { get; set; }
}
