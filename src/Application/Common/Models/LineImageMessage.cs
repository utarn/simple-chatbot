using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineImageMessage : LineMessage
{
    [JsonPropertyName("originalContentUrl")]
    public string OriginalContentUrl { get; set; }

    [JsonPropertyName("previewImageUrl")]
    public string PreviewImageUrl { get; set; }

    public LineImageMessage()
    {
        Type = "image";
    }
}