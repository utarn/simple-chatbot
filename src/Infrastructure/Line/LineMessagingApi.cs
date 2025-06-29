using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatbotApi.Application.Common.Interfaces;
using ChatbotApi.Application.Common.Models;
using ChatbotApi.Domain.Entities;

namespace ChatbotApi.Infrastructure.Line;

public class LineMessagingApi : ILineMessenger
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LineMessagingApi> _logger;

    public LineMessagingApi(IHttpClientFactory httpClientFactory, ILogger<LineMessagingApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }


    public async Task<LineSendResponse> SendMessage(Chatbot chatbot, LineReplyMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message.Messages.Count == 0)
        {
            return new LineSendResponse() { Status = 400, Error = "No message to send" };
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", chatbot.LineChannelAccessToken);

        var json = JsonSerializer.Serialize(message, LineMessageConverter.Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await httpClient.PostAsync("https://api.line.me/v2/bot/message/reply", content,
            cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var response = await result.Content.ReadAsStringAsync(cancellationToken);
            var sentResponse = JsonSerializer.Deserialize<SentResponse>(response)!;

            var sentMessages = new List<SentMessageData>();
            for (int i = 0; i < sentResponse.SentMessages.Count; i++)
            {
                var sentMessageData = new SentMessageData()
                {
                    MessageId = sentResponse.SentMessages[i].Id,
                    QuoteToken = sentResponse.SentMessages[i].QuoteToken,
                };

                if (message.Messages[i] is LineTextMessage textMessage)
                {
                    sentMessageData.AllText = textMessage.Text;
                }
                else if (message.Messages[i] is LineTextMessageV2 textMessageV2)
                {
                    sentMessageData.AllText = textMessageV2.Text;
                }
                sentMessages.Add(sentMessageData);
            }

            return new LineSendResponse() { Status = 200, SentMessages = sentMessages };
        }
        else
        {
            var validate = await httpClient.PostAsync("https://api.line.me/v2/bot/message/validate/reply", content,
                cancellationToken);
            if (validate.IsSuccessStatusCode)
            {
                var validateResult = await validate.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Validate result: {Result}", validateResult);
            }

            var error = await result.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error from LINE API: {Error}", error);
            return new LineSendResponse() { Status = 500, Error = error };
        }
    }

    public async Task<LineSendResponse> SendRawMessage(Chatbot chatbot, string json,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(json))
        {
            return new LineSendResponse() { Status = 400, Error = "No message to send" };
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", chatbot.LineChannelAccessToken);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await httpClient.PostAsync("https://api.line.me/v2/bot/message/reply", content,
            cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            return new LineSendResponse() { Status = 200 };
        }
        else
        {
            var validate = await httpClient.PostAsync("https://api.line.me/v2/bot/message/validate/reply", content,
                cancellationToken);
            if (validate.IsSuccessStatusCode)
            {
                var validateResult = await validate.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Validate result: {Result}", validateResult);
            }

            var error = await result.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error from LINE API: {Error}", error);
            return new LineSendResponse() { Status = 500, Error = error };
        }
    }

    public async Task<LineSendResponse> SendPushMessage(Chatbot chatbot, LinePushMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message.Messages.Count == 0)
        {
            return new LineSendResponse() { Status = 400, Error = "No message to send" };
        }

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", chatbot.LineChannelAccessToken);

        var json = JsonSerializer.Serialize(message, LineMessageConverter.Options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var result = await httpClient.PostAsync("https://api.line.me/v2/bot/message/push", content,
            cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            var response = await result.Content.ReadAsStringAsync(cancellationToken);
            var sentResponse = JsonSerializer.Deserialize<SentResponse>(response)!;

            var sentMessages = new List<SentMessageData>();
            for (int i = 0; i < sentResponse.SentMessages.Count; i++)
            {
                var sentMessageData = new SentMessageData()
                {
                    MessageId = sentResponse.SentMessages[i].Id,
                    QuoteToken = sentResponse.SentMessages[i].QuoteToken,
                };

                if (message.Messages[i] is LineTextMessage textMessage)
                {
                    sentMessageData.AllText = textMessage.Text;
                }
                else if (message.Messages[i] is LineTextMessageV2 textMessageV2)
                {
                    sentMessageData.AllText = textMessageV2.Text;
                }
                sentMessages.Add(sentMessageData);
            }

            return new LineSendResponse() { Status = 200, SentMessages = sentMessages };
        }
        else
        {
            var validate = await httpClient.PostAsync("https://api.line.me/v2/bot/message/validate/push", content,
                cancellationToken);
            if (validate.IsSuccessStatusCode)
            {
                var validateResult = await validate.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Validate result: {Result}", validateResult);
            }

            var error = await result.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error from LINE API: {Error}", error);
            return new LineSendResponse() { Status = 500, Error = error };
        }
    }
}
