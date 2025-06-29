using System.Collections;
using Microsoft.AspNetCore.Identity;

namespace ChatbotApi.Domain.Entities;

/// <summary>
///     Application User
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    ///     ชื่อ
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     โทเคนไลน์
    /// </summary>
    public string? LineAccessToken { get; set; }

    /// <summary>
    ///     ตั้งค่าส่งอีเมล
    /// </summary>
    public bool SendEmail { get; set; }

    /// <summary>
    ///     บัญชีเปิดการใช้งาน
    /// </summary>
    public bool IsEnabled { get; set; }

    public virtual ICollection<Chatbot> Chatbots { get; }

    public ApplicationUser()
    {
        Chatbots = new List<Chatbot>();
    }
}
