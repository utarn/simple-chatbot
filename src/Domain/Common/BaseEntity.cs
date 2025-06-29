using System.ComponentModel.DataAnnotations.Schema;

namespace ChatbotApi.Domain.Common;

/// <summary>
/// Base Entity
/// </summary>
public abstract class BaseEntity
{
    // This can easily be modified to be BaseEntity<T> and public T Id to support different key types.
    // Using non-generic integer types for simplicity
    /// <summary>
    ///    ไอดี
    /// </summary>
    public int Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    private readonly List<ChainEvent> _chainEvents = new();
    /// <summary>
    ///    อีเวนท์
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    [NotMapped]
    public IReadOnlyCollection<ChainEvent> ChainEvents => _chainEvents.AsReadOnly();
    /// <summary>
    ///   เพิ่มอีเวนท์
    /// </summary>
    /// <param name="domainEvent"></param>
    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void AddChainEvent(ChainEvent chainEvent)
    {
        _chainEvents.Add(chainEvent);
    }

    /// <summary>
    ///  ลบอีเวนท์
    /// </summary>
    /// <param name="domainEvent"></param>
    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void RemoveChainEvent(ChainEvent chainEvent)
    {
        _chainEvents.Remove(chainEvent);
    }

    /// <summary>
    ///  ล้างอีเวนท์
    /// </summary>
    public void ClearDomainEvents(bool isBefore)
    {
        _domainEvents.RemoveAll(baseEvent => baseEvent.IsBeforeSave == isBefore);
    }

    public void ClearChainEvents()
    {
        _chainEvents.Clear();
    }
}
