using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ModelInfoResponseDto
{
    [JsonPropertyName("data")]
    public List<ModelInfoDto> Data { get; set; } = new();
}
