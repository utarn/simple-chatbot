using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = new List<Annotation>();
}
