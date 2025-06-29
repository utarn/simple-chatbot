using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("message")]
    public LineEventMessage? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("source")]
    public Source? Source { get; set; }

    [JsonPropertyName("replyToken")]
    public string ReplyToken { get; set; } = null!;

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("webhookEventId")]
    public string? WebhookEventId { get; set; }

    [JsonPropertyName("deliveryContext")]
    public DeliveryContext? DeliveryContext { get; set; }

    [JsonPropertyName("postback")]
    public Postback? Postback { get; set; }
}