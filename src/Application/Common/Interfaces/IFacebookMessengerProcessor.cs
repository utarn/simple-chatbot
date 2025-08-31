using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IFacebookMessengerProcessor
{
    // Use ProcessorAttribute to declare Name and Description instead of properties
    
    Task<FacebookReplyStatus> ProcessFacebookAsync(int chatbotId, string message, string userId,
        CancellationToken cancellationToken = default);

    Task<FacebookReplyStatus> ProcessFacebookImageAsync(int chatbotId, List<FacebookAttachment>? attachments, string email,
        CancellationToken cancellationToken = default);

}
