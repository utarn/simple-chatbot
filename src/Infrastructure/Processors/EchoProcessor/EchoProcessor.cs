using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.Processors.EchoProcessor
{
    [Processor("Echo", "Echo Bot (Line): ตอบกลับข้อความพร้อมวันที่และเวลา")]
    public class EchoProcessor : ILineMessageProcessor
    {

        private readonly IDistributedCache _cache;
        private readonly ISystemService _systemService;
        private readonly IOpenAiService _openAiService;
        private readonly IApplicationDbContext _context;
        private readonly ILogger<EchoProcessor> _logger;

        // Product catalog with prices
        private static readonly Dictionary<string, decimal> ProductCatalog = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ปลากระป๋อง", 20 },
            { "ไข่ไก่", 7 },
            { "น้ำยาล้างจาน", 35 }
        };

        // Cache key for storing the product user is asking about
        private const string ProductStateKey = "product_state:{0}";

        public EchoProcessor(IDistributedCache cache, ISystemService systemService, IOpenAiService openAiService, IApplicationDbContext context, ILogger<EchoProcessor> logger)
        {
            _cache = cache;
            _systemService = systemService;
            _openAiService = openAiService;
            _context = context;
            _logger = logger;
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

        public async Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId, string replyToken, CancellationToken cancellationToken = default)
        {
            // Trim and normalize the message
            message = message?.Trim() ?? string.Empty;

            // Check if we have a pending product state for this user
            string productStateKey = string.Format(ProductStateKey, userId);
            string pendingProduct = await _cache.GetStringAsync(productStateKey, cancellationToken);

            // If we have a pending product state
            if (!string.IsNullOrEmpty(pendingProduct))
            {
                // Convert Thai numerals to Arabic before parsing
                string convertedMessage = ConvertThaiNumeralsToArabic(message);
                // Try to parse quantity from the message
                if (int.TryParse(convertedMessage, out int quantity) && quantity > 0)
                {
                    // Calculate total price
                    decimal unitPrice = ProductCatalog[pendingProduct];
                    decimal totalPrice = unitPrice * quantity;

                    // Clear the state
                    await _cache.RemoveAsync(productStateKey, cancellationToken);

                    // Return the price calculation
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage($"{pendingProduct} {quantity} ชิ้น ราคา {totalPrice} บาท")
                            }
                        }
                    };
                }
                else
                {
                    // The message is not a valid quantity, clear the state and treat as new query
                    await _cache.RemoveAsync(productStateKey, cancellationToken);
                }
            }
            else
            {
                // Only use LLM to find the best matching product if we don't have a pending product state
                string matchedProduct = await FindProductWithLLM(message, chatbotId, cancellationToken);

                // If we found a product
                if (matchedProduct != null)
                {
                    // Extract quantity if present
                    int quantity = ExtractQuantity(message);

                    if (quantity > 0)
                    {
                        // Calculate total price
                        decimal unitPrice = ProductCatalog[matchedProduct];
                        decimal totalPrice = unitPrice * quantity;

                        // Return the price calculation
                        return new LineReplyStatus
                        {
                            Status = 200,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                            {
                                new LineTextMessage($"{matchedProduct} {quantity} ชิ้น ราคา {totalPrice} บาท")
                            }
                            }
                        };
                    }
                    else
                    {
                        // No quantity found, store the product in cache and ask for quantity
                        await _cache.SetStringAsync(
                            productStateKey,
                            matchedProduct,
                            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
                            cancellationToken);

                        return new LineReplyStatus
                        {
                            Status = 200,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = new List<LineMessage>
                            {
                                new LineTextMessage($"คุณต้องการ {matchedProduct} กี่ชิ้น?")
                            }
                            }
                        };
                    }
                }
            }

            // No product found in the message
            return new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                   {
                       new LineTextMessage("ขออภัย ฉันไม่เข้าใจคำถาม กรุณาสอบถามเกี่ยวกับสินค้าที่มีอยู่ในร้านค้า")
                   }
                }
            };
        }

        // Method to use LLM to find the best matching product
        private async Task<string> FindProductWithLLM(string message, int chatbotId, CancellationToken cancellationToken)
        {
            // Extract just the product name part (remove quantity if present)
            string productName = System.Text.RegularExpressions.Regex.Replace(message, @"\d+", "").Trim();

            // Create a prompt for the LLM to find the best matching product
            string prompt = $@"Given the following list of products:
{string.Join(", ", ProductCatalog.Keys)}

What is the closest matching product to ""{productName}""?
Respond with only the exact product name from the list, or ""none"" if no close match exists.";

            // Get the chatbot's LLM key and model name from the database
            var chatbot = await _context.Chatbots.FindAsync(chatbotId);
            if (chatbot == null || string.IsNullOrEmpty(chatbot.LlmKey) || string.IsNullOrEmpty(chatbot.ModelName))
            {
                _logger.LogWarning("Chatbot or required fields missing for product matching");
                return null;
            }

            // Create an OpenAI request
            var request = new OpenAiRequest
            {
                Model = chatbot.ModelName,
                Messages = new List<OpenAIMessage>
                {
                    new OpenAIMessage
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                MaxTokens = 50,
                Temperature = 0.1m
            };

            // Use the OpenAI service to get a response from the LLM
            var response = await _openAiService.GetOpenAiResponseAsync(request, chatbot.LlmKey, cancellationToken);

            // Extract the product name from the response
            string result = response?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            // Return the product name if it exists in our catalog, otherwise null
            return ProductCatalog.ContainsKey(result) ? result : null;
        }

        // Helper method to extract quantity from a message
        private int ExtractQuantity(string message)
        {
            if (string.IsNullOrEmpty(message))
                return 0;

            // First convert any Thai numerals to Arabic numerals
            string convertedMessage = ConvertThaiNumeralsToArabic(message);

            // Look for numbers in the message (now in Arabic numerals)
            var numberStrings = System.Text.RegularExpressions.Regex.Matches(convertedMessage, @"\d+")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value);

            foreach (var numStr in numberStrings)
            {
                if (int.TryParse(numStr, out int quantity) && quantity > 0)
                {
                    return quantity;
                }
            }

            return 0; // No valid quantity found
        }

        // Helper method to convert Thai numerals to Arabic numerals
        private string ConvertThaiNumeralsToArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Dictionary mapping Thai numerals to Arabic numerals
            var thaiToArabic = new Dictionary<char, char>
            {
                { '๐', '0' },
                { '๑', '1' },
                { '๒', '2' },
                { '๓', '3' },
                { '๔', '4' },
                { '๕', '5' },
                { '๖', '6' },
                { '๗', '7' },
                { '๘', '8' },
                { '๙', '9' }
            };

            var result = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                result.Append(thaiToArabic.ContainsKey(c) ? thaiToArabic[c] : c);
            }
            return result.ToString();
        }

        // Helper method to find the closest matching product using fuzzy search
        private string FindClosestProduct(string query)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            // Convert query to lowercase for case-insensitive comparison
            string lowerQuery = query.ToLower();

            // First, check for exact matches
            foreach (var product in ProductCatalog.Keys)
            {
                if (product.ToLower() == lowerQuery)
                    return product;
            }

            // If no exact match, find the closest match based on string similarity
            string bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var product in ProductCatalog.Keys)
            {
                int distance = ComputeLevenshteinDistance(lowerQuery, product.ToLower());
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = product;
                }
            }

            // Return the closest match if it's reasonably similar (distance <= 2)
            return bestDistance <= 2 ? bestMatch : null;
        }

        // Helper method to compute Levenshtein distance between two strings
        private int ComputeLevenshteinDistance(string s1, string s2)
        {
            int[,] d = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= s2.Length; j++)
            {
                for (int i = 1; i <= s1.Length; i++)
                {
                    if (s1[i - 1] == s2[j - 1])
                        d[i, j] = d[i - 1, j - 1];
                    else
                        d[i, j] = Math.Min(Math.Min(
                            d[i - 1, j] + 1,    // deletion
                            d[i, j - 1] + 1),   // insertion
                            d[i - 1, j - 1] + 1 // substitution
                        );
                }
            }

            return d[s1.Length, s2.Length];
        }
    }
}