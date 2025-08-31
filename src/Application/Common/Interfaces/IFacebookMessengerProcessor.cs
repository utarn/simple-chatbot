using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IFacebookMessengerProcessor
{
    string Name { get; }
    string Description { get; }
    
    Task<FacebookReplyStatus> ProcessFacebookAsync(int chatbotId, string message, string userId,
        CancellationToken cancellationToken = default);

    Task<FacebookReplyStatus> ProcessFacebookImageAsync(int chatbotId, List<FacebookAttachment>? attachments, string email,
        CancellationToken cancellationToken = default);

}
