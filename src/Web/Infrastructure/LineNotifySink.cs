using Serilog.Core;
using Serilog.Events;

namespace ChatbotApi.Web.Infrastructure;

public class LineNotifySink : ILogEventSink
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _accessToken;
    private readonly string _appName;
    public LineNotifySink(IHttpClientFactory httpClientFactory, string appName, string accessToken)
    {
        _httpClientFactory = httpClientFactory;
        _appName = appName;
        _accessToken = accessToken;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level >= LogEventLevel.Error) 
        {
            var message = logEvent.RenderMessage();
            foreach (var property in logEvent.Properties)
            {
                message += $"{Environment.NewLine}{property.Key}: {property.Value}";
            }
            SendToLineNotify(message).GetAwaiter().GetResult();
        }
    }
  
    private async Task SendToLineNotify(string message)
    {
        using var httpClient = _httpClientFactory.CreateClient("resilient");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://notify-api.line.me/api/notify");
        request.Headers.Add("Authorization", $"Bearer {_accessToken}");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "message", $"{_appName} error exception: {message}" }
        });

        var response = await httpClient.SendAsync(request);
    }
}
