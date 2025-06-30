
using System.Text.Json.Serialization;

namespace ChatbotApi.Application.Common.Models;

public class ReceiptResult
{
    [JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

    [JsonPropertyName("amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("lineDisplayName")]
    public string? LineDisplayName { get; set; }
}
