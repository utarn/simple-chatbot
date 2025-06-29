using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineTemplateMessage : LineMessage
{
    [JsonPropertyName("altText")]
    public string AltText { get; set; }

    [JsonPropertyName("template")]
    public Template Template { get; set; }

    public LineTemplateMessage()
    {
        Type = "template";
    }
}