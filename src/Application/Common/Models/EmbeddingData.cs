using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class EmbeddingData
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; }
}
