using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.Processors.ReadImageProcessor
{
    public class ReadImageProcessor : ILineMessageProcessor
    {
        public string Name => Systems.ReadImage;

        private readonly IOpenAiService _openAiService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<ReadImageProcessor> _logger;

        public ReadImageProcessor(IOpenAiService openAiService, IApplicationDbContext context, ILogger<ReadImageProcessor> logger)
        {
            _openAiService = openAiService;
            _context = context;
            _logger = logger;
        }

        public Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
            string replyToken, CancellationToken cancellationToken = default)
        {
            // ReadImageProcessor only supports image messages
            return Task.FromResult(new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage("กรุณาส่งรูปภาพที่ต้องการให้วิเคราะห์")
                    }
                }
            });
        }

        public async Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
            string replyToken, string accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the image content from LINE
                var content = await GetContentAsync(evt, accessToken, cancellationToken);
                if (content == null || string.IsNullOrEmpty(content.ContentType) || content.Content.Length == 0)
                {
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage("ไม่สามารถดึงข้อมูลรูปภาพได้ กรุณาลองใหม่อีกครั้ง")
                            }
                        }
                    };
                }

                // Convert image to base64
                string base64Image = Convert.ToBase64String(content.Content);
                string dataUrl = $"data:{content.ContentType};base64,{base64Image}";

                // Get chatbot information for LLM key and model
                var chatbot = await GetChatbotAsync(chatbotId, cancellationToken);
                if (chatbot == null || string.IsNullOrEmpty(chatbot.LlmKey) || string.IsNullOrEmpty(chatbot.ModelName))
                {
                    _logger.LogWarning("Chatbot or required fields missing for image analysis");
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage("ไม่สามารถประมวลผลรูปภาพได้ กรุณาตรวจสอบการตั้งค่าระบบ")
                            }
                        }
                    };
                }

                // Create OpenAI request with anonymous type for image analysis (similar to BuildOpenRouterRequestBody)
                var requestBody = new
                {
                    model = chatbot.ModelName,
                    messages = new object[]
                    {
                        new {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = "Analyze this image and describe what you see in detail." }
                            }
                        },
                        new {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "image_url", image_url = new { url = dataUrl } }
                            }
                        }
                    },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                // Send request to OpenAI service
                var response = await _openAiService.GetOpenAiResponseAsync(requestBody, chatbot.LlmKey, cancellationToken);

                // Extract response content
                string result = response?.Choices?.FirstOrDefault()?.Message?.Content ?? "ไม่สามารถวิเคราะห์รูปภาพได้";

                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage($"ผลการวิเคราะห์รูปภาพ:\n{result}")
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image in ReadImageProcessor for messageId: {MessageId}", messageId);
                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage("เกิดข้อผิดพลาดในการประมวลผลรูปภาพ กรุณาลองใหม่อีกครั้ง")
                        }
                    }
                };
            }
        }

        private async Task<ContentResult?> GetContentAsync(LineEvent evt, string accessToken, CancellationToken cancellationToken)
        {
            if (evt.Message?.Id == null)
            {
                _logger.LogError("Event message ID is null in GetContentAsync. Cannot get content");
                return null;
            }

            string messageId = evt.Message.Id;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string contentUrl = $"https://api-data.line.me/v2/bot/message/{messageId}/content";

            try
            {
                _logger.LogDebug("Fetching content from LINE API for messageId: {MessageId}", messageId);
                HttpResponseMessage response = await client.GetAsync(contentUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Failed to get content from LINE API for messageId {MessageId}. Status: {StatusCode}. Body: {Body}",
                        messageId, response.StatusCode, errorDetail);
                    return null;
                }

                byte[] contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                string? contentType = response.Content.Headers.ContentType?.MediaType;

                if (string.IsNullOrEmpty(contentType) || contentBytes.Length == 0)
                {
                    _logger.LogWarning("LINE API returned empty content or no content type for messageId: {MessageId}",
                        messageId);
                    return null;
                }

                _logger.LogDebug(
                    "Successfully fetched content ({ContentType}, {ContentLength} bytes) for messageId: {MessageId}",
                    contentType, contentBytes.Length, messageId);

                return new ContentResult { Content = contentBytes, ContentType = contentType };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content from LINE API for messageId: {MessageId}", messageId);
                return null;
            }
        }

        private async Task<Chatbot?> GetChatbotAsync(int chatbotId, CancellationToken cancellationToken)
        {
            return await _context.Chatbots.FindAsync(chatbotId);
        }
    }

    // Simple class to hold content result
    internal class ContentResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string? ContentType { get; set; }
    }
}