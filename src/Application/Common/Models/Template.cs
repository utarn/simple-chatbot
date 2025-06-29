using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Template
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("columns")]
    public List<Column> Columns { get; set; }

    [JsonPropertyName("imageAspectRatio")]
    public string ImageAspectRatio { get; set; }

    [JsonPropertyName("imageSize")]
    public string ImageSize { get; set; }

    [JsonPropertyName("thumbnailImageUrl")]
    public string ThumbnailImageUrl { get; set; }

    [JsonPropertyName("imageBackgroundColor")]
    public string ImageBackgroundColor { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("defaultAction")]
    public DefaultAction DefaultAction { get; set; }

    [JsonPropertyName("actions")]
    public List<LineAction> Actions { get; set; }
}