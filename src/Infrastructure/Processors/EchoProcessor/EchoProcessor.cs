using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.Processors.EchoProcessor
{
    public class EchoProcessor : ILineMessageProcessor
    {
        public string Name => Systems.Echo;

        private readonly ISystemService _systemService;
        private readonly ILogger<EchoProcessor> _logger;
        private readonly IApplicationDbContext _context;
        private readonly IOpenAiService _openAiService;

        public EchoProcessor(
            ISystemService systemService,
            ILogger<EchoProcessor> logger,
            IApplicationDbContext context,
            IOpenAiService openAiService)
        {
            _systemService = systemService;
            _logger = logger;
            _context = context;
            _openAiService = openAiService;
        }

        private class SentenceClassificationResult
        {
            public string OriginalMessage { get; set; } = string.Empty;
            public string SentenceType { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string Language { get; set; } = "th";
            public string Reasoning { get; set; } = string.Empty;
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        private async Task<SentenceClassificationResult> ClassifySentenceWithLLM(string message, int chatbotId, CancellationToken cancellationToken)
        {
            var result = new SentenceClassificationResult
            {
                OriginalMessage = message,
                Language = "th"
            };

            if (string.IsNullOrWhiteSpace(message))
            {
                result.SentenceType = "unknown";
                result.Confidence = 0.0;
                result.Reasoning = "Empty message";
                return result;
            }

            var prompt = $@"Analyze the following Thai text and determine if it is a statement (บอกเล่า) or a question (คำถาม).

Text: ""{message}""

Please respond in JSON format with the following structure:
{{
    ""sentenceType"": ""question"" or ""statement"",
    ""confidence"": 0.0-1.0,
    ""reasoning"": ""brief explanation in Thai""
}}

Consider:
- Thai question particles: ไหม, มั้ย, หรือไม่, หรือเปล่า, etc.
- Question words: ใคร, อะไร, ที่ไหน, เมื่อไหร่, ทำไม, อย่างไร, etc.
- Sentence structure and context
- Presence of question marks";

            var chatbot = await _context.Chatbots
                .FirstOrDefaultAsync(c => c.Id == chatbotId, cancellationToken);

            if (chatbot == null || string.IsNullOrEmpty(chatbot.LlmKey))
            {
                _logger.LogError("Chatbot {ChatbotId} does not have an AI token", chatbotId);
                // Fallback to simple heuristic
                var isQuestion = message.Contains("?") ||
                               message.ToLower().Contains("ไหม") ||
                               message.ToLower().Contains("มั้ย");
                result.SentenceType = isQuestion ? "question" : "statement";
                result.Confidence = 0.5;
                result.Reasoning = "Fallback classification due to missing AI token";
                return result;
            }

            var openAiRequest = new OpenAiRequest
            {
                Model = chatbot.ModelName ?? "openai/gpt-4.1",
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
                var llmResponse = await _openAiService.GetOpenAiResponseAsync(
                    openAiRequest,
                    chatbot.LlmKey,
                    cancellationToken,
                    chatbot.ModelName);

                var content = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (!string.IsNullOrEmpty(content) && content.Contains("{"))
                {
                    try
                    {
                        // Extract JSON from response
                        var startIndex = content.IndexOf('{');
                        var endIndex = content.LastIndexOf('}');
                        if (startIndex >= 0 && endIndex > startIndex)
                        {
                            var jsonString = content.Substring(startIndex, endIndex - startIndex + 1);
                            using var jsonDoc = JsonDocument.Parse(jsonString);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("sentenceType", out var typeProp))
                            {
                                var type = typeProp.GetString()?.ToLower();
                                result.SentenceType = type == "question" ? "question" : "statement";
                            }
                            else
                            {
                                result.SentenceType = "statement";
                            }

                            if (root.TryGetProperty("confidence", out var confProp) &&
                                confProp.TryGetDouble(out var confidenceValue))
                            {
                                result.Confidence = Math.Max(0.0, Math.Min(1.0, confidenceValue));
                            }
                            else
                            {
                                result.Confidence = 0.8;
                            }

                            if (root.TryGetProperty("reasoning", out var reasonProp))
                            {
                                result.Reasoning = reasonProp.GetString() ?? "LLM classification";
                            }
                        }
                    }
                    catch
                    {
                        // Fallback if JSON parsing fails
                        result.SentenceType = message.Contains("?") ? "question" : "statement";
                        result.Confidence = 0.7;
                        result.Reasoning = "Fallback classification due to JSON parsing error";
                    }
                }
                else
                {
                    // Fallback if no valid JSON response
                    result.SentenceType = message.Contains("?") ? "question" : "statement";
                    result.Confidence = 0.6;
                    result.Reasoning = "Fallback classification due to invalid LLM response";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying sentence with LLM");
                // Fallback to simple heuristic
                var isQuestion = message.Contains("?") ||
                               message.ToLower().Contains("ไหม") ||
                               message.ToLower().Contains("มั้ย");
                result.SentenceType = isQuestion ? "question" : "statement";
                result.Confidence = 0.5;
                result.Reasoning = "Fallback classification due to LLM error";
            }

            // Add metadata
            var cleanMessage = message.Trim();
            result.Metadata = new Dictionary<string, object>
            {
                ["messageLength"] = cleanMessage.Length,
                ["hasQuestionMark"] = cleanMessage.EndsWith("?"),
                ["wordCount"] = cleanMessage.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                ["classificationMethod"] = "LLM"
            };

            return result;
        }

        public async Task<LineReplyStatus> ProcessLineAsync(
            LineEvent evt,
            int chatbotId,
            string message,
            string userId,
            string replyToken,
            CancellationToken cancellationToken = default)
        {
            // Get current date/time in Thailand timezone
            var now = _systemService.UtcNow.AddHours(7); // UTC+7 for Thailand
            var thaiCulture = new CultureInfo("th-TH");
            string dateTimeThai = now.ToString("dddd d MMMM yyyy HH:mm:ss", thaiCulture);

            // Classify the sentence using LLM
            var classification = await ClassifySentenceWithLLM(message, chatbotId, cancellationToken);

            // Create JSON response
            var jsonResponse = JsonSerializer.Serialize(new
            {
                originalMessage = classification.OriginalMessage,
                sentenceType = classification.SentenceType,
                confidence = classification.Confidence,
                language = classification.Language,
                reasoning = classification.Reasoning,
                processedAt = dateTimeThai,
                metadata = classification.Metadata
            }, new JsonSerializerOptions { WriteIndented = true });

            string replyText = $"ข้อความ: {message}\n\nประเภทประโยค: {(classification.SentenceType == "question" ? "คำถาม" : "การบอกเล่า")}\nความมั่นใจ: {classification.Confidence:P0}\nเหตุผล: {classification.Reasoning}\n\nผลการวิเคราะห์ (JSON):\n```json\n{jsonResponse}\n```\n\nวันที่และเวลา: {dateTimeThai}";

            _logger.LogInformation("EchoProcessor classified message: {Message} as {Type} with {Confidence:P0} confidence using LLM",
                message, classification.SentenceType, classification.Confidence);

            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage { Text = replyText }
                    }
                }
            };
        }

        public Task<LineReplyStatus> ProcessLineImageAsync(
            LineEvent evt,
            int chatbotId,
            string messageId,
            string userId,
            string replyToken,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            // EchoProcessor does not support image messages
            return Task.FromResult(new LineReplyStatus
            {
                Status = 404,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage("EchoProcessor ไม่รองรับข้อความรูปภาพ")
                    }
                }
            });
        }
    }
}