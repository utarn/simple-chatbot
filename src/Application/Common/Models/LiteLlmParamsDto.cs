using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LiteLlmParamsDto
{
    [JsonPropertyName("merge_reasoning_content_in_choices")]
    public bool MergeReasoningContentInChoices { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public int? Weight { get; set; }
}
