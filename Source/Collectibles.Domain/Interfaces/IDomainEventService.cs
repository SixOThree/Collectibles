using Collectibles.Domain.Common;

namespace Collectibles.Domain.Interfaces;

/// <summary>
/// Service for publishing domain events.
/// </summary>
public interface IDomainEventService
{
    Task PublishAsync(IDomainEvent domainEvent);
}
