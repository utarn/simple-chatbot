using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IFacebookMessenger
{
    Task<FacebookReplyStatus> ProcessFacebookMessage(Chatbot chatbot, FacebookReplyMessage message,
        CancellationToken cancellationToken = default);

}
