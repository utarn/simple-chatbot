using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ContentProvider
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    
    [JsonPropertyName("originalContentUrl")]
    public string? OriginalContentUrl { get; set; }
    
    [JsonPropertyName("previewImageUrl")]
    public string? PreviewImageUrl { get; set; }
}
