using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ChatbotApi.Application.Common.Models;

public class LineReplyMessage
{
    [JsonPropertyName("replyToken")]
    public string ReplyToken { get; set; }

    [JsonPropertyName("messages")]
    public List<LineMessage> Messages { get; set; }
}