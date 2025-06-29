using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Mentionee
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    [JsonPropertyName("isSelf")]
    public bool IsSelf { get; set; }
}