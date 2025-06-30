using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class FileAttachment
{
    [JsonPropertyName("base64")]
    public string Base64 { get; set; }

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; }
}
