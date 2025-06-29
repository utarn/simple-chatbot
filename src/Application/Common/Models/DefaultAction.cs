using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class DefaultAction
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("uri")]
    public string Uri { get; set; }

    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("postback")]
    public string Postback { get; set; }

    [JsonPropertyName("displayText")]
    public string DisplayText { get; set; }
}