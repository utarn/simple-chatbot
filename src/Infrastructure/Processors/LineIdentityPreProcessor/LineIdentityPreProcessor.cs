using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ChatbotApi.Infrastructure.Processors.LineIdentityPreProcessor;

public class LineIdentityPreProcessor : IPreProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LineIdentityPreProcessor> _logger;
    private readonly LineIdentityGoogleSheetHelper _googleSheetHelper;
    private readonly IDistributedCache _cache;
    private readonly IApplicationDbContext _context;

    public LineIdentityPreProcessor(
        IHttpClientFactory httpClientFactory,
        ILogger<LineIdentityPreProcessor> logger,
        IDistributedCache cache,
        IApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _googleSheetHelper = new LineIdentityGoogleSheetHelper(logger);
        _cache = cache;
        _context = context;
    }

    public async Task<OpenAIMessage?> PreProcessAsync(string userId, string messageText, CancellationToken cancellationToken = default)
    {
        // Only process if userId is provided (LINE user ID)
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        try
        {
            // First, try to get user identity from Google Sheet
            var userIdentity = await _googleSheetHelper.GetUserIdentityAsync(userId, cancellationToken);

            if (userIdentity != null)
            {
                // User exists, return their information
                var identityMessage = BuildIdentityMessage(userIdentity);
                _logger.LogInformation("Found existing user identity for LineUserId: {LineUserId}", userId);
                
                return new OpenAIMessage
                {
                    Role = "user",
                    Content = identityMessage
                };
            }
            else
            {
                // User doesn't exist, try to get their LINE profile and add them
                var profileName = await GetLineProfileNameAsync(userId, cancellationToken);
                
                if (!string.IsNullOrEmpty(profileName))
                {
                    // Add new user to Google Sheet
                    var success = await _googleSheetHelper.AddUserIdentityAsync(userId, profileName, cancellationToken);
                    
                    if (success)
                    {
                        _logger.LogInformation("Added new user identity for LineUserId: {LineUserId}, ProfileName: {ProfileName}", 
                            userId, profileName);
                        
                        return new OpenAIMessage
                        {
                            Role = "user",
                            Content = $"[New User] LineUserId = {userId}\nFirstName = {profileName}\n(Other fields are empty for new user)"
                        };
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add new user identity for LineUserId: {LineUserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not retrieve LINE profile for LineUserId: {LineUserId}", userId);
                }
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing line identity for user: {UserId}", userId);
            return null;
        }
    }

    private string BuildIdentityMessage(LineUserIdentity identity)
    {
        var message = new StringBuilder();
        message.AppendLine($"You identity is\nLineUserId = {identity.LineUserId}");
        
        if (!string.IsNullOrEmpty(identity.Initial))
            message.AppendLine($"Initial = {identity.Initial}");
            
        if (!string.IsNullOrEmpty(identity.FirstName))
            message.AppendLine($"FirstName = {identity.FirstName}");
            
        if (!string.IsNullOrEmpty(identity.LastName))
            message.AppendLine($"LastName = {identity.LastName}");
            
        if (!string.IsNullOrEmpty(identity.Group))
            message.AppendLine($"Group = {identity.Group}");
            
        if (!string.IsNullOrEmpty(identity.Faculty))
            message.AppendLine($"Faculty = {identity.Faculty}");
            
        if (!string.IsNullOrEmpty(identity.Campus))
            message.AppendLine($"Campus = {identity.Campus}");

        return message.ToString().TrimEnd();
    }

    private async Task<string?> GetLineProfileNameAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            // Check cache first
            var cachedProfileName = await _cache.GetStringAsync($"line_profile:{userId}", cancellationToken);
            if (!string.IsNullOrEmpty(cachedProfileName))
            {
                return cachedProfileName;
            }

            // Get LINE access token from any chatbot (we need it to call LINE API)
            var chatbot = await _context.Chatbots
                .Where(c => !string.IsNullOrEmpty(c.LineChannelAccessToken))
                .FirstOrDefaultAsync(cancellationToken);

            if (chatbot?.LineChannelAccessToken == null)
            {
                _logger.LogWarning("No LINE channel access token found in any chatbot configuration");
                return null;
            }

            var client = _httpClientFactory.CreateClient("resilient_nocompress");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", chatbot.LineChannelAccessToken);

            var url = $"https://api.line.me/v2/bot/profile/{userId}";
            var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get LINE profile for user {UserId}. Status: {StatusCode}", 
                    userId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var json = JsonDocument.Parse(content);
            var profileName = json.RootElement.GetProperty("displayName").GetString();

            if (!string.IsNullOrEmpty(profileName))
            {
                // Cache the profile name for 1 hour
                await _cache.SetStringAsync($"line_profile:{userId}", profileName,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) },
                    cancellationToken);
            }

            return profileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving LINE profile for user: {UserId}", userId);
            return null;
        }
    }
}