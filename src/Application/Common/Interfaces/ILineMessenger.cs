using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Application.Common.Interfaces;

public interface ILineMessenger
{
    Task<LineSendResponse> SendMessage(Chatbot chatbot, LineReplyMessage message,
        CancellationToken cancellationToken = default);
    Task<LineSendResponse> SendRawMessage(Chatbot chatbot, string json,
        CancellationToken cancellationToken = default);
    Task<LineSendResponse> SendPushMessage(Chatbot chatbot, LinePushMessage message,
        CancellationToken cancellationToken = default);
}
