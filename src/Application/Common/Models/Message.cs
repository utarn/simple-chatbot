using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ChatbotApi.Application.Common.Models;

public class Message
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("quotedMessageId")]
    public string? QuotedMessageId { get; set; }

    [JsonPropertyName("quoteToken")]
    public string QuoteToken { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("emojis")]
    public List<Emoji>? Emojis { get; set; }

    [JsonPropertyName("mention")]
    public Mention Mention { get; set; }

    [JsonPropertyName("contentProvider")]
    public ContentProvider ContentProvider { get; set; }

    [JsonPropertyName("imageSet")]
    public ImageSet? ImageSet { get; set; }

    [JsonPropertyName("file")]
    public FileData? File { get; set; }
}