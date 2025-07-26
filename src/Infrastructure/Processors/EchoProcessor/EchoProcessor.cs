using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.Processors.EchoProcessor
{
    public class EchoProcessor : ILineMessageProcessor
    {
        public string Name => Systems.Echo;

        private readonly IDistributedCache _cache;
        private readonly ISystemService _systemService;

        // Product catalog with prices
        private static readonly Dictionary<string, decimal> ProductCatalog = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ปลากระป๋อง", 20 },
            { "ไข่ไก่", 7 },
            { "น้ำยาล้างจาน", 35 }
        };

        // Cache key for storing the product user is asking about
        private const string ProductStateKey = "product_state:{0}";

        public EchoProcessor(IDistributedCache cache, ISystemService systemService)
        {
            _cache = cache;
            _systemService = systemService;
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
                // Try to parse quantity from the message
                if (int.TryParse(message, out int quantity) && quantity > 0)
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

            // Check if the message contains a product name and quantity
            foreach (var product in ProductCatalog.Keys)
            {
                if (message.Contains(product))
                {
                    // Extract quantity if present
                    int quantity = ExtractQuantity(message);

                    if (quantity > 0)
                    {
                        // Calculate total price
                        decimal unitPrice = ProductCatalog[product];
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
                                    new LineTextMessage($"{product} {quantity} ชิ้น ราคา {totalPrice} บาท")
                                }
                            }
                        };
                    }
                    else
                    {
                        // No quantity found, store the product in cache and ask for quantity
                        await _cache.SetStringAsync(
                            productStateKey,
                            product,
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
                                    new LineTextMessage($"คุณต้องการ {product} กี่ชิ้น?")
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

        // Helper method to extract quantity from a message
        private int ExtractQuantity(string message)
        {
            // Look for numbers in the message
            var numberStrings = System.Text.RegularExpressions.Regex.Matches(message, @"\d+")
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
    }
}