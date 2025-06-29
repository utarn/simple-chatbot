using System.Collections.Generic;

namespace ChatbotApi.Application.Common.Models;

public class LineSendResponse
{
    public int Status { get; set; }
    public string? Error { get; set; }
    public List<SentMessageData> SentMessages { get; set; } = new List<SentMessageData>();
    public List<ContentResult> ContentResults { get; set; } = new List<ContentResult>();
}