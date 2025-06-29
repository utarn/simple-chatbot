using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using ChatbotApi.Domain.Enums;
using ChatbotApi.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotApi.Infrastructure.Processors.EmailProcessors;

/// <summary>
/// Example implementation of ILineEmailProcessor that demonstrates email processing for Line messaging.
/// This processor handles emails based on subject keywords and sends Line messages directly.
/// </summary>
public class ExampleLineEmailProcessor  : ILineEmailProcessor
{
    private readonly ILogger<ExampleLineEmailProcessor> _logger;
    private readonly ILineMessenger _lineMessenger;
    private readonly IChatCompletion _chatCompletion;
    private readonly IApplicationDbContext _dbContext;
    private const string LinePushTo = "U4740f2e9d397de763e088afc9bee2d10";

    public ExampleLineEmailProcessor(
        ILogger<ExampleLineEmailProcessor> logger,
        ILineMessenger lineMessenger,
        IChatCompletion chatCompletion,
        IApplicationDbContext dbContext)
    {
        _logger = logger;
        _lineMessenger = lineMessenger;
        _chatCompletion = chatCompletion;
        _dbContext = dbContext;
    }

    public async Task ProcessEmailAsync(ObtainedEmail email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing general email from {SenderEmail} with subject '{Subject}' without specific chatbot",
            email.SenderEmail, email.Subject);

        try
        {
            if (!string.IsNullOrEmpty(email.Content))
            {
                _logger.LogDebug("Summarizing email content using OpenAI and sending via chatbot with SummarizeEmail plugin");

                // Find a chatbot with the SummarizeEmail plugin enabled
                var chatbot = await _dbContext.Chatbots
                    .Include(c => c.ChatbotPlugins)
                    .FirstOrDefaultAsync(c =>
                        c.ChatbotPlugins.Any(p => p.PluginName == Systems.SummarizeEmail),
                        cancellationToken);

                if (chatbot == null)
                {
                    _logger.LogWarning("No chatbot found with SummarizeEmail plugin enabled. Skipping summarization");
                    return;
                }

                // Compose summarization prompt
                string prompt = $"Summarize the following email content:\n\n{email.Content}";

                // Call OpenAI to summarize
                var chatResult = await _chatCompletion.ChatCompleteAsync(
                    chatbot.Id,
                    "", // userId not needed for system process
                    prompt,
                    null,
                    MessageChannel.OpenAI,
                    max_tokens: 200,
                    temperature: 0.7m,
                    cancellationToken: cancellationToken);

                string summary = chatResult?.Message ?? "No summary generated.";

                // Format message with email metadata
                string formattedMessage = $"From: {email.SenderEmail}\n" +
                                          $"Date: {email.DateTime:g}\n" +
                                          $"Subject: {email.Subject}\n\n" +
                                          $"{summary}";

                // Push formatted message to recipient
                var lineMessage = new LineTextMessage(formattedMessage);
                var pushMessage = new LinePushMessage
                {
                    To = LinePushTo,
                    Messages = new List<LineMessage> { lineMessage }
                };
                await _lineMessenger.SendPushMessage(chatbot, pushMessage, cancellationToken);
            }

            _logger.LogInformation("General email processing completed for {SenderEmail}", email.SenderEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in general email processing for {SenderEmail}", email.SenderEmail);
            throw;
        }
    }
}
