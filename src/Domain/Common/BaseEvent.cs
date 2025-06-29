using MediatR;

namespace ChatbotApi.Domain.Common;

/// <summary>
/// Base Event
/// </summary>
public abstract class BaseEvent : INotification
{
    public bool IsBeforeSave { get; set; } 
    public bool IsBackground { get; set; } 
    public string? QueueName { get; set; } = "normal";
}
