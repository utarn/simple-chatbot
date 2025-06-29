using System.Threading.Channels;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Infrastructure.BackgroundServices;

public class FetchEmailBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FetchEmailBackgroundService> _logger;
    private readonly ChannelWriter<ObtainedEmail> _channelWriter;

    public FetchEmailBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FetchEmailBackgroundService> logger,
        ChannelWriter<ObtainedEmail> channelWriter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channelWriter = channelWriter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FetchEmailBackgroundService started");

        // Wait a bit before starting to avoid initialization conflicts
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (!stoppingToken.IsCancellationRequested &&
               await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await FetchEmailsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching emails in background service");
            }
        }

        _logger.LogInformation("FetchEmailBackgroundService stopped");
    }

    private async Task FetchEmailsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var gmailService = scope.ServiceProvider.GetService<IGmailService>();

        if (gmailService == null)
        {
            _logger.LogWarning("IGmailService is not registered. Email fetching will be skipped");
            return;
        }

        try
        {
            // Get the first Gmail setting (single row as requested)
            var gmailSetting = await context.GmailSettings
                .FirstOrDefaultAsync(cancellationToken);

            if (gmailSetting == null)
            {
                _logger.LogDebug("No Gmail settings found. Skipping email fetch");
                return;
            }

            // Check if user is authorized
            var isAuthorized = await gmailService.IsAuthorizedAsync(gmailSetting.UserId);
            if (!isAuthorized)
            {
                _logger.LogDebug("User {UserId} is not authorized or tokens are invalid. Skipping email fetch",
                    gmailSetting.UserId);
                return;
            }

            // Fetch latest emails
            var emails = await gmailService.GetLatestEmailsAsync(gmailSetting.UserId);

            if (emails.Count > 0)
            {
                _logger.LogInformation("Fetched {EmailCount} new emails for user {UserId}",
                    emails.Count, gmailSetting.UserId);

                // Convert and send emails to channel
                foreach (var email in emails)
                {
                    var obtainedEmail = new ObtainedEmail
                    {
                        SenderEmail = email.From,
                        Subject = email.Subject,
                        Content = email.Body,
                        DateTime = email.ReceivedDateTime
                    };

                    // Write to channel (non-blocking)
                    if (_channelWriter.TryWrite(obtainedEmail))
                    {
                        _logger.LogDebug("Email from {SenderEmail} with subject '{Subject}' sent to channel",
                            obtainedEmail.SenderEmail, obtainedEmail.Subject);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to write email to channel. Channel may be full or closed");
                    }
                }
            }
            else
            {
                _logger.LogDebug("No new emails found for user {UserId}", gmailSetting.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails for background processing");
        }
    }
}
