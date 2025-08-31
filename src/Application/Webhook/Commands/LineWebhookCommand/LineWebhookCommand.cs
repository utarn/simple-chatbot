using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using Event = ChatbotApi.Application.Common.Models.LineEvent;
using LineEvent = ChatbotApi.Application.Common.Models.LineEvent;
using LineReplyStatus = ChatbotApi.Application.Common.Models.LineReplyStatus;
using LineSendResponse = ChatbotApi.Application.Common.Models.LineSendResponse;
using LineMessage = ChatbotApi.Application.Common.Models.LineMessage;
using LineTextMessage = ChatbotApi.Application.Common.Models.LineTextMessage;
using LineReplyMessage = ChatbotApi.Application.Common.Models.LineReplyMessage;
using LineImageMessage = ChatbotApi.Application.Common.Models.LineImageMessage;
using LineStickerMessage = ChatbotApi.Application.Common.Models.LineStickerMessage;

namespace ChatbotApi.Application.Webhook.Commands.LineWebhookCommand;

public class LineWebhookCommand : IRequest<LineSendResponse?>
{
    public int ChatbotId { get; set; }

    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    [JsonPropertyName("events")]
    public List<LineEvent> Events { get; set; } = new();

    public class LineWebhookCommandHandler : IRequestHandler<LineWebhookCommand, LineSendResponse?>
    {
        private static readonly string pattern = @"https?://\S+?\.(jpg|jpeg|png)";

        private static readonly Random random = new();
        private readonly IChatCompletion _chatCompletion;
        private readonly IApplicationDbContext _context;
        private readonly ILineMessenger _lineMessenger;
        private readonly ILogger<LineWebhookCommandHandler> _logger;
        private readonly IEnumerable<ILineMessageProcessor> _messageProcessors;
        private readonly IMemoryCache _cache;

        public LineWebhookCommandHandler(IApplicationDbContext context, IMemoryCache cache,
            IHttpClientFactory httpClientFactory, IEnumerable<ILineMessageProcessor> messageProcessors,
            ILogger<LineWebhookCommandHandler> logger, ILineMessenger lineMessenger, IChatCompletion chatCompletion)
        {
            _context = context;
            _messageProcessors = messageProcessors;
            _logger = logger;
            _lineMessenger = lineMessenger;
            _chatCompletion = chatCompletion;
            _cache = cache;
        }

        private static string? GetProcessorName(ILineMessageProcessor processor)
        {
            var attr = processor.GetType().GetCustomAttribute<ProcessorAttribute>();
            return attr?.Name;
        }

        public async Task<LineSendResponse?> Handle(LineWebhookCommand request, CancellationToken cancellationToken)
        {
            Chatbot chatbot = await _context.Chatbots
                .Include(c => c.ChatbotPlugins)
                .Include(c => c.PredefineMessages)
                .Where(c => c.Id == request.ChatbotId)
                .FirstAsync(cancellationToken);

            List<string> plugins = chatbot.ChatbotPlugins.Select(c => c.PluginName).ToList();
            foreach (Event evt in request.Events)
            {
                _logger.LogWarning("UserId {UserId} Type {Type} Source {Source} Message {Message}",
                    evt.Source?.UserId, evt.Type, evt.Source?.Type, evt.Message?.Text);
                string userId = evt.Source?.GroupId ?? evt.Source?.UserId ?? "";
                string replyToken = evt.ReplyToken;
                string? sourceMessageId = evt.Message?.Id;
                if (evt.Type == "postback")
                {
                    LineReplyStatus? toReturn =
                        await ProcessPostbackEvent(evt, chatbot, plugins, userId, replyToken, cancellationToken);
                    return await SendReply(cancellationToken, toReturn, replyToken, chatbot);
                }

                if (evt.Type == "message")
                {
                    if (evt.Message?.Type == "text")
                    {
                        LineReplyStatus? toReturn = await ProcessTextMessageEvent(evt, _chatCompletion, chatbot,
                            plugins, userId, replyToken, request.ChatbotId, sourceMessageId,
                            cancellationToken);

                        var result = await SendReply(cancellationToken, toReturn, replyToken, chatbot);
                        await PostProcessMessage(plugins, sourceMessageId, result, false, cancellationToken);
                    }

                    if (evt.Message?.Type == "sticker" && evt.Source?.Type == "user")
                    {
                        LineReplyStatus toReturn = StickerProcess(replyToken, cancellationToken);
                        return await SendReply(cancellationToken, toReturn, replyToken, chatbot);
                    }

                    if (evt.Message?.Type is "image" or "file" or "video" or "audio")
                    {
                        await HandleMediaMessage(evt, chatbot, plugins, userId, replyToken,
                            cancellationToken);
                    }

                    if (evt.Message?.Type == "location")
                    {
                        LineReplyStatus? toReturn = null;
                        if (chatbot.LineChannelAccessToken != null)
                        {
                            double? latitude = evt.Message?.Latitude;
                            double? longitude = evt.Message?.Longitude;
                            string? address = evt.Message?.Address;

                            if (latitude.HasValue && longitude.HasValue)
                            {
                                foreach (ILineMessageProcessor process in _messageProcessors)
                                {
                                    var processorName = GetProcessorName(process);
                                    if (processorName == null || !plugins.Contains(processorName))
                                    {
                                        continue;
                                    }

                                    var result = await process.ProcessLocationAsync(
                                        evt, chatbot.Id, latitude.Value, longitude.Value, address, userId, replyToken, cancellationToken);

                                    if (result.Status is 200 or 201)
                                    {
                                        toReturn = result;
                                        break;
                                    }
                                }
                            }
                        }

                        var sendResult = await SendReply(cancellationToken, toReturn, replyToken, chatbot);
                        // No post-processing for location by default
                    }
                }
            }

            return null;
        }

