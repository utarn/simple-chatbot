using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; }
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}
