namespace Collectibles.Application.Mappings.Explicit;

/// <summary>
/// Helper methods for handling asynchronous mapping operations.
/// Provides utilities for batching, parallel processing, and error handling.
/// </summary>
public static class AsyncMappingHelpers
{
    /// <summary>
    /// Maps a collection of items in parallel with a specified degree of parallelism.
    /// Useful for I/O-bound operations like file loading.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="sources">The source collection to map.</param>
    /// <param name="mappingFunc">The async mapping function.</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of mapped items.</returns>
    public static async Task<List<TDestination>> MapInParallelAsync<TSource, TDestination>(
        this IEnumerable<TSource> sources,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        int maxDegreeOfParallelism = 5,
        CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        var tasks = new List<Task<TDestination>>();

        foreach (var source in sources)
        {
            tasks.Add(MapWithSemaphoreAsync(source, mappingFunc, semaphore, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    private static async Task<TDestination> MapWithSemaphoreAsync<TSource, TDestination>(
        TSource source,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await mappingFunc(source, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Maps items in batches to avoid overwhelming resources.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="sources">The source collection to map.</param>
    /// <param name="mappingFunc">The async mapping function.</param>
    /// <param name="batchSize">Size of each batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of mapped items.</returns>
    public static async Task<List<TDestination>> MapInBatchesAsync<TSource, TDestination>(
        this IEnumerable<TSource> sources,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TDestination>();
        var sourceList = sources.ToList();

        for (int i = 0; i < sourceList.Count; i += batchSize)
        {
            var batch = sourceList.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(source => mappingFunc(source, cancellationToken));
            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);
        }

        return results;
    }

    /// <summary>
    /// Maps a single item with a timeout.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source item to map.</param>
    /// <param name="mappingFunc">The async mapping function.</param>
    /// <param name="timeout">Timeout duration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped item.</returns>
    /// <exception cref="TimeoutException">Thrown when the operation times out.</exception>
    public static async Task<TDestination> MapWithTimeoutAsync<TSource, TDestination>(
        this TSource source,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            return await mappingFunc(source, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Mapping operation timed out after {timeout.TotalSeconds} seconds");
        }
    }

    /// <summary>
    /// Maps an item with retry logic for transient failures.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source item to map.</param>
    /// <param name="mappingFunc">The async mapping function.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="delayBetweenRetries">Delay between retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped item.</returns>
    public static async Task<TDestination> MapWithRetryAsync<TSource, TDestination>(
        this TSource source,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        int maxRetries = 3,
        TimeSpan? delayBetweenRetries = null,
        CancellationToken cancellationToken = default)
    {
        var delay = delayBetweenRetries ?? TimeSpan.FromSeconds(1);
        var lastException = default(Exception);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await mappingFunc(source, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                lastException = ex;
                await Task.Delay(delay, cancellationToken);

                // Exponential backoff
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }

        throw new InvalidOperationException(
            $"Mapping failed after {maxRetries} retries",
            lastException);
    }

    /// <summary>
    /// Maps an item with a fallback value if mapping fails.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <param name="source">The source item to map.</param>
    /// <param name="mappingFunc">The async mapping function.</param>
    /// <param name="fallbackFunc">Function to create fallback value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped item or fallback value.</returns>
    public static async Task<TDestination> MapWithFallbackAsync<TSource, TDestination>(
        this TSource source,
        Func<TSource, CancellationToken, Task<TDestination>> mappingFunc,
        Func<TSource, TDestination> fallbackFunc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await mappingFunc(source, cancellationToken);
        }
        catch
        {
            return fallbackFunc(source);
        }
    }
}
