namespace Collectibles.Domain.Common.Entities;

public abstract class BaseEntity : IEntity<long>
{
    public virtual long Id { get; set; }

    private readonly List<DomainEvent> _domainEvents = new();

    // This collection is intentionally not persisted - it's used for in-memory domain event processing
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
