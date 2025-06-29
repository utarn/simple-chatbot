using ChatbotApi.Domain.Models;

namespace ChatbotApi.Application.Common.Interfaces;

/// <summary>
/// Interface for processing obtained emails from the background service for Line messaging.
/// Implement this interface to create custom Line email processors.
/// </summary>
public interface ILineEmailProcessor
{
    /// <summary>
    /// Process an obtained email asynchronously without specifying a chatbot.
    /// Implement this method to handle general email processing that doesn't require a specific chatbot context.
    /// </summary>
    /// <param name="email">The email to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the processing operation</returns>
    Task ProcessEmailAsync(ObtainedEmail email, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}