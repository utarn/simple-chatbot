using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Substitution
{
    [JsonPropertyName("user1")]
    public User1 User1 { get; set; }

    [JsonPropertyName("user2")]
    public User1? User2 { get; set; }

    [JsonPropertyName("user3")]
    public User1? User3 { get; set; }
}