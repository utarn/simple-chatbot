using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class DeliveryContext
{
    [JsonPropertyName("isRedelivery")]
    public bool IsRedelivery { get; set; }
}