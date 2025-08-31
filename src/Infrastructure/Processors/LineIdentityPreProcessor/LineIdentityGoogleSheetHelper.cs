using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ChatbotApi.Infrastructure.Processors.LineIdentityPreProcessor;

public class LineIdentityGoogleSheetHelper
{
    private readonly SheetsService _sheetsService;
    private readonly ILogger<LineIdentityPreProcessor> _logger;
    private readonly IDistributedCache _cache;
    private const string CACHE_KEY = "line_identity_sheet_data";
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromMinutes(5);

    // Hardcoded sheet ID and service account JSON as requested
    private const string SHEET_ID = "1KQGQAPXBC6CWN0u3vLQlvnzzuCVoEbOk1gEQWxAL-M8";
    private const string RANGE = "Sheet1!A:G";

        private const string SERVICE_ACCOUNT_JSON = @"
        {
          ""type"": ""service_account"",
          ""project_id"": ""perfectcomsolution-trusty"",
          ""private_key_id"": ""9aff101eb30877f27405ca1941a5abd073496ab0"",
          ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQDRgjCa5rZCUq2Q\ngxXUljOihgkAw0Qhqb3Lbh32rPfzrfBM14n7TzG+yiPLQ54EiC1ir8OxJOFnQsew\n+bdbyaqMA6l04/5Pi00kuKBJ7z5z2WjO2TL4pKFpeAw9XME2yJl5vWFXJsBRdd06\nf1T8CxbpUOCQ/Gs0z1ZSrHcIFUaT4KJJF0tjRu2RTr95dk58oOt625faFfPpoRcI\nDj0wZeuAmBX96kGXUnOLvkOMPxkWcPPHE57KQ8VskVrs/2EQb2/WVuzPnd4MqbaW\n/R20CqGlUWVClNLF4CO+4CskbJyszuOBC3AacMcY3M0iUCpUOXmCgBhNmkcMn5K2\nPjwO4JPpAgMBAAECggEAELHJWjqKqO/KJVMQvQMoA6ofEwi8R9ttBIYWjKa9TlVc\nqd7d/6DQo7WbUxHlCFLqnOvJEfdQn8gWPf+0EPQZqzUKfoZBaEi/Ia81lJakGRqo\nq5zqnx4NP6iBfy1CNzGWazlARa/QkN0tvwDQ/pGKppZbgqoeh9OCuy1DgekiGdve\nRngfGc/vCwuXm+OldVu1Kkb2iXqIGyQoLV281UnwytcoBqQOQQBRQ1wxhCGu8peM\n5HrSYjQs/SjmLR+gDghFHyN/B2F9QT6ARQIx9Pw3hpDlDV3/CnkQESzhZU+kAHc3\neSUVe7GbnC7hycVDoHpbrkB1YAY1t5nQmA7J11HRwQKBgQD5+GZ+/j42xSM8Jmae\nNxTlGhnN0oo30ejZ4yZPzUB4phW1tqRFQcQhI3WqwBirBnUoz/FanSlkjkseqsmp\nR6qcVbRkFc8Af9JXlNeUtWu0azFH7XzH06U+tJT0fpuCM9e0mZwsAoj7WZ1vLuhw\nerbNndco9NT5oBgA20p0dVCP4QKBgQDWj+7LWA3U4p1uJfYxXkg9beaNQi8RkIgF\n+EnsO+dfLq5OMxjkOa8an/+DOXtRdRoNQ8rOqMe4MewFLrJrKz+XfjPcZgF5FRhM\nfX9bB1BCeRWnsZHXCNLbndM7JfS58ebmI54M3q5jvzkxFs7b5SdWaEyMcpzltGrV\neUidkkAlCQKBgQCKzVLku3qCYS8yjEQ5IG7a1IZ1kq4rVsTMkGRKtbdSBy9Q6q0G\nxAELQaxp9yb7eKd/1Q+4+EHu01CFI+K8u83R54k2diGurkt3VG/s5Fx9H3SK8yVx\ntGUyj4WSyebCAtWJNC7TBUlZAKb6APsS0iFFxZqe5GyKfEo314zdY/MrIQKBgQCy\n+Y7cSdAH0xw1BC9vkNC7hQ/6pslyYlhEeo7XIkTmjZ7SFideQIvCrtHJGUq3cPHR\nPMpQRlOKXwIcdI5ZfNLnwFrsLp5t7N2++DQir2AQgsZAgos/jtmsXeMUBJ41+QV8\n1RsCa0GWbKz9OKRGosiEeC3aPcSIi01OUoPzBErDWQKBgDxCHEqByXuOS9uiXAGt\nRUWwSLZISX06eTubVC+0dMKef/KUYxYr14owf1wXMplFVZymZBGOULc1IHWuOLpG\nZ8d3VUA1HkvscHta978l+BW6ZTvVJir1Uv4sb0mQjis3Eqtp2z8PKlIeMCUj4oeg\nmfbUyXFOxM2I5jXoFbEJ28ti\n-----END PRIVATE KEY-----\n"",
          ""client_email"": ""passport-ai@perfectcomsolution-trusty.iam.gserviceaccount.com"",
          ""client_id"": ""115325371081409817511"",
          ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
          ""token_uri"": ""https://oauth2.googleapis.com/token"",
          ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
          ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/passport-ai%40perfectcomsolution-trusty.iam.gserviceaccount.com"",
          ""universe_domain"": ""googleapis.com""
        }";

