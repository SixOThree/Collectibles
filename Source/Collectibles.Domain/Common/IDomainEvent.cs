namespace Collectibles.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset DateOccurred { get; }
    bool IsPublished { get; set; }
}
