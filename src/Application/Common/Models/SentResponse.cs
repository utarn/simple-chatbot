using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class SentResponse
{
    [JsonPropertyName("sentMessages")]
    public List<SentMessage> SentMessages { get; set; }
}