    public LineIdentityGoogleSheetHelper(ILogger<LineIdentityPreProcessor> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;

        try
        {
            GoogleCredential credential;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SERVICE_ACCOUNT_JSON)))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(SheetsService.Scope.Spreadsheets);
            }

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "ChatbotLineIdentity",
            });
            _logger.LogInformation("Google Sheets service initialized successfully for LineIdentityPreProcessor");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Sheets service for LineIdentityPreProcessor");
            throw new InvalidOperationException("Failed to initialize Google Sheets service.", ex);
        }
    }

    private async Task<CachedSheetData?> GetCachedSheetDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await _cache.GetStringAsync(CACHE_KEY, cancellationToken);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                var cachedData = JsonSerializer.Deserialize<CachedSheetData>(cachedJson);
                if (cachedData != null && !cachedData.IsExpired(CACHE_EXPIRATION))
                {
                    _logger.LogInformation("Using cached sheet data from {CachedAt}", cachedData.CachedAt);
                    return cachedData;
                }
                else
                {
                    _logger.LogInformation("Cached sheet data expired, will refresh");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cached sheet data");
        }
        
        return null;
    }

    private async Task<CachedSheetData?> LoadAndCacheSheetDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Values.Get(SHEET_ID, RANGE);
            var response = await request.ExecuteAsync(cancellationToken);

            if (response.Values == null || response.Values.Count == 0)
            {
                _logger.LogWarning("No data found in Google Sheet {SheetId}", SHEET_ID);
                return null;
            }

            var users = new List<LineUserIdentity>();

            // Skip header row (index 0) and process all data rows
            for (int i = 1; i < response.Values.Count; i++)
            {
                var row = response.Values[i];
                if (row.Count > 0 && !string.IsNullOrEmpty(row[0]?.ToString()))
                {
                    users.Add(new LineUserIdentity
                    {
                        LineUserId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
                        Initial = row.Count > 1 ? row[1]?.ToString() ?? "" : "",
                        FirstName = row.Count > 2 ? row[2]?.ToString() ?? "" : "",
                        LastName = row.Count > 3 ? row[3]?.ToString() ?? "" : "",
                        Group = row.Count > 4 ? row[4]?.ToString() ?? "" : "",
                        Faculty = row.Count > 5 ? row[5]?.ToString() ?? "" : "",
                        Campus = row.Count > 6 ? row[6]?.ToString() ?? "" : ""
                    });
                }
            }

            var cachedData = new CachedSheetData
            {
                Users = users,
                CachedAt = DateTime.UtcNow
            };

            // Cache the data
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CACHE_EXPIRATION
            };

            var serializedData = JsonSerializer.Serialize(cachedData);
            await _cache.SetStringAsync(CACHE_KEY, serializedData, options, cancellationToken);

            _logger.LogInformation("Cached {UserCount} users from Google Sheet", users.Count);
            return cachedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load and cache sheet data");
            return null;
        }
    }

    public async Task<LineUserIdentity?> GetUserIdentityAsync(string lineUserId, CancellationToken cancellationToken)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service is not initialized. Cannot get user identity for LineUserId: {LineUserId}", lineUserId);
            return null;
        }

        try
        {
            // Try to get cached data first
            var cachedData = await GetCachedSheetDataAsync(cancellationToken);
            
            // If no valid cached data, load from sheet
            if (cachedData == null)
            {
                cachedData = await LoadAndCacheSheetDataAsync(cancellationToken);
                if (cachedData == null)
                {
                    return null;
                }
            }

            // Search for user in cached data
            var userIdentity = cachedData.Users.FirstOrDefault(u => u.LineUserId == lineUserId);
            
            if (userIdentity != null)
            {
                _logger.LogInformation("Found user identity from cache for LineUserId: {LineUserId}", lineUserId);
            }
            else
            {
                _logger.LogInformation("LineUserId {LineUserId} not found in cached data", lineUserId);
            }

            return userIdentity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user identity from Google Sheet for LineUserId: {LineUserId}", lineUserId);
            return null;
        }
    }

    public async Task<bool> AddUserIdentityAsync(string lineUserId, string profileName, CancellationToken cancellationToken)
    {
        if (_sheetsService == null)
        {
            _logger.LogError("Google Sheets service is not initialized. Cannot add user identity for LineUserId: {LineUserId}", lineUserId);
            return false;
        }

        try
        {
            var rowData = new List<object>
            {
                lineUserId,       // Column A: LineUserId
                "",               // Column B: Initial
                profileName,      // Column C: FirstName (using profile name)
                "",               // Column D: LastName
                "",               // Column E: Group
                "",               // Column F: Faculty
                ""                // Column G: Campus
            };

            var valueRange = new ValueRange { Values = new List<IList<object>> { rowData } };

            var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, SHEET_ID, RANGE);
            appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

            var response = await appendRequest.ExecuteAsync(cancellationToken);

            // Invalidate cache when new user is added
            await _cache.RemoveAsync(CACHE_KEY, cancellationToken);

            _logger.LogInformation("Successfully added new user identity to Google Sheet and invalidated cache. LineUserId: {LineUserId}, ProfileName: {ProfileName}, Updates: {Updates}",
                lineUserId, profileName, response.Updates?.UpdatedCells);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add user identity to Google Sheet. LineUserId: {LineUserId}, ProfileName: {ProfileName}",
                lineUserId, profileName);
            return false;
        }
    }

    public async Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(CACHE_KEY, cancellationToken);
            _logger.LogInformation("Cache invalidated for line identity sheet data");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating cache");
        }
    }
}