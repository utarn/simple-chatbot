using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Source
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("groupId")]
    public string GroupId { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; }
}