using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Chatbots.Queries.GetModelHarborModelsQuery;

public class ModelHarborModelDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class ModelHarborResponseDto
{
    [JsonPropertyName("data")]
    public List<ModelHarborModelDto> Data { get; set; } = new();
}
