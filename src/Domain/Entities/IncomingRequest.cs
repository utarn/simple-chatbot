using ChatbotApi.Domain.Common;

namespace ChatbotApi.Domain.Entities;

public class IncomingRequest : BaseAuditableEntity
{
    public string? Raw { get; set; }
    public string? Endpoint { get; set; }
    public string? Channel { get; set; }
}