        // ChatReport helper removed

        private async Task<LineReplyStatus?> ProcessPostbackEvent(Event evt, Chatbot chatbot, List<string> plugins,
            string userId, string replyToken, CancellationToken cancellationToken)
        {
            string messageText = evt.Postback?.Data ?? string.Empty;
            LineReplyStatus? toReturn = null;
            if (chatbot.LineChannelAccessToken != null)
            {
                foreach (ILineMessageProcessor process in _messageProcessors)
                {
                    var processorName = GetProcessorName(process);
                    if (processorName == null || !plugins.Contains(processorName))
                    {
                        continue;
                    }

                    LineReplyStatus result =
                        await process.ProcessLineAsync(evt, chatbot.Id, messageText, userId, replyToken,
                            cancellationToken);
                    if (result.Status is 200 or 201)
                    {
                        toReturn = result;
                        break;
                    }
                }
            }

            return toReturn;
        }

        private async Task<LineReplyStatus?> ProcessTextMessageEvent(Event evt, IChatCompletion selectedChatCompletion,
            Chatbot chatbot, List<string> plugins, string userId, string replyToken,
            int chatbotId, string? sourceMessageId, CancellationToken cancellationToken)
        {
            string messageText = evt.Message?.Text ?? string.Empty;
            LineReplyStatus? toReturn = null;

            if (chatbot.LineChannelAccessToken != null)
            {
                foreach (ILineMessageProcessor process in _messageProcessors)
                {
                    var processorName = GetProcessorName(process);
                    if (processorName == null || !plugins.Contains(processorName))
                    {
                        continue;
                    }

                    var processResult = await process.ProcessLineAsync(evt, chatbot.Id, messageText, userId,
                        replyToken, cancellationToken);
                    if (processResult.Status is 200 or 201)
                    {
                        toReturn = processResult;
                        break;
                    }
                }
            }

            if (toReturn == null)
            {
                try
                {
                    var chatCompletion = await selectedChatCompletion.ChatCompleteAsync(
                        chatbotId, userId, messageText, null, MessageChannel.Line);

                    var cleansedMessage = chatCompletion.Message.Replace("****", "")
                        .Replace("**", "")
                        .Replace("`", "'")
                        .Replace("\"", "");

                    if (cleansedMessage.Length > 4000)
                    {
                        var messages = new List<LineMessage>();
                        int chunkSize = 4000;
                        int overlap = 20;
                        int startIndex = 0;

                        while (startIndex < cleansedMessage.Length)
                        {
                            int endIndex = Math.Min(startIndex + chunkSize, cleansedMessage.Length);
                            string chunk = cleansedMessage.Substring(startIndex, endIndex - startIndex);

                            if (!string.IsNullOrEmpty(chunk))
                            {
                                // Add the chunk as a text message
                                messages.Add(new LineTextMessage { Text = chunk });
                            }

                            // Move the start index forward, but keep overlap
                            startIndex = endIndex - (endIndex == cleansedMessage.Length ? 0 : overlap);
                        }

                        toReturn = new LineReplyStatus
                        {
                            Status = 200,
                            ReplyMessage = new LineReplyMessage { ReplyToken = replyToken, Messages = messages }
                        };
                    }
                    else
                    {
                        toReturn = new LineReplyStatus
                        {
                            Status = 200,
                            ReplyMessage = new LineReplyMessage
                            {
                                ReplyToken = replyToken,
                                Messages = [new LineTextMessage { Text = cleansedMessage }]
                            }
                        };
                    }
                }
                catch (ChatCompletionException e)
                {
                    toReturn = new LineReplyStatus
                    {
                        Status = e.StatusCode,
                        ReplyMessage = new LineReplyMessage
                        {
                            ReplyToken = replyToken,
                            Messages = new List<LineMessage> { new LineTextMessage { Text = e.Message } }
                        }
                    };
                }

                if (toReturn.Status == 200 &&
                    toReturn.ReplyMessage?.Messages[0] is LineTextMessage textMessage)
                {
                    List<string> urlList = ExtractUrls(textMessage.Text);

                    // Remove URLs from the text
                    textMessage.Text = Regex.Replace(textMessage.Text, pattern, "").Trim();

                    if (urlList.Count > 0)
                    {
                        foreach (string url in urlList)
                        {
                            LineImageMessage imageMessage = new() { OriginalContentUrl = url, PreviewImageUrl = url };
                            toReturn.ReplyMessage.Messages.Add(imageMessage);
                        }
                    }
                }
            }

            return toReturn;
        }

