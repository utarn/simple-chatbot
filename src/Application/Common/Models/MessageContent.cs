using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class MessageContent
{
    public string Content { get; set; }
    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; } = new List<Annotation>();
}
