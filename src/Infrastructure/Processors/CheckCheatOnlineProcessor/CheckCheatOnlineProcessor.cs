// src/Infrastructure/Processors/CheckCheatOnlineProcessor/CheckCheatOnlineProcessor.cs

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using Microsoft.Extensions.Configuration;

using Microsoft.EntityFrameworkCore;


namespace ChatbotApi.Infrastructure.Processors.CheckCheatOnlineProcessor;

public class CheckCheatOnlineProcessor : ILineMessageProcessor, IOpenAiMessageProcessor
{
    public string Name => "CheckCheatOnline";
    public string Description => "ตรวจสอบการโกงออนไลน์ (Line)";

    private readonly IApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CheckCheatOnlineProcessor> _logger;
    private readonly IOpenAiService _openAiService;
    private readonly string? _checkGonApiKey;
    private readonly string? _checkGonBaseUrl;
    private const string DefaultCheckGonBaseUrl = "https://open-api.checkgon.com";


    private const string CheckIntentionSchema =
        """
        {
          "type": "object",
          "properties": {
            "isGreeting": {
              "type": "boolean"
            },
            "isStoryTelling": {
              "type": "boolean"
            },
            "isQuestion": {
              "type": "boolean"
            },
            "phoneNumber": {
              "type": "string",
              "description": "Extract phone number only if explicitly mentioned. Normalize to digits only, e.g., '0812345678'."
            },
            "bankAccount": {
              "type": "string",
              "description": "Extract bank account number only if explicitly mentioned. Normalize to digits only, e.g., '1234567890'."
            },
            "websiteUrl": {
              "type": "string",
              "description": "Extract website URL only if explicitly mentioned, e.g., 'example.com' or 'http://example.com'."
            }
          },
          "required": [
            "isGreeting",
            "isStoryTelling",
            "isQuestion"
          ]
        }
        """;

    public CheckCheatOnlineProcessor(
        IApplicationDbContext context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<CheckCheatOnlineProcessor> logger,
        IOpenAiService openAiService)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _openAiService = openAiService;
        _checkGonApiKey = configuration["CheckGon:ApiKey"];
        _checkGonBaseUrl = configuration["CheckGon:BaseUrl"] ?? DefaultCheckGonBaseUrl;

        if (string.IsNullOrEmpty(_checkGonApiKey))
        {
            _logger.LogWarning("CheckGon API Key is not configured. CheckGon functionality will be disabled");
        }

