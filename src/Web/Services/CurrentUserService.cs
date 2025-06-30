using System.Net;
using System.Security.Claims;
using ChatbotApi.Application.Common.Extensions;
using ChatbotApi.Application.Common.Interfaces;

namespace ChatbotApi.Web.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public string Role => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    public string Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    
    public IPAddress? IPAddress => _httpContextAccessor.HttpContext?.GetRemoteIpAddress();
}

public static class HttpContextExtensions
{
    /// <summary>
    ///     Get remote ip address, optionally allowing for x-forwarded-for header check
    /// </summary>
    /// <param name="context">Http context</param>
    /// <param name="allowForwarded">Whether to allow x-forwarded-for header check</param>
    /// <returns>IPAddress</returns>
    public static IPAddress? GetRemoteIpAddress(this HttpContext context, bool allowForwarded = true)
    {
        if (allowForwarded)
        {
            // if you are allowing these forward headers, please ensure you are restricting context.Connection.RemoteIpAddress
            // to cloud flare ips: https://www.cloudflare.com/ips/
            string header = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                            context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
                            context.Request.Headers["REMOTE_ADDR"].FirstOrDefault() ?? string.Empty;
            if (IPAddress.TryParse(header, out IPAddress? ip))
            {
                return ip;
            }
        }

        return context.Connection.RemoteIpAddress;
    }
}
