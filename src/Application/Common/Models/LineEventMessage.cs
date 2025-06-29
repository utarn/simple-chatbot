using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineEventMessage
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("packageId")]
    public string? PackageId { get; set; }

    [JsonPropertyName("stickerId")]
    public string? StickerId { get; set; }

    [JsonPropertyName("duration")]
    public long? Duration { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileSize")]
    public long? FileSize { get; set; }

    [JsonPropertyName("contentProvider")]
    public ContentProvider? ContentProvider { get; set; }

    [JsonPropertyName("imageSet")]
    public ImageSet? ImageSet { get; set; }

    [JsonPropertyName("emojis")]
    public List<Emoji>? Emojis { get; set; }

    [JsonPropertyName("mention")]
    public Mention? Mention { get; set; }
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}