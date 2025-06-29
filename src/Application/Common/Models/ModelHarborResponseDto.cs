using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ModelHarborResponseDto
{
    [JsonPropertyName("data")]
    public List<ModelHarborModelDto> Data { get; set; } = new();
}
