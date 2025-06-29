using System;

using Microsoft.AspNetCore.Http;

namespace Utharn.Library.Localizer;

internal class LocalizerService : ILocalizerService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _language;

    public LocalizerService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetCurrentLanguage()
    {
        _language = _httpContextAccessor.HttpContext?.Request.Cookies["Culture"];
        _language ??= "th-TH";
        return _language;
    }

    public bool IsThai()
    {
        if (GetCurrentLanguage() == "th-TH" || GetCurrentLanguage() == null)
        {
            return true;
        }

        return false;
    }

    public bool IsEng()
    {
        return !IsThai();
    }

    public void SetLanguage(string culture)
    {
        _language = null;
        Remove("Culture");
        Set("Culture", culture, 600);
    }

    public void SetThai()
    {
        _language = null;
        Remove("Culture");
        Set("Culture", "th-TH", 600);
    }

    public void SetEnglish()
    {
        _language = null;
        Remove("Culture");
        Set("Culture", "en-US", 600);
    }

    private void Set(string key, string value, int? expireTime)
    {
        CookieOptions option = new()
        {
            HttpOnly = true, IsEssential = true, Secure = false, SameSite = SameSiteMode.Strict
        };

        if (expireTime.HasValue)
        {
            option.Expires = DateTime.Now.AddMinutes(expireTime.Value);
        }
        else
        {
            option.Expires = DateTime.Now.AddMinutes(30);
        }

        _httpContextAccessor.HttpContext?.Response.Cookies.Append(key, value, option);
    }

    private void Remove(string key)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(key);
    }
}