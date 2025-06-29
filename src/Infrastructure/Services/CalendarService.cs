using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Entities;
using ChatbotApi.Domain.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatbotApi.Infrastructure.Services;

public class GoogleCalendarService : ICalendarService
{
    private readonly CalendarSettings _calendarSettings;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GoogleCalendarService> _logger;
    private readonly ISystemService _systemService;
    private readonly GoogleAuthorizationCodeFlow _flow;

    public GoogleCalendarService(
        IOptions<CalendarSettings> calendarSettings,
        IApplicationDbContext context,
        ILogger<GoogleCalendarService> logger,
        ISystemService systemService)
    {
        _calendarSettings = calendarSettings.Value;
        _context = context;
        _logger = logger;
        _systemService = systemService;

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _calendarSettings.ClientId,
                ClientSecret = _calendarSettings.ClientSecret
            },
            Scopes = _calendarSettings.Scopes,
            DataStore = null // We'll use database instead
        });
    }

    public string GetAuthorizationUrl(string userId)
    {
        try
        {
            var redirectUri = GetRedirectUri();
            var request = _flow.CreateAuthorizationCodeRequest(redirectUri);
            request.State = userId; // Store userId in state parameter
            var authUri = request.Build();

            _logger.LogInformation("Generated Calendar authorization URL for user {UserId}", userId);
            return authUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Calendar authorization URL for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HandleAuthorizationCallbackAsync(string userId, string authorizationCode)
    {
        try
        {
            var redirectUri = GetRedirectUri();
            var tokenResponse = await _flow.ExchangeCodeForTokenAsync(
                userId,
                authorizationCode,
                redirectUri,
                CancellationToken.None);

            // Store tokens in database
            await SaveTokensToDatabase(userId, tokenResponse);

            _logger.LogInformation("Successfully stored Calendar tokens for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Calendar authorization callback for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> CreateEventAsync(string userId, string summary, string description, DateTime startDateTime, DateTime endDateTime, bool isAllDay = false)
    {
        try
        {
            var tokenResponse = await GetValidTokenAsync(userId);
            if (tokenResponse == null)
            {
                _logger.LogWarning("No valid Calendar tokens found for user {UserId}", userId);
                return false;
            }

            var credential = new UserCredential(_flow, userId, tokenResponse);

            var calendarService = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatBot API Calendar Integration"
            });

            var calendarEvent = new Google.Apis.Calendar.v3.Data.Event()
            {
                Summary = summary,
                Description = description
            };

            if (isAllDay)
            {
                calendarEvent.Start = new EventDateTime()
                {
                    Date = startDateTime.ToString("yyyy-MM-dd"),
                    TimeZone = "UTC"
                };
                calendarEvent.End = new EventDateTime()
                {
                    Date = endDateTime.ToString("yyyy-MM-dd"),
                    TimeZone = "UTC"
                };
            }
            else
            {
                calendarEvent.Start = new EventDateTime()
                {
                    DateTimeDateTimeOffset = startDateTime,
                    TimeZone = "UTC"
                };
                calendarEvent.End = new EventDateTime()
                {
                    DateTimeDateTimeOffset = endDateTime,
                    TimeZone = "UTC"
                };
            }

            var request = calendarService.Events.Insert(calendarEvent, "primary");
            await request.ExecuteAsync();

            _logger.LogInformation("Successfully created Calendar event '{EventSummary}' for user {UserId}", summary, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Calendar event '{EventSummary}' for user {UserId}", summary, userId);
            return false;
        }
    }

    public async Task<bool> IsAuthorizedAsync(string userId)
    {
        try
        {
            var tokenResponse = await GetValidTokenAsync(userId);
            return tokenResponse != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Calendar authorization for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RevokeAuthorizationAsync(string userId)
    {
        try
        {
            var tokenResponse = await GetValidTokenAsync(userId);
            if (tokenResponse?.AccessToken != null)
            {
                await _flow.RevokeTokenAsync(userId, tokenResponse.AccessToken, CancellationToken.None);
            }

            // Remove from database
            await RemoveTokensFromDatabase(userId);

            _logger.LogInformation("Successfully revoked Calendar authorization for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking Calendar authorization for user {UserId}", userId);
            return false;
        }
    }

    private async Task<TokenResponse?> GetValidTokenAsync(string userId)
    {
        try
        {
            var calendarSetting = await _context.CalendarSettings
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (calendarSetting == null)
            {
                return null;
            }

            var tokenResponse = new TokenResponse
            {
                AccessToken = calendarSetting.AccessToken,
                RefreshToken = calendarSetting.RefreshToken,
                IssuedUtc = calendarSetting.IssuedUtc ?? DateTime.UtcNow,
                ExpiresInSeconds = calendarSetting.ExpiresInSeconds
            };

            // Check if token needs refresh
            if (IsTokenExpired(tokenResponse))
            {
                try
                {
                    var refreshedToken = await _flow.RefreshTokenAsync(userId, tokenResponse.RefreshToken, CancellationToken.None);

                    // Update database with refreshed token
                    await SaveTokensToDatabase(userId, refreshedToken);
                    return refreshedToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing Calendar token for user {UserId}", userId);
                    await RemoveTokensFromDatabase(userId);
                    return null;
                }
            }

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid Calendar token for user {UserId}", userId);
            return null;
        }
    }

    private async Task SaveTokensToDatabase(string userId, TokenResponse tokenResponse)
    {
        try
        {
            var existingSetting = await _context.CalendarSettings
                .FirstOrDefaultAsync(c => c.UserId == userId);

            var now = DateTime.UtcNow;

            if (existingSetting != null)
            {
                // Update existing record
                existingSetting.AccessToken = tokenResponse.AccessToken;
                existingSetting.RefreshToken = tokenResponse.RefreshToken ?? existingSetting.RefreshToken;
                existingSetting.IssuedUtc = tokenResponse.IssuedUtc;
                existingSetting.ExpiresInSeconds = tokenResponse.ExpiresInSeconds;
                existingSetting.UpdatedAt = now;
            }
            else
            {
                // Create new record
                var calendarSetting = new CalendarSetting
                {
                    UserId = userId,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                    IssuedUtc = tokenResponse.IssuedUtc,
                    ExpiresInSeconds = tokenResponse.ExpiresInSeconds,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.CalendarSettings.Add(calendarSetting);
            }

            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Calendar tokens to database for user {UserId}", userId);
            throw;
        }
    }

    private async Task RemoveTokensFromDatabase(string userId)
    {
        try
        {
            var existingSetting = await _context.CalendarSettings
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (existingSetting != null)
            {
                _context.CalendarSettings.Remove(existingSetting);
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Calendar tokens from database for user {UserId}", userId);
            throw;
        }
    }

    private static bool IsTokenExpired(TokenResponse token)
    {
        if (token.ExpiresInSeconds == null)
        {
            return true;
        }

        var expiryTime = token.IssuedUtc.AddSeconds(token.ExpiresInSeconds.Value);
        return DateTime.UtcNow >= expiryTime.AddMinutes(-5); // Refresh 5 minutes before expiry
    }

    private string GetRedirectUri()
    {
        return $"{_systemService.FullHostName}/Calendar/Callback";
    }
}
