using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class User1
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "mention";

    [JsonPropertyName("mentionee")]
    public MentioneeReply Mentionee { get; set; }
}