using System.Text;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Domain.Entities;
using ChatbotApi.Domain.Models;
using ChatbotApi.Domain.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatbotApi.Infrastructure.Services;

public class GmailService : IGmailService
{
    private readonly GmailSettings _gmailSettings;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GmailService> _logger;
    private readonly ISystemService _systemService;
    private readonly GoogleAuthorizationCodeFlow _flow;

    public GmailService(
        IOptions<GmailSettings> gmailSettings,
        IApplicationDbContext context,
        ILogger<GmailService> logger,
        ISystemService systemService)
    {
        _gmailSettings = gmailSettings.Value;
        _context = context;
        _logger = logger;
        _systemService = systemService;

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _gmailSettings.ClientId,
                ClientSecret = _gmailSettings.ClientSecret
            },
            Scopes = _gmailSettings.Scopes,
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

            _logger.LogInformation("Generated authorization URL for user {UserId}", userId);
            return authUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL for user {UserId}", userId);
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

            _logger.LogInformation("Successfully stored Gmail tokens for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling authorization callback for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string userId, string to, string subject, string body)
    {
        try
        {
            var tokenResponse = await GetValidTokenAsync(userId);
            if (tokenResponse == null)
            {
                _logger.LogWarning("No valid tokens found for user {UserId}", userId);
                return false;
            }

            var credential = new UserCredential(_flow, userId, tokenResponse);

            var gmailApiService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatBot API Gmail Integration"
            });

            var message = CreateMessage(to, subject, body);
            var request = gmailApiService.Users.Messages.Send(message, "me");

            await request.ExecuteAsync();

            _logger.LogInformation("Successfully sent email to {To} for user {UserId}", to, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To} for user {UserId}", to, userId);
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
            _logger.LogError(ex, "Error checking authorization for user {UserId}", userId);
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

            _logger.LogInformation("Successfully revoked Gmail authorization for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking authorization for user {UserId}", userId);
            return false;
        }
    }

    private async Task<TokenResponse?> GetValidTokenAsync(string userId)
    {
        try
        {
            var gmailSetting = await _context.GmailSettings
                .FirstOrDefaultAsync(g => g.UserId == userId);

            if (gmailSetting == null)
            {
                return null;
            }

            var tokenResponse = new TokenResponse
            {
                AccessToken = gmailSetting.AccessToken,
                RefreshToken = gmailSetting.RefreshToken,
                IssuedUtc = gmailSetting.IssuedUtc ?? DateTime.UtcNow,
                ExpiresInSeconds = gmailSetting.ExpiresInSeconds
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
                    _logger.LogError(ex, "Error refreshing token for user {UserId}", userId);
                    await RemoveTokensFromDatabase(userId);
                    return null;
                }
            }

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid token for user {UserId}", userId);
            return null;
        }
    }

    private async Task SaveTokensToDatabase(string userId, TokenResponse tokenResponse)
    {
        try
        {
            var existingSetting = await _context.GmailSettings
                .FirstOrDefaultAsync(g => g.UserId == userId);

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
                var gmailSetting = new GmailSetting
                {
                    UserId = userId,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                    IssuedUtc = tokenResponse.IssuedUtc,
                    ExpiresInSeconds = tokenResponse.ExpiresInSeconds,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.GmailSettings.Add(gmailSetting);
            }

            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tokens to database for user {UserId}", userId);
            throw;
        }
    }

    private async Task RemoveTokensFromDatabase(string userId)
    {
        try
        {
            var existingSetting = await _context.GmailSettings
                .FirstOrDefaultAsync(g => g.UserId == userId);

            if (existingSetting != null)
            {
                _context.GmailSettings.Remove(existingSetting);
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing tokens from database for user {UserId}", userId);
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

    private static Google.Apis.Gmail.v1.Data.Message CreateMessage(string to, string subject, string body)
    {
        var message = new StringBuilder();
        message.AppendLine($"To: {to}");
        message.AppendLine($"Subject: {subject}");
        message.AppendLine("Content-Type: text/html; charset=utf-8");
        message.AppendLine();
        message.AppendLine(body);

        var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message.ToString()))
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");

        return new Google.Apis.Gmail.v1.Data.Message
        {
            Raw = rawMessage
        };
    }

    public async Task<List<GmailEmailInfo>> GetLatestEmailsAsync(string userId)
    {
        try
        {
            var tokenResponse = await GetValidTokenAsync(userId);
            if (tokenResponse == null)
            {
                _logger.LogWarning("No valid tokens found for user {UserId}", userId);
                return new List<GmailEmailInfo>();
            }

            var credential = new UserCredential(_flow, userId, tokenResponse);
            var gmailApiService = new Google.Apis.Gmail.v1.GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatBot API Gmail Integration"
            });

            // Get the stored latest email ID for this user
            var gmailSetting = await _context.GmailSettings
                .FirstOrDefaultAsync(g => g.UserId == userId);

            var latestEmailId = gmailSetting?.LatestEmailId;

            // Create a query to get emails from the last 4 hours
            var fourHoursAgo = DateTime.UtcNow.AddHours(-4);
            var query = $"after:{fourHoursAgo:yyyy/MM/dd}";

            // List messages
            var listRequest = gmailApiService.Users.Messages.List("me");
            listRequest.Q = query;
            listRequest.MaxResults = 100; // Limit to avoid too many results

            var messageList = await listRequest.ExecuteAsync();
            var messages = messageList.Messages ?? new List<Google.Apis.Gmail.v1.Data.Message>();

            var emailInfoList = new List<GmailEmailInfo>();
            string? newLatestEmailId = null;

            // Process messages to find new ones
            foreach (var message in messages.OrderByDescending(m => m.Id))
            {
                // If we have a latest email ID and we've reached it, stop processing
                if (!string.IsNullOrEmpty(latestEmailId) && message.Id == latestEmailId)
                {
                    break;
                }

                // Set the new latest email ID to the first (most recent) message
                newLatestEmailId ??= message.Id;

                // Get full message details
                var messageRequest = gmailApiService.Users.Messages.Get("me", message.Id);
                messageRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

                var fullMessage = await messageRequest.ExecuteAsync();
                var emailInfo = ConvertToGmailEmailInfo(fullMessage);

                emailInfoList.Add(emailInfo);
            }

            // Update the latest email ID if we found new emails
            if (!string.IsNullOrEmpty(newLatestEmailId) && emailInfoList.Any())
            {
                await UpdateLatestEmailId(userId, newLatestEmailId);
            }

            _logger.LogInformation("Retrieved {EmailCount} new emails for user {UserId}", emailInfoList.Count, userId);
            return emailInfoList.OrderBy(e => e.ReceivedDateTime).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest emails for user {UserId}", userId);
            return new List<GmailEmailInfo>();
        }
    }

    private async Task UpdateLatestEmailId(string userId, string latestEmailId)
    {
        try
        {
            var gmailSetting = await _context.GmailSettings
                .FirstOrDefaultAsync(g => g.UserId == userId);

            if (gmailSetting != null)
            {
                gmailSetting.LatestEmailId = latestEmailId;
                gmailSetting.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating latest email ID for user {UserId}", userId);
        }
    }

    private static GmailEmailInfo ConvertToGmailEmailInfo(Google.Apis.Gmail.v1.Data.Message message)
    {
        var emailInfo = new GmailEmailInfo
        {
            Id = message.Id,
            Snippet = message.Snippet ?? string.Empty,
            IsRead = !message.LabelIds?.Contains("UNREAD") ?? true
        };

        // Extract headers
        var headers = message.Payload?.Headers ?? new List<MessagePartHeader>();

        foreach (var header in headers)
        {
            switch (header.Name?.ToLower())
            {
                case "subject":
                    emailInfo.Subject = header.Value ?? string.Empty;
                    break;
                case "from":
                    emailInfo.From = header.Value ?? string.Empty;
                    break;
                case "to":
                    if (!string.IsNullOrEmpty(header.Value))
                    {
                        emailInfo.To = header.Value.Split(',')
                            .Select(email => email.Trim())
                            .ToList();
                    }
                    break;
                case "cc":
                    if (!string.IsNullOrEmpty(header.Value))
                    {
                        emailInfo.Cc = header.Value.Split(',')
                            .Select(email => email.Trim())
                            .ToList();
                    }
                    break;
                case "date":
                    if (DateTime.TryParse(header.Value, out var parsedDate))
                    {
                        emailInfo.ReceivedDateTime = parsedDate.ToUniversalTime();
                    }
                    break;
            }
        }

        // Extract body (simplified - gets plain text or HTML)
        emailInfo.Body = ExtractEmailBody(message.Payload);

        return emailInfo;
    }

    private static string ExtractEmailBody(MessagePart? payload)
    {
        if (payload == null) return string.Empty;

        // If this part has a body
        if (payload.Body?.Data != null)
        {
            return DecodeBase64String(payload.Body.Data);
        }

        // If this part has sub-parts, recursively extract body
        if (payload.Parts != null)
        {
            foreach (var part in payload.Parts)
            {
                // Prefer text/plain, then text/html
                if (part.MimeType == "text/plain" && part.Body?.Data != null)
                {
                    return DecodeBase64String(part.Body.Data);
                }
            }

            // If no plain text found, try HTML
            foreach (var part in payload.Parts)
            {
                if (part.MimeType == "text/html" && part.Body?.Data != null)
                {
                    return DecodeBase64String(part.Body.Data);
                }
            }

            // Recursively search sub-parts
            foreach (var part in payload.Parts)
            {
                var body = ExtractEmailBody(part);
                if (!string.IsNullOrEmpty(body))
                {
                    return body;
                }
            }
        }

        return string.Empty;
    }

    private static string DecodeBase64String(string base64String)
    {
        try
        {
            // Gmail uses URL-safe base64 encoding
            var base64 = base64String.Replace('-', '+').Replace('_', '/');

            // Add padding if necessary
            while (base64.Length % 4 != 0)
            {
                base64 += "=";
            }

            var bytes = Convert.FromBase64String(base64);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetRedirectUri()
    {
        return $"{_systemService.FullHostName}/Gmail/Callback";
    }
}
