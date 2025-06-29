using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;

namespace ChatbotApi.Application.Common.Extensions;

public static class HttpClientExtension
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound ||
                             msg.StatusCode == HttpStatusCode.InternalServerError ||
                             msg.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }

}