        private List<string> ExtractUrls(string text)
        {
            List<string> urlList = new();
            MatchCollection matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                urlList.Add(match.Value);
            }

            return urlList;
        }

        private async Task HandleMediaMessage(Event evt, Chatbot chatbot, List<string> plugins, string userId,
            string replyToken, CancellationToken cancellationToken)
        {
            if (evt.Message?.ImageSet != null && !string.IsNullOrEmpty(evt.Message.ImageSet.Id))
            {
                await ProcessImageSet(evt, chatbot, plugins, userId, replyToken, cancellationToken);
            }
            else
            {
                await ProcessSingleMedia(evt, chatbot, plugins, userId, replyToken, cancellationToken);
            }
        }

        private async Task ProcessImageSet(Event evt, Chatbot chatbot, List<string> plugins, string userId,
            string replyToken, CancellationToken cancellationToken)
        {
            // Random wait
            await Task.Delay(random.Next(100, 5000), cancellationToken);
            // Generate a unique cache key for the image set
            string cacheKey = $"ImageSet_{evt.Message?.ImageSet?.Id}";
            string lockKey = $"{cacheKey}_Lock";

            // Try to acquire the lock using IMemoryCache
            bool lockAcquired = false;
            try
            {
                // Attempt to set the lock key with a short expiration time
                lockAcquired = _cache.TryGetValue(lockKey, out _);
                if (!lockAcquired)
                {
                    _cache.Set(lockKey, "locked",
                        TimeSpan.FromSeconds(10)); // Lock expires after 10 seconds
                    lockAcquired = true;
                }

                if (lockAcquired)
                {
                    // Store the current image in the cache
                    string imageKey = $"{cacheKey}_{evt.Message?.ImageSet?.Index}";
                    _cache.Set(imageKey, evt,
                        TimeSpan.FromMinutes(5)); // Set expiration time for the image

                    // Check if all images in the set have been received
                    bool allImagesReceived = true;
                    for (int i = 1; i <= evt.Message?.ImageSet?.Total; i++)
                    {
                        string key = $"{cacheKey}_{i}";
                        if (!_cache.TryGetValue(key, out _))
                        {
                            allImagesReceived = false;
                            break;
                        }
                    }

                    if (allImagesReceived)
                    {
                        // Retrieve all images from cache
                        List<Event> imageEvents = new();
                        for (int i = 1; i <= evt.Message?.ImageSet?.Total; i++)
                        {
                            string key = $"{cacheKey}_{i}";
                            if (_cache.TryGetValue(key, out Event? cachedEvent))
                            {
                                if (cachedEvent != null)
                                {
                                    imageEvents.Add(cachedEvent);
                                }
                            }
                        }

                        // Process all images together
                        LineReplyStatus? toReturn = null;
                        if (chatbot.LineChannelAccessToken != null)
                        {
                            foreach (ILineMessageProcessor proces in _messageProcessors)
                            {
                                var processorName = GetProcessorName(proces);
                                if (processorName == null || !plugins.Contains(processorName))
                                {
                                    continue;
                                }

                                LineReplyStatus result = await proces.ProcessLineImagesAsync(
                                    evt,
                                    chatbot.Id,
                                    imageEvents, userId,
                                    replyToken, chatbot.LineChannelAccessToken, cancellationToken);
                                if (result.Status == 200)
                                {
                                    toReturn = result;
                                    break;
                                }
                            }
                        }

                        // ChatReport logic removed

                        var sendImageResult = await SendReply(cancellationToken, toReturn,
                            replyToken, chatbot);

                        await PostProcessMessage(plugins, null, sendImageResult, true, cancellationToken);
                    }
                }
                else
                {
                    // If the lock is not acquired, another request is processing the image set
                    _logger.LogWarning(
                        "Failed to acquire lock for image set {ImageSetId}. Another request is processing it",
                        evt.Message?.ImageSet?.Id);
                }
            }
            finally
            {
                // Release the lock by removing the cache entry
                if (lockAcquired)
                {
                    _cache.Remove(lockKey);
                }
            }
        }

