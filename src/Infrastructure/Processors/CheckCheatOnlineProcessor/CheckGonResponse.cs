using System.Text.Json.Serialization;

namespace ChatbotApi.Infrastructure.Processors.CheckCheatOnlineProcessor;

public class CheckGonResponse
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("messageEn")]
    public string? MessageEn { get; set; }

    [JsonPropertyName("messageTh")]
    public string? MessageTh { get; set; }

    [JsonPropertyName("data")]
    public List<CheckGonCaseData>? Data { get; set; } // Key change: List of cases

    [JsonPropertyName("searchCount")]
    public int SearchCount { get; set; }

    // Helper property to easily check if data was found
    [JsonIgnore] // Don't serialize/deserialize this helper
    public bool HasData => Data != null && Data.Count > 0;
}
