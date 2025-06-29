using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class PostbackParams
{
    [JsonPropertyName("date")]
    public string Date { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("datetime")]
    public string Datetime { get; set; }
}