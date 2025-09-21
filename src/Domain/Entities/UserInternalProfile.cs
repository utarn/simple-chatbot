using ChatbotApi.Domain.Common;

namespace ChatbotApi.Domain.Entities;

public class UserInternalProfile : BaseEntity
{
    public string? LineUserId { get; set; }
    public string? Initial { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Group { get; set; }
    public string? Faculty { get; set; }
    public string? Campus { get; set; }
}