        if (string.IsNullOrEmpty(_checkGonBaseUrl))
        {
            _logger.LogWarning("CheckGon Base URL is not configured. Using default: {DefaultUrl}",
                DefaultCheckGonBaseUrl);
            _checkGonBaseUrl = DefaultCheckGonBaseUrl; // Ensure it has a value
        }
    }

    public async Task<OpenAIResponse?> ProcessOpenAiAsync(int chatbotId, List<OpenAIMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var lastUserMessage = messages.LastOrDefault(m => m.Role == "user");
        if (lastUserMessage == null || string.IsNullOrWhiteSpace(lastUserMessage.Content))
        {
            _logger.LogInformation("No relevant user message found in the history for CheckCheatOnlineProcessor");
            return null;
        }

        string userMessageContent = lastUserMessage.Content;
        _logger.LogInformation("CheckCheatOnlineProcessor processing message for Chatbot {ChatbotId}: '{Message}'",
            chatbotId, userMessageContent);

        var (checkType, rawValue, formattedValue) =
        // 1. Get the last user message content
            await ProcessUserMessage(chatbotId, userMessageContent, cancellationToken);
        if (checkType == null || rawValue == null || formattedValue == null)
        {
            return null;
        }

        var replyMessage = await GetReplyMessage(chatbotId, checkType, rawValue, formattedValue, cancellationToken);
        if (replyMessage == null)
        {
            return null;
        }

        _logger.LogInformation("CheckCheatOnlineProcessor generated reply for Chatbot {ChatbotId}", chatbotId);
        return new OpenAIResponse
        {
            Id = $"cco-{Guid.NewGuid()}",
            Object = "chat.completion",
            Created = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = Name,
            Choices = new List<OpenAIChoice>
            {
                new OpenAIChoice
                {
                    Index = 0,
                    Message = new OpenAIMessage { Role = "assistant", Content = replyMessage },
                    FinishReason = "stop"
                }
            }
        };
    }

    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
        string replyToken, CancellationToken cancellationToken = default)
    {
        var (checkType, rawValue, formattedValue) = await ProcessUserMessage(chatbotId, message, cancellationToken);
        if (checkType == null || rawValue == null || formattedValue == null)
        {
            return new LineReplyStatus() { Status = 404 };
        }

        try
        {
            var replyMessage = await GetReplyMessage(chatbotId, checkType, rawValue, formattedValue, cancellationToken);
            if (replyMessage == null)
            {
                return new LineReplyStatus() { Status = 404 };
            }

            return new LineReplyStatus()
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage()
                {
                    ReplyToken = replyToken,
                    Messages = [new LineTextMessage() { Type = "text", Text = replyMessage }]
                }
            };
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("CheckGon API returned 404 (Not Found) for the searched item");
            return new LineReplyStatus() { Status = 404 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for chatbot {ChatbotId}", chatbotId);
            return new LineReplyStatus() { Status = 500 };
        }
    }

    private async Task<(string? checkType, string? rawValue, string? formattedValue)>
        ProcessUserMessage(int chatbotId, string message, CancellationToken cancellationToken)
    {
        // No more TokenAiSummarize
        UserIntention? detectedIntentionResult = await DetectIntentionAsync(chatbotId, message, cancellationToken);
        if (detectedIntentionResult == null)
        {
            _logger.LogWarning("Intention detection failed or returned null for chatbot {ChatbotId}", chatbotId);
            return (null, null, null);
        }

        string? checkType = null;
        string? rawValue = null;
        string? formattedValue = null;

        if (!string.IsNullOrWhiteSpace(detectedIntentionResult.PhoneNumber))
        {
            checkType = "phone-number";
            rawValue = detectedIntentionResult.PhoneNumber;
            formattedValue = FormatPhoneNumber(rawValue);
        }
        else if (!string.IsNullOrWhiteSpace(detectedIntentionResult.BankAccount))
        {
            checkType = "bank-account";
            rawValue = detectedIntentionResult.BankAccount;
            formattedValue = FormatBankAccount(rawValue);
        }
        else if (!string.IsNullOrWhiteSpace(detectedIntentionResult.WebsiteUrl))
        {
            checkType = "website";
            rawValue = detectedIntentionResult.WebsiteUrl;
            formattedValue = FormatWebsiteUrl(rawValue);
        }

        if (formattedValue == null || checkType == null || rawValue == null)
        {
            _logger.LogInformation(
                "No checkable entity (phone, bank account, URL) detected in message for chatbot {ChatbotId}",
                chatbotId);
        }

        return (checkType, rawValue, formattedValue);
    }

    private async Task<string?> GetReplyMessage(int chatbotId, string checkType, string rawValue, string formattedValue,
        CancellationToken cancellationToken)
    {
        var chatbot = await _context.Chatbots.FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);
        if (chatbot == null || string.IsNullOrEmpty(chatbot.LlmKey) || string.IsNullOrEmpty(chatbot.ModelName))
        {
            _logger.LogError("Chatbot entity or required fields missing for scam check");
            return null;
        }

        // Build the OpenAI prompt and schema
        var prompt = $"Check if this {checkType} is associated with scams: {formattedValue}. Respond in JSON: {{ \"isScam\": true/false, \"description\": \"...\", \"caseType\": \"...\", \"caseSeverity\": \"...\", \"informDate\": \"...\", \"damagePrice\": number }}";
        var modelName = chatbot.ModelName ?? "openai/gpt-4.1";
        var openAiRequest = new OpenAiRequest
        {
            Model = modelName,
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage
                {
                    Role = "user",
                    Content = prompt
                }
            }
        };

        try
        {
            var response = await _openAiService.GetOpenAiResponseAsync(openAiRequest, chatbot.LlmKey, cancellationToken, modelName);
            var content = response?.Choices?.FirstOrDefault()?.Message?.Content;
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("OpenAI scam check returned empty content");
                return null;
            }

            // Parse the JSON response
            ScamCheckResult? scamResult = null;
            try
            {
                scamResult = JsonSerializer.Deserialize<ScamCheckResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse OpenAI scam check JSON response: {Content}", content);
                return null;
            }

            if (scamResult == null)
            {
                _logger.LogWarning("OpenAI scam check returned null after deserialization");
                return null;
            }

            if (scamResult.IsScam)
            {
                // Map to CheckGonCaseData for BuildFoundReply compatibility
                var caseData = new CheckGonCaseData
                {
                    Description = scamResult.Description,
                    CaseType = scamResult.CaseType,
                    CaseSeverity = scamResult.CaseSeverity,
                    InformDate = DateTime.TryParse(scamResult.InformDate, out var dt) ? dt : (DateTime?)null,
                    DamagePrice = scamResult.DamagePrice
                };
                return BuildFoundReply(checkType, rawValue, caseData);
            }
            else
            {
                return BuildNotFoundReply(checkType, rawValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OpenAI scam check for {Type} {Value}", checkType, formattedValue);
            return null;
        }
    }

    // Helper class for OpenAI scam check JSON response
    private class ScamCheckResult
    {
        public bool IsScam { get; set; }
        public string? Description { get; set; }
        public string? CaseType { get; set; }
        public string? CaseSeverity { get; set; }
        public string? InformDate { get; set; }
        public decimal? DamagePrice { get; set; }
    }

    private string BuildFoundReply(string checkType, string value, CheckGonCaseData caseData)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🚨🚨🚨 พบข้อมูลแจ้งเตือนสำหรับ {GetThaiTypeName(checkType)}: {value}");
        sb.AppendLine($"รายละเอียด: {caseData.Description ?? "ไม่มีคำอธิบาย"}");
        if (caseData.InformDate.HasValue)
        {
            sb.AppendLine(
                $"วันที่แจ้ง: {caseData.InformDate.Value.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("th-TH"))}");
        }

        if (caseData.DamagePrice.HasValue && caseData.DamagePrice > 0)
        {
            sb.AppendLine($"มูลค่าความเสียหาย: {caseData.DamagePrice.Value:N0} บาท"); // Format as number
        }

        sb.AppendLine($"ประเภทเคส: {caseData.CaseType}");
        sb.AppendLine($"ความร้ายแรง: {(caseData.CaseSeverity == "high" ? "สูง" : "ปานกลาง")}");

        var formatValue = value;
        if (checkType == "phone-number")
        {
            formatValue = value.Replace("-", "");
        }

        var bytes = Encoding.UTF8.GetBytes(formatValue);
        switch (checkType)
        {
            case "phone-number":
                sb.AppendLine(
                    $"รายละเอียดเพิ่มเติมที่ https://checkgon.go.th/result/number/{Convert.ToBase64String(bytes)}");
                break;
            case "bank-account":
                sb.AppendLine(
                    $"รายละเอียดเพิ่มเติมที่ https://checkgon.go.th/result/account/{Convert.ToBase64String(bytes)}");
                break;
            case "website":
                sb.AppendLine(
                    $"รายละเอียดเพิ่มเติมที่ https://checkgon.go.th/result/shop/{Convert.ToBase64String(bytes)}");
                break;
        }

        return sb.ToString().Trim();
    }

    private string BuildNotFoundReply(string checkType, string value)
    {
        return
            $"ไม่พบข้อมูลแจ้งเตือนสำหรับ {GetThaiTypeName(checkType)}: {value} ในระบบ\nหากระบบค้นหาไม่ถูกต้องกรุณาระบบว่าเป็นเบอร์โทรศัพท์ บัญชีธนาคาร หรือชื่อธนาคาร\nหากคุณถูกโกงจากข้อมูลนี้ กรุณาช่วยเหลือผู้อื่นไม่ให้เป็นเหยื่ออีกด้วยการแจ้งเตือนที่ https://checkgon.go.th/";
    }

    private string GetThaiTypeName(string? type) => type switch
    {
        "phone-number" => "เบอร์โทรศัพท์",
        "bank-account" => "บัญชีธนาคาร",
        "website" => "เว็บไซต์",
        _ => "ข้อมูล"
    };


    private async Task<UserIntention?> DetectIntentionAsync(int chatbotId, string message, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"""
                Extract any explicitly mentioned Thai phone numbers (normalize to 10 digits starting with 0), bank account numbers (digits only), or website URLs.
                ถ้ามีชื่อค่ายโทรศัพท์ เช่น  ais dtac true nt  แปลว่าเป็นเบอร์โทรศัพท์
                ถ้ามีชื่อธนาคาร เช่น กรุงเทพ ไทยพาณิชย์ กรุงไทย กสิกร กรุงศรี ออมสิน GSB BBL SCB KTB KBANK BAY ฯลฯ แปลว่าเป็นเลขบัญชี
    
                User Message:
                "{message}"
                """;

            var openAiRequest = new OpenAiRequest
            {
                Model = "gemini-2.0-flash",
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                // Optionally, if schema is supported by your OpenAIService, add it here (custom property)
                // Otherwise, include schema in prompt or as a system message if supported
            };

            var llmResponse = await _openAiService.GetOpenAiResponseAsync(openAiRequest, "", cancellationToken);

            if (llmResponse == null || llmResponse.Choices == null || llmResponse.Choices.Count == 0)
            {
                _logger.LogWarning("OpenAIService returned no choices for intention detection");
                return null;
            }

            var content = llmResponse.Choices[0].Message.Content;

            // Try to parse as IntentionResponse (if the service returns structured JSON)
            UserIntention? result = null;
            try
            {
                var detectedIntention = JsonSerializer.Deserialize<IntentionResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                result = detectedIntention?.Result ?? new UserIntention();
            }
            catch
            {
                // If not JSON, fallback to empty
                result = new UserIntention();
            }

            // Basic validation on extracted data (optional but recommended)
            if (result.PhoneNumber != null &&
                !Regex.IsMatch(result.PhoneNumber, @"^0\d{9}$"))
            {
                _logger.LogWarning("AI returned potentially invalid phone format: {PhoneNumber}", result.PhoneNumber);
                result.PhoneNumber = null; // Invalidate if format is wrong
            }

            if (result.BankAccount != null &&
                !Regex.IsMatch(result.BankAccount, @"^\d+$"))
            {
                _logger.LogWarning("AI returned potentially invalid bank account format: {BankAccount}", result.BankAccount);
                result.BankAccount = null; // Invalidate if not digits
            }

            return result;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error deserializing intention detection response");
            return null; // Indicate failure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during intention detection");
            return null; // Indicate failure
        }
    }


    private async Task<CheckGonResponse?> CheckCheckGonApiAsync(string type, string value,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_checkGonApiKey))
        {
            _logger.LogWarning("CheckGon API Key is missing. Cannot perform check");
            return null;
        }

        if (string.IsNullOrEmpty(_checkGonBaseUrl))
        {
            _logger.LogError("CheckGon Base URL is not configured. Cannot perform check");
            return null;
        }


        var httpClient = _httpClientFactory.CreateClient("CheckGonClient");
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _checkGonApiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


        // Type and value are already formatted and encoded where needed by caller
        string requestUrl = $"{_checkGonBaseUrl}/api/v1/watchlist/findcase/{type}/{value}";
        _logger.LogInformation("Calling CheckGon API: {Url}", requestUrl);

        try
        {
            var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            _logger.LogDebug("CheckGon API Success Response: {ResponseContent}", responseContent);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonObject = JsonSerializer.Deserialize<CheckGonResponse>(responseContent, options);
            return jsonObject;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP Error calling CheckGon API. URL: {Url}", requestUrl);
            throw;
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error deserializing CheckGon API response. URL: {Url}", requestUrl);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling CheckGon API. URL: {Url}", requestUrl);
            throw;
        }
    }


    private string? FormatPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        string digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        // Expecting 10 digits starting with 0 for Thai numbers after AI normalization
        if (digits.Length == 10 && digits.StartsWith("0"))
        {
            // Format to XXX-XXX-XXXX
            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        }
        // Handle 9 digits (assuming mobile, prepend 0 - check if this assumption is valid)
        else if (digits.Length == 9 && !digits.StartsWith("0")) // e.g., 812345678
        {
            _logger.LogWarning("Formatting 9-digit phone number by prepending '0': {Original} -> 0{Digits}",
                phoneNumber, digits);
            string correctedDigits = "0" + digits;
            // Format to XXX-XXX-XXXX
            return
                $"{correctedDigits.Substring(0, 3)}-{correctedDigits.Substring(3, 3)}-{correctedDigits.Substring(6, 4)}";
            // Or handle 9 digits differently if needed, e.g. XX-XXX-XXXX (less common now)
            // return $"{digits.Substring(0, 2)}-{digits.Substring(2, 3)}-{digits.Substring(5, 4)}";
        }

        _logger.LogWarning("Invalid or non-standard phone number format received: {PhoneNumber}", phoneNumber);
        return null; // Return null if format is unexpected or cannot be reasonably formatted
    }

    private string? FormatBankAccount(string? bankAccount)
    {
        if (string.IsNullOrWhiteSpace(bankAccount)) return null;

        // Remove dashes and spaces, keep only digits
        string digits = new string(bankAccount.Where(char.IsDigit).ToArray());

        if (string.IsNullOrEmpty(digits))
        {
            _logger.LogWarning("Bank account number contained no digits: {BankAccount}", bankAccount);
            return null;
        }

        return digits;
    }

    private string? FormatWebsiteUrl(string? websiteUrl)
    {
        if (string.IsNullOrWhiteSpace(websiteUrl)) return null;

        // Basic cleanup - remove leading/trailing whitespace
        string url = websiteUrl.Trim();

        // Ensure it has a scheme for robust encoding, default to http if missing
        if (!url.Contains("://"))
        {
            url = "http://" + url; // Defaulting to http, consider https based on context if possible
        }

        // Validate if it's a somewhat valid URI structure before encoding
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
        {
            _logger.LogWarning("Invalid website URL format: {WebsiteUrl}", websiteUrl);
            return null; // Not a valid URL structure
        }


        // URL Encode the Host + Path/Query part for safety in the GET request path
        // The checkgon API seems to expect the *value* itself to be encoded if it contains special chars.
        // Let's encode the original (trimmed) string.
        return WebUtility.UrlEncode(websiteUrl.Trim());
    }


    public Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken,
        string accessToken, CancellationToken cancellationToken = default)
    {
        // No processing for image
        _logger.LogInformation("Received image message, CheckCheatOnlineProcessor does not process images");
        return Task.FromResult(new LineReplyStatus() { Status = 404 });
    }
}
