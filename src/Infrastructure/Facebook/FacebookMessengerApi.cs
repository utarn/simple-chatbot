using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ChatbotApi.Application.Webhook.Commands.FacebookWebhookCommand;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Infrastructure.Facebook;

public class FacebookMessengerApi : IFacebookMessenger
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FacebookMessengerApi> _logger;

    public FacebookMessengerApi(IHttpClientFactory httpClientFactory, ILogger<FacebookMessengerApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }
    
    public async Task<FacebookReplyStatus> ProcessFacebookMessage(Chatbot chatbot, FacebookReplyMessage message,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://graph.facebook.com/v21.0/");
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            Converters = { new FacebookMessageConverter() },
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var textJson = JsonSerializer.Serialize(message, options);
        var content = new StringContent(textJson, Encoding.UTF8, "application/json");
        var result = await httpClient.PostAsync(
            $"me/messages?access_token={chatbot.FacebookAccessToken}",
            content,
            cancellationToken);
        
        if (result.IsSuccessStatusCode)
        {
            return new FacebookReplyStatus() { Status = 200 };
        }
        else
        {
            var error = await result.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error from Facebook API: {Error}", error);
            return new FacebookReplyStatus() { Status = 500, Error = error };
        }
    }
}

