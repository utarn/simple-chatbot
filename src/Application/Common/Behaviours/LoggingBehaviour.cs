using ChatbotApi.Application.Common.Interfaces;
using MediatR.Pipeline;
using Microsoft.Extensions.Logging;

namespace ChatbotApi.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest> where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly ICurrentUserService _user;

    public LoggingBehaviour(ILogger<TRequest> logger, ICurrentUserService user)
    {
        _logger = logger;
        _user = user;
    }

    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var email = _user.Email;
        string ipAddress = _user.IPAddress?.ToString() ?? string.Empty;

        _logger.LogInformation("ChatbotApi.Request: {IpAddress} {Name} {@Email} {@Request}",
            ipAddress, requestName, email, request);
        return Task.CompletedTask;
    }
}
