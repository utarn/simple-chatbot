using System.Threading.Channels;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.Services;

/// <summary>
/// Service that consumes emails from the ObtainedEmail channel and processes them using registered ILineEmailProcessor implementations.
/// </summary>
public class EmailConsumerService : BackgroundService
{
    private readonly ChannelReader<ObtainedEmail> _channelReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailConsumerService> _logger;

    public EmailConsumerService(
        ChannelReader<ObtainedEmail> channelReader,
        IServiceProvider serviceProvider,
        ILogger<EmailConsumerService> logger)
    {
        _channelReader = channelReader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailConsumerService started");

        await foreach (var email in _channelReader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessEmailAsync(email, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email from {SenderEmail} with subject '{Subject}'",
                    email.SenderEmail, email.Subject);
            }
        }

        _logger.LogInformation("EmailConsumerService stopped");
    }

    private async Task ProcessEmailAsync(ObtainedEmail email, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing email from {SenderEmail} with subject '{Subject}' received at {DateTime}",
            email.SenderEmail, email.Subject, email.DateTime);

        try
        {
            // Get all registered ILineEmailProcessor implementations
            using var scope = _serviceProvider.CreateScope();
            var emailProcessors = scope.ServiceProvider.GetServices<ILineEmailProcessor>();

            foreach (var processor in emailProcessors)
            {
                try
                {
                    _logger.LogDebug("Processing email with {ProcessorType}", processor.GetType().Name);
                    await processor.ProcessEmailAsync(email, cancellationToken);
                    _logger.LogInformation("Email processed by {ProcessorType}", processor.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email with {ProcessorType}", processor.GetType().Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email from {SenderEmail}", email.SenderEmail);
        }

        _logger.LogDebug("Email processing completed for {SenderEmail}", email.SenderEmail);
    }
}