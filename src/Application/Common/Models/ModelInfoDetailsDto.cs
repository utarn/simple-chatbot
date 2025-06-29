using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ModelInfoDetailsDto
{
    [JsonPropertyName("input_cost_per_token")]
    public double? InputCostPerToken { get; set; }

    [JsonPropertyName("output_cost_per_token")]
    public double? OutputCostPerToken { get; set; }

    [JsonPropertyName("output_cost_per_reasoning_token")]
    public double? OutputCostPerReasoningToken { get; set; }

    [JsonPropertyName("input_cost_per_query")]
    public double? InputCostPerQuery { get; set; }

    [JsonPropertyName("cache_read_input_token_cost")]
    public double? CacheReadInputTokenCost { get; set; }

    [JsonPropertyName("input_cost_per_token_batches")]
    public double? InputCostPerTokenBatches { get; set; }

    [JsonPropertyName("output_cost_per_token_batches")]
    public double? OutputCostPerTokenBatches { get; set; }

    [JsonPropertyName("input_cost_per_token_above_200k_tokens")]
    public double? InputCostPerTokenAbove200kTokens { get; set; }

    [JsonPropertyName("output_cost_per_token_above_200k_tokens")]
    public double? OutputCostPerTokenAbove200kTokens { get; set; }

    [JsonPropertyName("max_input_tokens")]
    public int? MaxInputTokens { get; set; }

    [JsonPropertyName("max_output_tokens")]
    public int? MaxOutputTokens { get; set; }

    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("supports_system_messages")]
    public bool? SupportsSystemMessages { get; set; }

    [JsonPropertyName("supports_response_schema")]
    public bool? SupportsResponseSchema { get; set; }

    [JsonPropertyName("supports_vision")]
    public bool? SupportsVision { get; set; }

    [JsonPropertyName("supports_function_calling")]
    public bool? SupportsFunctionCalling { get; set; }

    [JsonPropertyName("supports_tool_choice")]
    public bool? SupportsToolChoice { get; set; }

    [JsonPropertyName("supports_prompt_caching")]
    public bool? SupportsPromptCaching { get; set; }

    [JsonPropertyName("supports_pdf_input")]
    public bool? SupportsPdfInput { get; set; }

    [JsonPropertyName("supports_native_streaming")]
    public bool? SupportsNativeStreaming { get; set; }

    [JsonPropertyName("supports_web_search")]
    public bool? SupportsWebSearch { get; set; }

    [JsonPropertyName("supports_embedding_image_input")]
    public bool? SupportsEmbeddingImageInput { get; set; }

    [JsonPropertyName("supported_openai_params")]
    public List<string>? SupportedOpenaiParams { get; set; }
}
