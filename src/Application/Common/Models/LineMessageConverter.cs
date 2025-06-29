using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineMessageConverter : JsonConverter<LineMessage>
{
    public static JsonSerializerOptions Options = new JsonSerializerOptions
    {
        Converters = { new LineMessageConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public override LineMessage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            if (doc.RootElement.TryGetProperty("type", out JsonElement typeElement))
            {
                var type = typeElement.GetString();
                switch (type)
                {
                    case "text":
                        return JsonSerializer.Deserialize<LineTextMessage>(doc.RootElement.GetRawText(), options)!;
                    case "textV2":
                        return JsonSerializer.Deserialize<LineTextMessageV2>(doc.RootElement.GetRawText(), options)!;
                    case "sticker":
                        return JsonSerializer.Deserialize<LineStickerMessage>(doc.RootElement.GetRawText(), options)!;
                    case "image":
                        return JsonSerializer.Deserialize<LineImageMessage>(doc.RootElement.GetRawText(), options)!;
                    case "template":
                        return JsonSerializer.Deserialize<LineTemplateMessage>(doc.RootElement.GetRawText(), options)!;
                    default:
                        throw new NotSupportedException($"Unknown type: {type}");
                }
            }

            throw new JsonException("Missing type discriminator");
        }
    }

    public override void Write(Utf8JsonWriter writer, LineMessage value, JsonSerializerOptions options)
    {
        var writeOptions = new JsonSerializerOptions(options)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        JsonSerializer.Serialize(writer, value, value.GetType(), writeOptions);
    }
}