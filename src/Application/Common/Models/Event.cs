using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class Event
{
    [JsonPropertyName("replyToken")]
    public string ReplyToken { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("source")]
    public Source Source { get; set; }

    [JsonPropertyName("webhookEventId")]
    public string WebhookEventId { get; set; }

    [JsonPropertyName("deliveryContext")]
    public DeliveryContext DeliveryContext { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("postback")]
    public Postback Postback { get; set; }
}