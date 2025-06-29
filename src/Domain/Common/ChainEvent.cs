using MediatR;

namespace ChatbotApi.Domain.Common;

public class ChainEvent : INotification
{
    public IList<BaseEvent> Events { get; } = new List<BaseEvent>();
}
