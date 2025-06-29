using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

public class FacebookEntry
{
    [JsonPropertyName("messaging")]
    public List<FacebookReceivingMessage>? Messaging { get; init; }
}


public class FacebookReceivingMessage
{
    [JsonPropertyName("sender")]
    public FacebookUser? Sender { get; init; }

    [JsonPropertyName("message")]
    public FacebookBodyMessage? Message { get; init; }
}

public class FacebookReplyMessage
{
    [JsonPropertyName("messaging_type")]
    public string MessagingType { get; set; } = "RESPONSE";

    [JsonPropertyName("recipient")]
    public FacebookUser Recipient { get; set; }

    [JsonPropertyName("message")]
    public FacebookSendingMessage Message { get; set; }
}

public class FacebookUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class FacebookBodyMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("quick_replies")]
    public List<QuickReply>? QuickReplies { get; set; }  
    [JsonPropertyName("attachments")]
    public List<FacebookAttachment>? Attachment { get; set; }
}

public abstract class FacebookSendingMessage
{
    [JsonIgnore]
    public string? Type { get; set; }
}

public class TextFacebookMessage : FacebookSendingMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("quick_replies")]
    public List<QuickReply>? QuickReplies { get; set; }

    public TextFacebookMessage()
    {
        Type = "text";
    }

}

public class ImageFacebookMessage : FacebookSendingMessage
{
    [JsonPropertyName("attachments")]
    public List<FacebookAttachment>? Attachment { get; set; }

    public ImageFacebookMessage()
    {
        Type = "image";
    }
}

public class FacebookAttachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "image";

    [JsonPropertyName("payload")]
    public FacebookAttachmentPayload Payload { get; set; }
}

public class FacebookAttachmentPayload
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("is_reusable")]
    public bool IsReusable { get; set; } = true;
}

public class QuickReply
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "text";

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("payload")]
    public string Payload { get; set; }
}

public class FacebookReplyStatus
{
    public int Status { get; set; }
    public string Error { get; set; }
    public List<FacebookReplyMessage>? ReplyMessage { get; set; }
    public string? Raw { get; set; }
}


public class FacebookMessageConverter : JsonConverter<FacebookSendingMessage>
{
    public override FacebookSendingMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var rootElement = jsonDocument.RootElement;

        // Check the "type" property to determine the specific type
        if (rootElement.TryGetProperty("type", out var typeProperty))
        {
            var type = typeProperty.GetString();
            return type switch
            {
                "text" => JsonSerializer.Deserialize<TextFacebookMessage>(rootElement.GetRawText(), options),
                "image" => JsonSerializer.Deserialize<ImageFacebookMessage>(rootElement.GetRawText(), options),
                _ => throw new JsonException($"Unknown type: {type}")
            };
        }

        throw new JsonException("Type property is missing");
    }

    public override void Write(Utf8JsonWriter writer, FacebookSendingMessage value, JsonSerializerOptions options)
    {
        // Serialize based on the runtime type
        switch (value)
        {
            case TextFacebookMessage textMessage:
                JsonSerializer.Serialize(writer, textMessage, options);
                break;
            case ImageFacebookMessage imageMessage:
                JsonSerializer.Serialize(writer, imageMessage, options);
                break;
            default:
                throw new JsonException($"Unsupported type: {value.GetType()}");
        }
    }
}
