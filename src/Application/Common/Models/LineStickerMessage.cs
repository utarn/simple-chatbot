using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class LineStickerMessage : LineMessage
{
    [JsonPropertyName("packageId")]
    public string PackageId { get; set; }

    [JsonPropertyName("stickerId")]
    public string StickerId { get; set; }

    public LineStickerMessage()
    {
        Type = "sticker";
    }
}