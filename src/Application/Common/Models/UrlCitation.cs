using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class UrlCitation
{
    [JsonPropertyName("end_index")]
    public int EndIndex { get; set; }
    [JsonPropertyName("start_index")]
    public int StartIndex { get; set; }
    [JsonPropertyName("title")]
    public string Title { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("logo_path")]
    public string LogoPath { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; }
}
