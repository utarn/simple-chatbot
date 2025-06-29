using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineTextMessage : LineMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    public LineTextMessage()
    {
        Type = "text";
    }

    public LineTextMessage(string text)
    {
        Type = "text";
        Text = text;
    }
}