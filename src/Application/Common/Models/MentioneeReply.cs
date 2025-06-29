using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class MentioneeReply
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "user";

    [JsonPropertyName("userId")]
    public string UserId { get; set; }
}