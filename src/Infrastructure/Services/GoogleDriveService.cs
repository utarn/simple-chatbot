using ChatbotApi.Domain.Settings;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using File = Google.Apis.Drive.v3.Data.File;

namespace ChatbotApi.Infrastructure.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly ISystemService _systemService;
    private readonly GoogleAuthorizationCodeFlow _flow;

    public GoogleDriveService(
        IOptions<DriveSettings> driveSettings,
        IApplicationDbContext context,
        ILogger<GoogleDriveService> logger,
        ISystemService systemService)
    {
        DriveSettings driveSettings1 = driveSettings.Value;
        _context = context;
        _logger = logger;
        _systemService = systemService;

        _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = driveSettings1.ClientId,
                ClientSecret = driveSettings1.ClientSecret
            },
            Scopes = driveSettings1.Scopes,
            DataStore = null // We'll use database instead
        });
    }

    public string GetAuthorizationUrl(string userId)
    {
        try
        {
            var redirectUri = GetRedirectUri();
            var request = _flow.CreateAuthorizationCodeRequest(redirectUri);
            request.State = userId;
            var authUri = request.Build();

            _logger.LogInformation("Generated Drive authorization URL for user {UserId}", userId);
            return authUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Drive authorization URL for user {UserId}", userId);
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

            await SaveTokensToDatabase(userId, tokenResponse);

            _logger.LogInformation("Successfully stored Drive tokens for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Drive authorization callback for user {UserId}", userId);
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
            _logger.LogError(ex, "Error checking Drive authorization for user {UserId}", userId);
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

            await RemoveTokensFromDatabase(userId);

            _logger.LogInformation("Successfully revoked Drive authorization for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking Drive authorization for user {UserId}", userId);
            return false;
        }
    }

    private async Task<TokenResponse?> GetValidTokenAsync(string userId)
    {
        try
        {
            var driveSetting = await _context.DriveSettings
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (driveSetting == null)
            {
                return null;
            }

            var tokenResponse = new TokenResponse
            {
                AccessToken = driveSetting.AccessToken,
                RefreshToken = driveSetting.RefreshToken,
                IssuedUtc = driveSetting.IssuedUtc ?? DateTime.UtcNow,
                ExpiresInSeconds = driveSetting.ExpiresInSeconds
            };

            if (IsTokenExpired(tokenResponse))
            {
                try
                {
                    var refreshedToken = await _flow.RefreshTokenAsync(userId, tokenResponse.RefreshToken, CancellationToken.None);
                    await SaveTokensToDatabase(userId, refreshedToken);
                    return refreshedToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing Drive token for user {UserId}", userId);
                    await RemoveTokensFromDatabase(userId);
                    return null;
                }
            }

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid Drive token for user {UserId}", userId);
            return null;
        }
    }

    private async Task SaveTokensToDatabase(string userId, TokenResponse tokenResponse)
    {
        try
        {
            var existingSetting = await _context.DriveSettings
                .FirstOrDefaultAsync(d => d.UserId == userId);

            var now = DateTime.UtcNow;

            if (existingSetting != null)
            {
                existingSetting.AccessToken = tokenResponse.AccessToken;
                existingSetting.RefreshToken = tokenResponse.RefreshToken ?? existingSetting.RefreshToken;
                existingSetting.IssuedUtc = tokenResponse.IssuedUtc;
                existingSetting.ExpiresInSeconds = tokenResponse.ExpiresInSeconds;
                existingSetting.UpdatedAt = now;
            }
            else
            {
                var driveSetting = new DriveSetting
                {
                    UserId = userId,
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
                    IssuedUtc = tokenResponse.IssuedUtc,
                    ExpiresInSeconds = tokenResponse.ExpiresInSeconds,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.DriveSettings.Add(driveSetting);
            }

            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Drive tokens to database for user {UserId}", userId);
            throw;
        }
    }

    private async Task RemoveTokensFromDatabase(string userId)
    {
        try
        {
            var existingSetting = await _context.DriveSettings
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (existingSetting != null)
            {
                _context.DriveSettings.Remove(existingSetting);
                await _context.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Drive tokens from database for user {UserId}", userId);
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
        return DateTime.UtcNow >= expiryTime.AddMinutes(-5);
    }

    private string GetRedirectUri()
    {
        return $"{_systemService.FullHostName}/GoogleDrive/Callback";
    }

    public async Task<IList<File>> ListRootFilesAsync(string userId)
    {
        try
        {
            var token = await GetValidTokenAsync(userId);
            if (token == null)
            {
                _logger.LogWarning("No valid Google Drive token found for user {UserId}", userId);
                return new List<File>();
            }

            var credential = new UserCredential(_flow, userId, token);

            using var driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatbotApi"
            });

            var allFiles = new List<File>();
            string? pageToken = null;

            do
            {
                var request = driveService.Files.List();
                request.Q = "'root' in parents and trashed = false";
                request.Fields = "nextPageToken, files(id, name, mimeType, modifiedTime, size)";
                request.PageSize = 100;
                request.PageToken = pageToken;

                var result = await request.ExecuteAsync();
                if (result.Files != null)
                    allFiles.AddRange(result.Files);

                pageToken = result.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return allFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing root files for user {UserId}", userId);
            throw;
        }
    }
}
