using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using System.Web;
using ChatbotApi.Application.Chatbots.Commands.CreatePreMessageFromFileCommand;
using ChatbotApi.Domain.Entities;
using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ChatbotApi.Infrastructure.BackgroundServices;

public class CronRefreshBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CronRefreshBackgroundService> _logger;
    private readonly ChannelWriter<ProcessFileBackgroundTask> _channelWriter;
    private readonly HttpClient _httpClient;

    public CronRefreshBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CronRefreshBackgroundService> logger,
        ChannelWriter<ProcessFileBackgroundTask> channelWriter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channelWriter = channelWriter;
        _httpClient = new HttpClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await CheckAndRefreshPreMessagesAsync(stoppingToken);
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckAndRefreshPreMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking and refreshing PreMessages");
            }
        }
    }

    private async Task CheckAndRefreshPreMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        // Get all PreMessages that have a URL and a CronJob set
        var preMessagesWithCron = context.PreMessages
            .Include(p => p.ChatBot)
            .Where(p => p.Url != null && !string.IsNullOrEmpty(p.CronJob))
            .AsEnumerable()
            .GroupBy(p => p.FileHash)
            .Select(group => 
                // Try to get the first item with non-zero ChunkSize and OverlappingSize
                group.OrderBy(p => p.Order)
                    .FirstOrDefault(p => p.ChunkSize != 0 && p.OverlappingSize != 0) 
                // If none found, just take the first item in the ordered group
                ?? group.OrderBy(p => p.Order).FirstOrDefault()
            )
            .Where(p => p != null) // Filter out any null results
            .ToList();
        
        foreach (var preMessage in preMessagesWithCron)
        {
            if (preMessage == null) continue;
            
            var utcNow = DateTime.UtcNow;
            try
            {
                var cronExpression = CronExpression.Parse(preMessage.CronJob!);
                var nextOccurrenceFromLast = cronExpression.GetNextOccurrence(
                    preMessage.LastUpdate ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
                if (nextOccurrenceFromLast.HasValue && nextOccurrenceFromLast.Value <= utcNow)
                {
                    _logger.LogInformation(
                        "Refreshing PreMessage {ChatBotId}-{Order} with URL {Url} based on cron schedule {CronJob}",
                        preMessage.ChatBotId, preMessage.Order, preMessage.Url, preMessage.CronJob);

                    await RefreshPreMessageAsync(preMessage, httpClientFactory, stoppingToken);
                    preMessage.LastUpdate = utcNow;
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cron schedule for PreMessage {ChatBotId}-{Order}",
                    preMessage.ChatBotId, preMessage.Order);
            }
        }
    }

    private async Task RefreshPreMessageAsync(PreMessage preMessage, IHttpClientFactory httpClientFactory,
        CancellationToken stoppingToken)
    {
        if (preMessage.Url == null) return;
        try
        {
            var url = preMessage.Url;
            var httpClient = httpClientFactory.CreateClient();
            // Add browser-like headers to the HttpClient
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            httpClient.DefaultRequestHeaders.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, stoppingToken);
            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            if (preMessage.FileMimeType == "text/html" || preMessage.FileMimeType == "text/plaintext")
            {
                var htmlContent = await response.Content.ReadAsStringAsync(stoppingToken);
                var chunkSize = preMessage.ChatBot.MaxChunkSize ?? 2000;
                if (preMessage.ChunkSize != 0)
                {
                    chunkSize = preMessage.ChunkSize;
                }

                var overlappingSize = preMessage.ChatBot.MaxOverlappingSize ?? 200;
                if (preMessage.OverlappingSize != 0)
                {
                    overlappingSize = preMessage.OverlappingSize;
                }

                // HTML content: pass the URL directly
                var backgroundTask = new ProcessFileBackgroundTask
                {
                    ChatBotId = preMessage.ChatBotId,
                    Url = url,
                    ChunkSize = chunkSize,
                    OverlappingSize = overlappingSize,
                    UseCag = false,
                    FileContent = Encoding.UTF8.GetBytes(htmlContent),
                    FileMimeType = MediaTypeNames.Text.Html,
                    FileName = $"{Guid.NewGuid()}.html",
                    CronJob = preMessage.CronJob
                };

                await _channelWriter.WriteAsync(backgroundTask, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing PreMessage {ChatBotId}-{Order} with URL {Url}",
                preMessage.ChatBotId, preMessage.Order, preMessage.Url);
        }
    }
    }
