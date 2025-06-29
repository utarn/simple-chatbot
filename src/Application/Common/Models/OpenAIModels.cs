using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class OpenAIMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = new List<Annotation>();
}

public class OpenAIContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "text", "image_url"

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    [JsonPropertyName("imageUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OpenAIImageUrl? ImageUrl { get; set; }
}

public class OpenAIImageUrl
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty; // URL of the image
}

public class OpenAIResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("object")]
    public string Object { get; set; }
    [JsonPropertyName("created")]
    public int Created { get; set; }
    [JsonPropertyName("model")]
    public string Model { get; set; }
    [JsonPropertyName("choices")]
    public List<OpenAIChoice>? Choices { get; set; }
    [JsonPropertyName("usage")]
    public OpenAIUsage Usage { get; set; }
}

public class OpenAIChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }
    [JsonPropertyName("message")]
    public OpenAIMessage Message { get; set; }
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class OpenAIUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}
