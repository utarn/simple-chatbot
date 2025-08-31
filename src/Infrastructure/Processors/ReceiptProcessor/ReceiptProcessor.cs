using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using ContentResult = ChatbotApi.Infrastructure.Processors.LLamaPassportProcessor.ContentResult;

namespace ChatbotApi.Infrastructure.Processors.ReceiptProcessor;

[Processor("Receipt", "สร้างใบเสร็จรับเงิน (Line)")]
public class ReceiptProcessor : ILineMessageProcessor
{

    private readonly IApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReceiptProcessor> _logger;
    private readonly ReceiptGoogleSheetHelper _googleSheetHelper;
    private readonly IDistributedCache _cache;
    private readonly string _openRouterApiKey = "sk-**";

    private const string OpenRouterApiUrl = "https://openrouter.ai/api/v1/chat/completions";
    private const string OpenRouterModel = "meta-llama/llama-4-maverick";
    private const string SpreadsheetId = "1hcbJNz_-_I7fFQGD8Ppz-SLrMIoHI3Zt0mRjULbKepU";
    private const string Range = "Sheet1!A:C";

    public static string ReceiptPrompt =
        "Extract the following information from the receipt image as strictly as possible. " +
        "Return a strict JSON object with these fields: " +
        "\"receiptNumber\" (string, the receipt number), " +
        "\"amount\" (number, the total amount), " +
        "\"lineDisplayName\" (string, the LINE user's display name, if provided separately, otherwise leave blank). " +
        "If any field is missing or not found, set it to null. " +
        "Example: {\"receiptNumber\":\"123456\",\"amount\":299.50,\"lineDisplayName\":\"John Doe\"}";

