using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ChatbotApi.Application.Webhook.Commands.LineWebhookCommand;
using ChatbotApi.Domain.Enums;
using ChatbotApi.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

public class FacebookWebhookCommand : IRequest<bool>
{
    [JsonPropertyName("entry")]
    public List<FacebookEntry> Entry { get; set; }

    public int ChatbotId { get; set; }

    public class FacebookWebhookCommandHandler : IRequestHandler<FacebookWebhookCommand, bool>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<FacebookWebhookCommandHandler> _logger;
        private readonly IFacebookMessenger _facebookMessenger;
        private readonly ISystemService _systemService;
        private readonly IEnumerable<IFacebookMessengerProcessor> _messageProcessors;
        private readonly IChatCompletion _chatCompletion;

        private static readonly string Pattern = @"https?://\S+?\.(jpg|jpeg|png)";

        public FacebookWebhookCommandHandler(IApplicationDbContext context,
            ILogger<FacebookWebhookCommandHandler> logger, IFacebookMessenger facebookMessenger,
            ISystemService systemService, IEnumerable<IFacebookMessengerProcessor> messageProcessors,
            IChatCompletion chatCompletion)
        {
            _context = context;
            _logger = logger;
            _facebookMessenger = facebookMessenger;
            _systemService = systemService;
            _messageProcessors = messageProcessors;
            _chatCompletion = chatCompletion;
        }

        public async Task<bool> Handle(FacebookWebhookCommand request, CancellationToken cancellationToken)
        {
            var chatbot = await _context.Chatbots
                .Include(c => c.ChatbotPlugins)
                .Include(c => c.PredefineMessages)
                .Where(c => c.Id == request.ChatbotId)
                .FirstAsync(cancellationToken);

            if (chatbot.FacebookVerifyToken == null || chatbot.FacebookAccessToken == null)
            {
                return false;
            }

            var plugins = chatbot.ChatbotPlugins.Select(p => p.PluginName).ToList();

            // ChatReport logic removed

            foreach (var entry in request.Entry)
            {
                var messaging = entry.Messaging?.FirstOrDefault();
                var userId = messaging?.Sender?.Id;
                var messageText = messaging?.Message?.Text;
                var attachments = messaging?.Message?.Attachment;

                _logger.LogWarning("Facebook Webhook: {UserId} said: {MessageText}", userId, messageText);
                FacebookReplyStatus? toReturn = null;

                // ChatReport logic removed
                // if (toReturn != null)
                // {
                //     return await SendToServer(cancellationToken, toReturn, chatbot);
                // }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Message does not contain a valid user ID");
                    continue;
                }

                if (!string.IsNullOrEmpty(messageText))
                {
                    // ChatReport logic removed

                    foreach (var process in _messageProcessors)
                    {
                        if (!plugins.Contains(process.Name))
                        {
                            continue;
                        }

                        var result =
                            await process.ProcessFacebookAsync(chatbot.Id, messageText, userId, cancellationToken);
                        if (result.Status is 200 or 201)
                        {
                            toReturn = result;
                            break;
                        }
                    }

                    if (toReturn != null)
                    {
                        await SendToServer(cancellationToken, toReturn, chatbot);
                        continue;
                    }

                    try
                    {
                        var chatCompletion = await _chatCompletion.ChatCompleteAsync(
                            request.ChatbotId, userId, messageText, null, MessageChannel.Messenger);

                        toReturn =
                            new FacebookReplyStatus()
                            {
                                Status = 200,
                                ReplyMessage =
                                [
                                    new FacebookReplyMessage()
                                    {
                                        Recipient = new FacebookUser() { Id = userId },
                                        Message = new TextFacebookMessage() { Text = chatCompletion.Message }
                                    }
                                ]
                            };
                    }
                    catch (ChatCompletionException e)
                    {
                        toReturn = new FacebookReplyStatus() { Status = e.StatusCode, Error = e.Message };
                    }

                    if (toReturn.Status == 200 && toReturn.ReplyMessage != null)
                    {
                        var toAdd = new List<FacebookReplyMessage>();
                        var toRemove = new List<FacebookReplyMessage>();
                        foreach (var replyMessage in toReturn.ReplyMessage)
                        {
                            if (replyMessage.Message is not TextFacebookMessage textFacebookMessage)
                            {
                                continue;
                            }

                            if (textFacebookMessage.Text == null)
                            {
                                continue;
                            }

                            List<string> urlList = new List<string>();
                            MatchCollection matches = Regex.Matches(textFacebookMessage.Text, Pattern);
                            foreach (Match match in matches)
                            {
                                urlList.Add(match.Value);
                            }

                            string cleanedText = Regex.Replace(textFacebookMessage.Text, Pattern, "");
                            textFacebookMessage.Text = cleanedText.Trim();
                            if (textFacebookMessage.Text == "")
                            {
                                toRemove.Add(replyMessage);
                            }

                            if (urlList.Count > 0)
                            {
                                foreach (string url in urlList)
                                {
                                    var imageMessage = new FacebookReplyMessage()
                                    {
                                        Recipient = new FacebookUser() { Id = userId },
                                        Message = new ImageFacebookMessage()
                                        {
                                            Attachment =
                                            [
                                                new FacebookAttachment()
                                                {
                                                    Payload = new FacebookAttachmentPayload() { Url = url }
                                                }
                                            ]
                                        }
                                    };

                                    toAdd.Add(imageMessage);
                                }
                            }
                        }

                        foreach (var replyMessage in toRemove)
                        {
                            toReturn.ReplyMessage.Remove(replyMessage);
                        }

                        foreach (var replyMessage in toAdd)
                        {
                            toReturn.ReplyMessage.Add(replyMessage);
                        }


                        await SendToServer(cancellationToken, toReturn, chatbot);
                    }
                }

                else if (attachments != null)
                {
                    toReturn = null;
                    if (chatbot is { FacebookAccessToken: not null, FacebookVerifyToken: not null })
                    {
                        foreach (var process in _messageProcessors)
                        {
                            if (!plugins.Contains(process.Name))
                            {
                                continue;
                            }

                            var result = await process.ProcessFacebookImageAsync(chatbot.Id, attachments, userId,
                                cancellationToken);
                            if (result.Status == 200)
                            {
                                toReturn = result;
                                break;
                            }
                        }
                    }

                    // ChatReport logic removed
                    if (toReturn != null)
                    {
                        await SendToServer(cancellationToken, toReturn, chatbot);
                    }
                }
            }

            return true;
        }

        #region Common

        private async Task<bool> SendToServer(CancellationToken cancellationToken, FacebookReplyStatus? toReturn,
            Chatbot chatbot)
        {
            try
            {
                if (toReturn is { Status: 200, ReplyMessage: not null })
                {
                    foreach (var replyMessage in toReturn.ReplyMessage)
                    {
                        await _facebookMessenger.ProcessFacebookMessage(chatbot, replyMessage, cancellationToken);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
