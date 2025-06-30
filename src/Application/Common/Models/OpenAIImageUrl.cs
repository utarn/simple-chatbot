using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty; // URL of the image
}
