using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;

using ChatbotApi.Domain.Constants;
using System.Net;

namespace ChatbotApi.Infrastructure.Processors.GoldReportProcessor;

[Processor("GoldReport", "รายงานราคาทองคำ (Line)")]
public class GoldReportProcessor : ILineMessageProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOpenAiService _openAiService;
    private readonly IApplicationDbContext _context;

    public GoldReportProcessor(IHttpClientFactory httpClientFactory, IOpenAiService openAiService, IApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _openAiService = openAiService;
        _context = context;
    }
    private const string Prompt = """
  You are an expert in parsing HTML and extracting structured data. Your task is to extract gold price information from the provided HTML snippet and convert it into a JSON object.

**Strictly adhere to the following rules and JSON schema:**

1.  **Field Names:** Use *only* the English field names provided in the schema.
2.  **Data Types:** Ensure values match the data types implied by the schema (e.g., numbers for prices/amounts/revisions, strings for text).
3.  **Language:** Translate all content values (e.g., types of gold, directions) into concise, professional English.
4.  **Price Extraction:** Extract numerical values for `bidPrice` and `askPrice` as floating-point numbers.
5.  **Daily Change:**
    *   `amount`: Extract the numerical value next to "วันนี้" (e.g., 50) as an integer.
    *   `direction`: Determine based on the `css-sprite` class: if `css-sprite-up` is present, set to "up"; if `css-sprite-down` were present, set to "down".
6.  **Update Details:**
    *   `date`: Convert the Thai date (e.g., "14 มิถุนายน 2568") to ISO 8601 format "YYYY-MM-DD" (e.g., "2568-06-14").
    *   `time`: Extract the time (e.g., "09:00") as a string.
    *   `revision`: Extract the number from "(ครั้งที่ X)" as an integer.
7.  **Ignore Irrelevant HTML:** Disregard any `<span>` tags or other HTML elements that are not directly contributing to the requested data points (e.g., empty `<span>` tags, redundant number in the change row).

**Expected JSON Schema:**

```json
{
  "title": "string",
  "goldPurity": "string",
  "prices": {
    "goldBar": {
      "type": "string",
      "bidPrice": 0.0,
      "askPrice": 0.0
    },
    "ornamentalGold": {
      "type": "string",
      "bidPrice": 0.0,
      "askPrice": 0.0
    }
  },
  "dailyChange": {
    "amount": 0,
    "direction": "string"
  },
  "updateDetails": {
    "date": "YYYY-MM-DD",
    "time": "HH:MM",
    "revision": 0
  }
}
""";
    public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId, string replyToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new LineReplyStatus { Status = 404 };
        }

        var chatbot = await _context.Chatbots.FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);
        if (chatbot == null || string.IsNullOrEmpty(chatbot.LlmKey) || string.IsNullOrEmpty(chatbot.ModelName))
        {
            return new LineReplyStatus() { Status = 404, Raw = "Chatbot entity or required fields missing for gold report check" };
        }

        var client = _httpClientFactory.CreateClient("resilient");
        var response = await client.GetAsync("https://ทองคําราคา.com/", cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        html = System.Net.WebUtility.HtmlDecode(html);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var node = doc.DocumentNode.SelectSingleNode("//div[@class='divgta goldshopf']");
        string cleansedHtml = "";
        if (node != null)
        {
            cleansedHtml = node.InnerHtml;
        }

        var openAiRequest = new OpenAiRequest
        {
            Model = chatbot.ModelName,
            Messages = new List<OpenAIMessage>
            {
                new OpenAIMessage { Role = "user", Content = Prompt },
                new OpenAIMessage { Role = "user", Content = cleansedHtml }
            },
            MaxTokens = 2000,
            Temperature = 0.7m,
        };

        var llmResponse = await _openAiService.GetOpenAiResponseAsync(openAiRequest, chatbot.LlmKey, cancellationToken);

        var content = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return new LineReplyStatus { Status = 404 };
        }
        content = ExtractJsonFromMarkdown(content!);

        try
        {
            var goldReport = JsonSerializer.Deserialize<GoldReport>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var formattedMessage = FormatGoldReport(goldReport);

            return new LineReplyStatus()
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage()
                {
                    ReplyToken = replyToken,
                    Messages = [new LineTextMessage() { Type = "text", Text = formattedMessage }]
                }
            };
        }
        catch (JsonException)
        {
            // If deserialization fails, return the raw JSON content for debugging
            return new LineReplyStatus()
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage()
                {
                    ReplyToken = replyToken,
                    Messages = [new LineTextMessage() { Type = "text", Text = content }]
                }
            };
        }
    }

    private static string ExtractJsonFromMarkdown(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        // Check for ```json block
        var match = System.Text.RegularExpressions.Regex.Match(
            content,
            @"```json\s*(?<json>[\s\S]*?)\s*```",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        if (match.Success && match.Groups["json"].Success)
        {
            return match.Groups["json"].Value.Trim();
        }

        // Check for generic ``` block
        match = System.Text.RegularExpressions.Regex.Match(
            content,
            @"```\s*(?<json>[\s\S]*?)\s*```",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return match.Success && match.Groups["json"].Success
            ? match.Groups["json"].Value.Trim()
            : content;
    }
    private string FormatGoldReport(GoldReport? report)
    {
        if (report == null) return "Could not retrieve gold price information.";

        var sb = new StringBuilder();
        sb.AppendLine($"**{report.Title}**");
        sb.AppendLine($"*Purity: {report.GoldPurity}*");
        sb.AppendLine($"_Updated: {report.UpdateDetails?.Date} {report.UpdateDetails?.Time} (Rev. {report.UpdateDetails?.Revision})_");

        sb.AppendLine("\n--- Prices ---");
        if (report.Prices?.GoldBar != null)
        {
            sb.AppendLine($"**{report.Prices.GoldBar.Type}**");
            sb.AppendLine($"  - Bid: {report.Prices.GoldBar.BidPrice:N2}");
            sb.AppendLine($"  - Ask: {report.Prices.GoldBar.AskPrice:N2}");
        }
        if (report.Prices?.OrnamentalGold != null)
        {
            sb.AppendLine($"**{report.Prices.OrnamentalGold.Type}**");
            sb.AppendLine($"  - Bid: {report.Prices.OrnamentalGold.BidPrice:N2}");
            sb.AppendLine($"  - Ask: {report.Prices.OrnamentalGold.AskPrice:N2}");
        }

        sb.AppendLine("\n--- Daily Change ---");
        var directionEmoji = report.DailyChange?.Direction == "up" ? "⬆️" : "⬇️";
        sb.AppendLine($"{directionEmoji} {report.DailyChange?.Amount:N0}");

        return sb.ToString();
    }

    public Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken, string accessToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus { Status = 404 });
    }
}