        private async Task ProcessSingleMedia(Event evt, Chatbot chatbot, List<string> plugins, string userId,
            string replyToken, CancellationToken cancellationToken)
        {
            // Process single image
            LineReplyStatus? toReturn = null;
            if (chatbot.LineChannelAccessToken != null)
            {
                foreach (ILineMessageProcessor process in _messageProcessors)
                {
                    var processorName = GetProcessorName(process);
                    if (processorName == null || !plugins.Contains(processorName))
                    {
                        continue;
                    }

                    LineReplyStatus result = await process.ProcessLineImageAsync(evt, chatbot.Id,
                        evt.Message?.Id ?? string.Empty,
                        userId,
                        replyToken,
                        chatbot.LineChannelAccessToken, cancellationToken);
                    if (result.Status == 200)
                    {
                        toReturn = result;
                        break;
                    }
                }
            }

            // ChatReport logic removed
            var sendImageResult =
                await SendReply(cancellationToken, toReturn, replyToken, chatbot);

            await PostProcessMessage(plugins, null, sendImageResult, true, cancellationToken);
        }

        private async Task PostProcessMessage(List<string> plugins, string? sourceMessageId, LineSendResponse? result,
            bool isMedia, CancellationToken cancellationToken)
        {
            foreach (ILineMessageProcessor process in _messageProcessors)
            {
                var processorName = GetProcessorName(process);
                if (processorName == null || !plugins.Contains(processorName))
                {
                    continue;
                }

                if (result is { Status: 200 or 201 })
                {
                    await process.PostProcessLineAsync("model", sourceMessageId, result, isMedia, cancellationToken);
                }
            }
        }

        #region Common

        private async Task<LineSendResponse?> SendReply(CancellationToken cancellationToken,
            LineReplyStatus? toReturn,
            string replyToken, Chatbot chatbot)
        {
            try
            {
                if (toReturn is { Status: 200, ReplyMessage: not null })
                {
                    var response = await _lineMessenger.SendMessage(chatbot, toReturn.ReplyMessage, cancellationToken);
                    response.ContentResults = toReturn.ContentResults;
                    return response;
                }

                if (toReturn is { Status: 201, Raw: not null })
                {
                    string template =
                        "{\"replyToken\": \"%replyToken%\",\"messages\": [ { \"altText\" : \"Flex Template\", \"type\" : \"flex\", \"contents\" : %json% } ]}";
                    template = template.Replace("%replyToken%", replyToken);
                    template = template.Replace("%json%", toReturn.Raw);
                    var response = await _lineMessenger.SendRawMessage(chatbot, template, cancellationToken);
                    response.ContentResults = toReturn.ContentResults;
                    return response;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        private LineReplyStatus StickerProcess(string replyToken, CancellationToken cancellationToken)
        {
            List<(string packageId, string stickerId)> funnyStickers = new()
            {
                ("446", "1992"),
                ("446", "1989"),
                ("446", "2024"),
                ("446", "2014"),
                ("789", "10871"),
                ("789", "10876"),
                ("789", "10891"),
                ("1070", "17852"),
                ("1070", "17866"),
                ("6136", "10551377"),
                ("6136", "10551378"),
                ("6136", "10551398"),
                ("6359", "11069859"),
                ("6359", "11069853"),
                ("6359", "11069868"),
                ("11537", "52002734"),
                ("11537", "52002739"),
                ("11537", "52002740"),
                ("11537", "52002745")
            };

            (string packageId, string stickerId) randomSticker = funnyStickers[random.Next(funnyStickers.Count)];

            // Create reply message with a random funny sticker
            LineReplyMessage replyMessage = new()
            {
                ReplyToken = replyToken,
                Messages = new List<LineMessage>
                {
                    new LineStickerMessage
                    {
                        PackageId = randomSticker.packageId, StickerId = randomSticker.stickerId
                    }
                }
            };

            return new LineReplyStatus { Status = 200, ReplyMessage = replyMessage };
        }

        #endregion
    }
}
