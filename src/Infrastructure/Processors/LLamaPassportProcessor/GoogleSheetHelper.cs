using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatbotApi.Infrastructure.Processors.LLamaPassportProcessor
{
    public partial class GoogleSheetHelper
    {
        private readonly SheetsService _sheetsService;
        private readonly ILogger<GoogleSheetHelper> _logger;

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

        public GoogleSheetHelper(ILogger<GoogleSheetHelper> logger)
        {
            _logger = logger;

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
                    HttpClientInitializer = credential, ApplicationName = "ChatbotPassportOCR",
                });
                _logger.LogInformation("Google Sheets service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to initialize Google Sheets service. Make sure SERVICE_ACCOUNT_JSON is correct and dependencies are installed");
                throw new InvalidOperationException("Failed to initialize Google Sheets service.", ex);
            }
        }

        public async Task AppendRowAsync(string spreadsheetId, string range, IList<object> rowData,
            CancellationToken cancellationToken)
        {
            if (_sheetsService == null)
            {
                _logger.LogError(
                    "Google Sheets service is not initialized. Cannot append row to Spreadsheet ID: {SpreadsheetId}",
                    spreadsheetId);
                throw new InvalidOperationException("Google Sheets service is not initialized.");
            }

            try
            {
                var valueRange = new ValueRange { Values = new List<IList<object>> { rowData } };

                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, range);
                appendRequest.ValueInputOption =
                    SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

                var response = await appendRequest.ExecuteAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully appended row to Google Sheet {SpreadsheetId} Range {Range}. Updates: {Updates}",
                    spreadsheetId, range, response.Updates?.UpdatedCells);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to append row to Google Sheet {SpreadsheetId} Range {Range}",
                    spreadsheetId, range);
                throw;
            }
        }
        
    }
}
