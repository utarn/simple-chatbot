using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class EmbeddingsResponse
{
    [JsonPropertyName("data")]
    public List<EmbeddingData> Data { get; set; }
}
