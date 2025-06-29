using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Postback
{
    [JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonPropertyName("params")]
    public PostbackParams Params { get; set; }
}