using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Annotation
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
    [JsonPropertyName("url_citation")]
    public UrlCitation UrlCitation { get; set; }
}
