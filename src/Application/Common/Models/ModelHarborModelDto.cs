using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ModelHarborModelDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}
