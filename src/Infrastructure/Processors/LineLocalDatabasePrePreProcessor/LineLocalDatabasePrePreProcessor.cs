using System.Net.Http.Headers;
using System.Text;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Application.Common.Attributes;
using ChatbotApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ChatbotApi.Infrastructure.Processors.LineLocalDatabasePrePreProcessor;

[Processor("LineLocalDatabasePrePreProcessor", "LINE Local Database Pre-Processor")]
public class LineLocalDatabasePrePreProcessor : IPreProcessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LineLocalDatabasePrePreProcessor> _logger;
    private readonly IDistributedCache _cache;
    private readonly IApplicationDbContext _context;

    public LineLocalDatabasePrePreProcessor(
        IHttpClientFactory httpClientFactory,
        ILogger<LineLocalDatabasePrePreProcessor> logger,
        IDistributedCache cache,
        IApplicationDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
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
            // First, try to get user identity from local database
            var userIdentity = await GetUserInternalProfileAsync(userId, cancellationToken);

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
                    // Add new user to local database
                    var success = await AddUserInternalProfileAsync(userId, profileName, cancellationToken);
                    
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

    private string BuildIdentityMessage(UserInternalProfile identity)
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

    private async Task<UserInternalProfile?> GetUserInternalProfileAsync(string lineUserId, CancellationToken cancellationToken)
    {
        try
        {
            // Check cache first
            var cachedProfileJson = await _cache.GetStringAsync($"user_internal_profile:{lineUserId}", cancellationToken);
            if (!string.IsNullOrEmpty(cachedProfileJson))
            {
                // Note: In a real implementation, we would deserialize the cached data
                // For now, we'll just indicate that we found cached data
                _logger.LogInformation("Using cached user profile for LineUserId: {LineUserId}", lineUserId);
            }

            // Get user from database
            var userProfile = await _context.UserInternalProfiles
                .FirstOrDefaultAsync(u => u.LineUserId == lineUserId, cancellationToken);

            if (userProfile != null)
            {
                // Cache the profile for 1 hour
                var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
                // Note: In a real implementation, we would serialize the object to cache it
                await _cache.SetStringAsync($"user_internal_profile:{lineUserId}", "cached", options, cancellationToken);
                
                _logger.LogInformation("Found user profile from database for LineUserId: {LineUserId}", lineUserId);
            }
            else
            {
                _logger.LogInformation("LineUserId {LineUserId} not found in database", lineUserId);
            }

            return userProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile from database for LineUserId: {LineUserId}", lineUserId);
            return null;
        }
    }

    private async Task<bool> AddUserInternalProfileAsync(string lineUserId, string profileName, CancellationToken cancellationToken)
    {
        try
        {
            var newUserProfile = new UserInternalProfile
            {
                LineUserId = lineUserId,
                FirstName = profileName,
                Initial = "",
                LastName = "",
                Group = "",
                Faculty = "",
                Campus = ""
            };

            _context.UserInternalProfiles.Add(newUserProfile);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate cache when new user is added
            await _cache.RemoveAsync($"user_internal_profile:{lineUserId}", cancellationToken);

            _logger.LogInformation("Successfully added new user profile to database. LineUserId: {LineUserId}, ProfileName: {ProfileName}",
                lineUserId, profileName);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user profile to database. LineUserId: {LineUserId}, ProfileName: {ProfileName}",
                lineUserId, profileName);
            return false;
        }
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
            var json = System.Text.Json.JsonDocument.Parse(content);
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