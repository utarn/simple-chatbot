using ChatbotApi.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChatbotApi.Infrastructure.Services;

public class SystemService : ISystemService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly TimeProvider _dateTime;
    private readonly TimeZoneInfo _timeZone;
    
    public SystemService(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, TimeProvider dateTime)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _dateTime = dateTime;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    }

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(_dateTime.GetUtcNow().UtcDateTime, _timeZone); 
    public DateTime UtcNow => _dateTime.GetUtcNow().UtcDateTime;
    public string HostName => _httpContextAccessor.HttpContext?.Request.Host.Value ?? "localhost";
    public string Scheme => _httpContextAccessor.HttpContext?.Request.Scheme ?? "http";
    public string FullHostName => $"{Scheme}://{HostName}";

    public HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    public string GetLink(string controller, string action)
    {
        if (_httpContextAccessor.HttpContext == null) return string.Empty;
        return _linkGenerator.GetUriByAction(_httpContextAccessor.HttpContext, action, controller) ?? string.Empty;
    }
}
