using System.Collections.Generic;
using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Common.Models;

public class LinePushMessage
{
    public string To { get; set; } = null!;
    public List<LineMessage> Messages { get; set; } = new List<LineMessage>();
}