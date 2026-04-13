using System.Diagnostics.CodeAnalysis;

namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Common extension methods for mapping operations.
/// These helpers provide null-safe mapping and collection mapping utilities.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Maps a nullable source to a destination using the provided mapping function.
    /// Returns null if the source is null.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source object to map.</param>
    /// <param name="mappingFunc">The mapping function to apply.</param>
    /// <returns>The mapped object or null.</returns>
    [return: NotNullIfNotNull(nameof(source))]
    public static TDestination? MapIfNotNull<TSource, TDestination>(
        this TSource? source,
        Func<TSource, TDestination> mappingFunc)
        where TSource : class
        where TDestination : class
    {
        return source == null ? null : mappingFunc(source);
    }

    /// <summary>
    /// Maps a collection of items using the provided mapping function.
    /// Returns an empty collection if the source is null or empty.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="sources">The source collection to map.</param>
    /// <param name="mappingFunc">The mapping function to apply to each item.</param>
    /// <returns>A list of mapped items.</returns>
    public static List<TDestination> MapToList<TSource, TDestination>(
        this IEnumerable<TSource>? sources,
        Func<TSource, TDestination> mappingFunc)
    {
        if (sources == null)
        {
            return new List<TDestination>();
        }

        return sources.Select(mappingFunc).ToList();
    }

    /// <summary>
    /// Asynchronously maps a collection of items using the provided async mapping function.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="sources">The source collection to map.</param>
    /// <param name="mappingFunc">The async mapping function to apply to each item.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task containing a list of mapped items.</returns>
    public static async Task<List<TDestination>> MapToListAsync<TSource, TDestination>(
        this IEnumerable<TSource>? sources,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        CancellationToken cancellationToken = default)
    {
        if (sources == null)
        {
            return new List<TDestination>();
        }

        var tasks = sources.Select(source => mappingFunc(source, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Maps a nullable value type to another value type with a default value.
    /// </summary>
    /// <typeparam name="TSource">The source value type.</typeparam>
    /// <typeparam name="TDestination">The destination value type.</typeparam>
    /// <param name="source">The nullable source value.</param>
    /// <param name="mappingFunc">The mapping function to apply.</param>
    /// <param name="defaultValue">The default value to use if source is null.</param>
    /// <returns>The mapped value or default.</returns>
    public static TDestination MapValueOrDefault<TSource, TDestination>(
        this TSource? source,
        Func<TSource, TDestination> mappingFunc,
        TDestination defaultValue = default)
        where TSource : struct
        where TDestination : struct
    {
        return source.HasValue ? mappingFunc(source.Value) : defaultValue;
    }

    /// <summary>
    /// Maps a string value with null/empty handling.
    /// </summary>
    /// <param name="value">The string value to map.</param>
    /// <param name="defaultValue">The default value if string is null or empty.</param>
    /// <returns>The original string or default value.</returns>
    public static string MapStringOrDefault(this string? value, string defaultValue = "")
    {
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    /// <summary>
    /// Safely maps a DateTime? to DateTime with MinValue as default.
    /// Common pattern in the application for Created/Modified dates.
    /// </summary>
    /// <param name="dateTime">The nullable DateTime.</param>
    /// <returns>The DateTime value or DateTime.MinValue.</returns>
    public static DateTime MapDateTimeOrMin(this DateTime? dateTime)
    {
        return dateTime ?? DateTime.MinValue;
    }

    /// <summary>
    /// Creates a data URL from binary content.
    /// Common pattern for image preview generation.
    /// </summary>
    /// <param name="content">The binary content.</param>
    /// <param name="mimeType">The MIME type of the content.</param>
    /// <returns>A data URL string or null if content is null/empty.</returns>
    public static string? ToDataUrl(this byte[]? content, string mimeType = "image/jpeg")
    {
        if (content == null || content.Length == 0)
        {
            return null;
        }

        var base64 = Convert.ToBase64String(content);
        return $"data:{mimeType};base64,{base64}";
    }

    /// <summary>
    /// Converts binary content to base64 string.
    /// </summary>
    /// <param name="content">The binary content.</param>
    /// <returns>Base64 string or null if content is null/empty.</returns>
    public static string? ToBase64String(this byte[]? content)
    {
        if (content == null || content.Length == 0)
        {
            return null;
        }

        return Convert.ToBase64String(content);
    }
}
