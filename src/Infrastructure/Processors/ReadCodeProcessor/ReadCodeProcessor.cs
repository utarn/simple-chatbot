using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;

namespace ChatbotApi.Infrastructure.Processors.ReadCodeProcessor
{
    public class ReadCodeProcessor : ILineMessageProcessor
    {
        public string Name => "ReadCode";
        public string Description => "อ่าน QR Code และ Barcode (Line)";

        private readonly ILogger<ReadCodeProcessor> _logger;

        public ReadCodeProcessor(ILogger<ReadCodeProcessor> logger)
        {
            _logger = logger;
        }

        public Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId,
            string replyToken, CancellationToken cancellationToken = default)
        {
            // ReadCodeProcessor only supports image messages
            return Task.FromResult(new LineReplyStatus
            {
                Status = 200,
                ReplyMessage = new LineReplyMessage
                {
                    ReplyToken = replyToken,
                    Messages = new List<LineMessage>
                    {
                        new LineTextMessage("กรุณาส่งรูปภาพ QR Code หรือ Barcode ที่ต้องการอ่าน")
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

                // Read the QR code/barcode from the image
                string? result = ReadCodeFromImage(content.Content);
                
                if (string.IsNullOrEmpty(result))
                {
                    return new LineReplyStatus
                    {
                        Status = 200,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage>
                            {
                                new LineTextMessage("ไม่สามารถอ่าน QR Code หรือ Barcode จากรูปภาพได้ กรุณาตรวจสอบรูปภาพและลองใหม่อีกครั้ง")
                            }
                        }
                    };
                }

                return new LineReplyStatus
                {
                    Status = 200,
                    ReplyMessage = new LineReplyMessage
                    {
                        ReplyToken = replyToken,
                        Messages = new List<LineMessage>
                        {
                            new LineTextMessage($"ข้อมูลที่อ่านได้จาก QR Code/Barcode:\n{result}")
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image in ReadCodeProcessor for messageId: {MessageId}", messageId);
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

            // Using the same pattern as other processors
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

        private string? ReadCodeFromImage(byte[] imageBytes)
        {
            try
            {
                // Load the image using SkiaSharp
                using var skiaImage = SKBitmap.Decode(imageBytes);
                if (skiaImage == null)
                {
                    _logger.LogWarning("Failed to decode image with SkiaSharp");
                    return null;
                }

                // Create a barcode reader
                var barcodeReader = new BarcodeReader
                {
                    Options = new DecodingOptions
                    {
                        PossibleFormats = new List<BarcodeFormat>
                        {
                            BarcodeFormat.QR_CODE,
                            BarcodeFormat.CODE_128,
                            BarcodeFormat.CODE_39,
                            BarcodeFormat.EAN_8,
                            BarcodeFormat.EAN_13,
                            BarcodeFormat.UPC_A,
                            BarcodeFormat.UPC_E,
                            BarcodeFormat.ITF,
                            BarcodeFormat.DATA_MATRIX,
                            BarcodeFormat.AZTEC,
                            BarcodeFormat.PDF_417,
                            BarcodeFormat.MAXICODE
                        },
                        TryHarder = true,
                        PureBarcode = false
                    }
                };

                // Try to decode the barcode/QR code
                ZXing.Result result = barcodeReader.Decode(skiaImage);
                return result?.Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading code from image");
                return null;
            }
        }
    }

    // Simple class to hold content result, similar to what's used in other processors
    internal class ContentResult
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string? ContentType { get; set; }
    }
}