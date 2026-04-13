namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Interface for complex asynchronous mapping operations that require external I/O.
/// Use this when mapping requires async operations like file storage access,
/// database queries, or external API calls.
/// </summary>
/// <typeparam name="TSource">The source entity type.</typeparam>
/// <typeparam name="TDestination">The destination DTO type.</typeparam>
public interface IAsyncMappingService<TSource, TDestination>
{
    /// <summary>
    /// Asynchronously maps a source entity to a destination DTO.
    /// </summary>
    /// <param name="source">The source entity to map.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the mapped destination DTO.</returns>
    Task<TDestination> MapAsync(TSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously maps a collection of source entities to destination DTOs.
    /// </summary>
    /// <param name="sources">The source entities to map.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing the mapped destination DTOs.</returns>
    Task<IEnumerable<TDestination>> MapManyAsync(IEnumerable<TSource> sources, CancellationToken cancellationToken = default);
}
