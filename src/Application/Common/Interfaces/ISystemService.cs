using Microsoft.AspNetCore.Http;

namespace ChatbotApi.Application.Common.Interfaces;

/// <summary>
/// ระบบ
/// </summary>
public interface ISystemService
{
    /// <summary>
    /// วันเวลาปัจจุบัน
    /// </summary>
    DateTime Now { get; }
    /// <summary>
    /// วันเวลาปัจจุบัน (UTC)
    /// </summary>
    DateTime UtcNow { get; }
    /// <summary>
    /// ชื่อโฮสต์
    /// </summary>
    string HostName { get; }
    /// <summary>
    /// http
    /// </summary>
    string Scheme { get; }
    /// <summary>
    /// ชื่อโฮสต์เต็ม
    /// </summary>
    string FullHostName { get; }
    /// <summary>
    /// Context
    /// </summary>
    HttpContext? HttpContext { get; }
    /// <summary>
    /// ลิงก์
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    string GetLink(string controller, string action);
}
