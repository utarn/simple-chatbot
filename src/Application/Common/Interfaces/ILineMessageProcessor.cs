using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Common.Interfaces;

public interface ILineMessageProcessor
{
    // All Processors need to be registered in Systems.cs, Name should be referenced to variable in Systems
    // There is no need for implementation to register in dependency injection, it will be done automatically 
    string Name { get; }

    Task<LineReplyStatus> ProcessLineAsync(LineEvent evt, int chatbotId, string message, string userId, string replyToken,
        CancellationToken cancellationToken = default);

    Task<LineReplyStatus> ProcessLineImageAsync(LineEvent evt, int chatbotId, string messageId, string userId,
        string replyToken, string accessToken, CancellationToken cancellationToken = default);

    public Task<LineReplyStatus> ProcessLineImagesAsync(LineEvent mainEvent, int chatbotId, List<LineEvent> imageEvents,
        string userId,
        string replyToken, string accessToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus() { Status = 404 });
    }

    public Task<bool> PostProcessLineAsync(string role, string? sourceMessageId, LineSendResponse response, bool isForce = false,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
    public Task<LineReplyStatus> ProcessLocationAsync(LineEvent evt, int chatbotId, double latitude, double longitude, string? address, string userId, string replyToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new LineReplyStatus() { Status = 404 });
    }
}

