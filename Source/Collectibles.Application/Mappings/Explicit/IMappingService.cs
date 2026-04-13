namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Interface for complex mapping operations that require dependency injection.
/// Use this when mapping requires external services like IFileStorage, ILogger, etc.
/// For simple property-to-property mappings, use extension methods instead.
/// </summary>
/// <typeparam name="TSource">The source entity type.</typeparam>
/// <typeparam name="TDestination">The destination DTO type.</typeparam>
public interface IMappingService<TSource, TDestination>
{
    /// <summary>
    /// Maps a source entity to a destination DTO.
    /// </summary>
    /// <param name="source">The source entity to map.</param>
    /// <returns>The mapped destination DTO.</returns>
    TDestination Map(TSource source);

    /// <summary>
    /// Maps a collection of source entities to destination DTOs.
    /// </summary>
    /// <param name="sources">The source entities to map.</param>
    /// <returns>The mapped destination DTOs.</returns>
    IEnumerable<TDestination> MapMany(IEnumerable<TSource> sources);
}
