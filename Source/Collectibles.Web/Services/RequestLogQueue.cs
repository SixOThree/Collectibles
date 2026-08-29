using System.Threading.Channels;

using Collectibles.Domain.Constants;

namespace Collectibles.Web.Services;

/// <summary>
/// In-memory queue for request log entries.
/// Uses System.Threading.Channels for high-performance async producer/consumer pattern.
/// </summary>
public class RequestLogQueue
{
    private readonly Channel<RequestLogEntry> _queue;

    public RequestLogQueue(int capacity = ApplicationConstants.BatchProcessing.RequestLogQueueCapacity)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest, // Drop oldest entries if queue is full
        };
        _queue = Channel.CreateBounded<RequestLogEntry>(options);
    }

    /// <summary>
    /// Enqueues a request log entry for background processing.
    /// Non-blocking operation - returns immediately.
    /// </summary>
    /// <returns></returns>
    public ValueTask EnqueueAsync(RequestLogEntry entry, CancellationToken cancellationToken = default)
    {
        return _queue.Writer.TryWrite(entry)
            ? ValueTask.CompletedTask
            : _queue.Writer.WriteAsync(entry, cancellationToken);
    }

    /// <summary>
    /// Dequeues request log entries for processing by background service.
    /// </summary>
    /// <returns></returns>
    public IAsyncEnumerable<RequestLogEntry> DequeueAllAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}
