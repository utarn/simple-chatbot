using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Column
{
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