    public ReceiptProcessor(
        IApplicationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ReceiptProcessor> logger,
        IConfiguration configuration,
        IDistributedCache cache)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _googleSheetHelper = new ReceiptGoogleSheetHelper(logger);
        _cache = cache;
    }

    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
        string replyToken, CancellationToken cancellationToken = default)
    {
        Chatbot? chatbot = await _context.Chatbots
            .FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);

        if (chatbot == null || chatbot.LineChannelAccessToken == null)
        {
            _logger.LogError("Chatbot with ID {ChatbotId} not found", chatbotId);
            return new LineReplyStatus { Status = 404 };
        }

        // Only process image messages
        if (evt.Message?.Type != "image")
        {
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage("กรุณาส่งรูปภาพใบเสร็จเท่านั้น")
                    }
                }
            };
        }

        var result = await ProcessReceiptAsync(evt, chatbot.LineChannelAccessToken, userId, cancellationToken);

        string replyText;
        if (result != null)
        {
            // Save to Google Sheet
            var rowData = new List<object>
            {
                result.ReceiptNumber ?? "-",
                result.Amount?.ToString("F2", CultureInfo.InvariantCulture) ?? "-",
                result.LineDisplayName ?? "-"
            };
            await _googleSheetHelper.AppendRowAsync(SpreadsheetId, Range, rowData, cancellationToken);

            replyText = BuildReceiptDisplayMessage(result) + "\nข้อมูลถูกบันทึกลง Google Sheet เรียบร้อยแล้ว";
        }
        else
        {
            replyText = "ไม่สามารถดึงข้อมูลจากใบเสร็จได้ กรุณาตรวจสอบรูปภาพและลองใหม่อีกครั้ง";
        }

        return new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineTextMessage(replyText)
                }
            }
        };
    }

    public async Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken, string accessToken, CancellationToken cancellationToken = default)
    {
        var result = await ProcessReceiptAsync(evt, accessToken, userId, cancellationToken);

        string replyText;
        if (result != null)
        {
            var rowData = new List<object>
            {
                result.ReceiptNumber ?? "-",
                result.Amount?.ToString("F2", CultureInfo.InvariantCulture) ?? "-",
                result.LineDisplayName ?? "-"
            };
            await _googleSheetHelper.AppendRowAsync(SpreadsheetId, Range, rowData, cancellationToken);

            replyText = BuildReceiptDisplayMessage(result) + "\nข้อมูลถูกบันทึกลง Google Sheet เรียบร้อยแล้ว";
        }
        else
        {
            replyText = "ไม่สามารถดึงข้อมูลจากใบเสร็จได้ กรุณาตรวจสอบรูปภาพและลองใหม่อีกครั้ง";
        }

        return new LineReplyStatus
        {
            Status = 200,
            ReplyMessage = new LineReplyMessage
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineTextMessage(replyText)
                }
            }
        };
    }

    private async Task<ReceiptResult?> ProcessReceiptAsync(LineEvent evt, string accessToken, string userId, CancellationToken cancellationToken)
    {
        var content = await GetContentAsync(evt, accessToken, cancellationToken);
        if (content == null || string.IsNullOrEmpty(content.ContentType) || content.Content.Length == 0)
        {
            return null;
        }

        string base64Content = Convert.ToBase64String(content.Content);
        string mimeType = content.ContentType;
        string imageDataUrl = $"data:{mimeType};base64,{base64Content}";

        // Get display name from LINE profile
        string? displayName = await GetLineProfileName(userId, accessToken, cancellationToken);

        var schemaProperties = new
        {
            receiptNumber = new { type = "string" },
            amount = new { type = "number" },
            lineDisplayName = new { type = "string" }
        };
        var schemaRequired = new[] { "receiptNumber", "amount", "lineDisplayName" };

        var requestBody = BuildOpenRouterRequestBody(ReceiptPrompt, schemaProperties, schemaRequired, imageDataUrl, displayName);

        var result = await CallOpenRouterApiAsync<ReceiptResult>(requestBody, evt.Message?.Id ?? "", cancellationToken);

        if (result != null)
        {
            // If model didn't fill lineDisplayName, use the fetched display name
            if (string.IsNullOrEmpty(result.LineDisplayName) && !string.IsNullOrEmpty(displayName))
            {
                result.LineDisplayName = displayName;
            }
        }

        return result;
    }

    private async Task<ContentResult?> GetContentAsync(LineEvent evt, string accessToken, CancellationToken cancellationToken)
    {
        if (evt.Message?.Id == null)
        {
            _logger.LogError("Event message ID is null in GetContentAsync. Cannot get content");
            return null;
        }

        string messageId = evt.Message.Id;

        HttpClient client = _httpClientFactory.CreateClient("resilient_nocompress");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

        string contentUrl = $"https://api-data.line.me/v2/bot/message/{messageId}/content";

        HttpResponseMessage response;
        byte[] contentBytes;
        string? contentType;

        try
        {
            _logger.LogDebug("Fetching content from LINE API for messageId: {MessageId}", messageId);
            response = await client.GetAsync(contentUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to get content from LINE API for messageId {MessageId}. Status: {StatusCode}. Body: {Body}",
                    messageId, response.StatusCode, errorDetail);
                return null;
            }

            contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = response.Content.Headers.ContentType?.MediaType;

            if (string.IsNullOrEmpty(contentType) || contentBytes.Length == 0)
            {
                _logger.LogWarning("LINE API returned empty content or no content type for messageId: {MessageId}",
                    messageId);
                return null;
            }

            _logger.LogDebug(
                "Successfully fetched content ({ContentType}, {ContentLength} bytes) for messageId: {MessageId}",
                contentType, contentBytes.Length, messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching content from LINE API for messageId: {MessageId}", messageId);
            return null;
        }

        return new ContentResult { Content = contentBytes, ContentType = contentType };
    }

    private object BuildOpenRouterRequestBody(string prompt, object schemaProperties, string[] schemaRequired, string imageDataUrl, string? displayName)
    {
        var messages = new List<object>
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = imageDataUrl } }
                }
            },
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = prompt }
                }
            }
        };

        // Optionally, provide displayName as context
        if (!string.IsNullOrEmpty(displayName))
        {
            messages.Add(new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = $"LINE display name: {displayName}" }
                }
            });
        }

        return new
        {
            messages = messages.ToArray(),
            model = OpenRouterModel,
            temperature = 1,
            max_completion_tokens = 512,
            top_p = 1,
            stream = false,
            stop = (object?)null,
            provider = new { sort = "throughput" },
            response_format = new
            {
                type = "json_object",
                schema = new
                {
                    type = "object",
                    properties = schemaProperties,
                    required = schemaRequired,
                    additionalProperties = false
                }
            }
        };
    }

    private async Task<T?> CallOpenRouterApiAsync<T>(object requestBody, string messageId, CancellationToken cancellationToken)
        where T : class
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("resilient_nocompress");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openRouterApiKey);

        string responseContent;
        try
        {
            var jsonRequest = JsonSerializer.Serialize(requestBody,
                new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
            _logger.LogDebug(
                "Sending request to OpenRouter API for messageId: {MessageId}. Request body (partial): {Request}",
                messageId, jsonRequest);

            HttpResponseMessage response = await httpClient.PostAsync(OpenRouterApiUrl,
                new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
                cancellationToken);

            responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Received response from OpenRouter API for messageId: {MessageId}. Status: {StatusCode}. Body (partial): {Response}",
                messageId, response.StatusCode, responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OpenRouter API call failed for messageId {MessageId} with status {StatusCode}. Response: {ResponseContent}",
                    messageId, response.StatusCode, responseContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during OpenRouter API call for messageId: {MessageId}", messageId);
            return null;
        }

        var openRouterResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);
        string? innerJsonContent = openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrEmpty(innerJsonContent))
        {
            _logger.LogError(
                "OpenRouter response is missing choices or message content for messageId: {MessageId}. Response: {ResponseContent}",
                messageId, responseContent);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(innerJsonContent);
            if (result == null)
            {
                _logger.LogError(
                    "Failed to deserialize OpenRouter response content to type {TypeName} for messageId: {MessageId}. Content: {Content}",
                    typeof(T).Name, messageId, innerJsonContent);
                return null;
            }

            return result;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx,
                "JSON deserialization failed to type {TypeName} for messageId: {MessageId}. Content: {Content}",
                typeof(T).Name, messageId, innerJsonContent);
            return null;
        }
    }

    private async Task<string?> GetLineProfileName(string userId, string accessToken, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("resilient_nocompress");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var url = $"https://api.line.me/v2/bot/profile/{userId}";
        var lineResponse = await client.GetAsync(url, cancellationToken);

        if (!lineResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get user profile: {StatusCode}", lineResponse.StatusCode);
            return null;
        }

        var lineContent = await lineResponse.Content.ReadAsStringAsync(cancellationToken);
        var json = JsonDocument.Parse(lineContent);
        return json.RootElement.GetProperty("displayName").GetString() ?? string.Empty;
    }

    private string BuildReceiptDisplayMessage(ReceiptResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ข้อมูลใบเสร็จ:");
        sb.AppendLine($"เลขที่ใบเสร็จ: {result.ReceiptNumber ?? "-"}");
        sb.AppendLine($"ยอดเงิน: {(result.Amount.HasValue ? result.Amount.Value.ToString("F2") : "-")}");
        sb.AppendLine($"ชื่อ LINE: {result.LineDisplayName ?? "-"}");
        return sb.ToString();
    }
}
