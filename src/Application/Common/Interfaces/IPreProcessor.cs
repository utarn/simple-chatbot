using System.Threading;
using System.Threading.Tasks;
using ChatbotApi.Application.Common.Models;

namespace ChatbotApi.Application.Common.Interfaces;

public interface IPreProcessor
{
    string Name { get; }
    /// <summary>
    /// Process incoming user message before chat completion and return an OpenAIMessage to prepend.
    /// Return null if no message should be added.
    /// </summary>
    /// <param name="userId">The user id (may be empty)</param>
    /// <param name="messageText">Original user message text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An OpenAIMessage to append to messages (or null to skip)</returns>
    Task<OpenAIMessage?> PreProcessAsync(string userId, string messageText, CancellationToken cancellationToken = default);
}
