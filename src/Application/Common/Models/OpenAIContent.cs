using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "text", "image_url"

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("imageUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIImageUrl? ImageUrl { get; set; }
}
