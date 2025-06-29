using System.Diagnostics;
using ChatbotApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private readonly ICurrentUserService _user;

    public PerformanceBehaviour(
        ILogger<TRequest> logger,
        ICurrentUserService user)
    {
        _timer = new Stopwatch();

        _logger = logger;
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var email = _user.Email;
            _logger.LogWarning("ChatbotApi.Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Email} {@Request}",
                requestName, elapsedMilliseconds, email, request);
        }

        return response;
    }
}
