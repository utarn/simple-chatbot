namespace ChatbotApi.Domain.Entities;

/// <summary>
/// Provision User
/// </summary>
public class ProvisionUser : BaseEntity
{
    /// <summary>
    ///    อีเมล
    /// </summary>
    /// <example>user@domain.com</example>
    public string Email { get; set; } = default!;

    /// <summary>
    ///     ชื่อ
    /// </summary>
    /// <example>Peter Anderson</example>
    public string Name { get; set; } = default!;

    /// <summary>
    ///     สิทธิ์
    /// </summary>
    /// <example>User</example>
    public string Role { get; set; } = default!;

    public virtual ICollection<Chatbot> Chatbots { get; }

    public ProvisionUser()
    {
        Chatbots = new List<Chatbot>();
    }
}
