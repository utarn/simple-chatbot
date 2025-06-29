using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ModelInfoDto
{
    [JsonPropertyName("model_name")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("litellm_params")]
    public LiteLlmParamsDto? LiteLlmParams { get; set; }

    [JsonPropertyName("model_info")]
    public ModelInfoDetailsDto? ModelInfo { get; set; }
}
