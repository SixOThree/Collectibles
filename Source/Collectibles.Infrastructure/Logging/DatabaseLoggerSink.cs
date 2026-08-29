using System.Text.Json;
using System.Threading.Channels;

using Collectibles.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Collectibles.Infrastructure.Logging;

/// <summary>
/// Serilog sink that persists log events to the SysLogs table.
/// </summary>
/// <remarks>
/// Writes are queued onto a bounded channel and drained in batches by a single background
/// task. Previously <see cref="Emit"/> created a DI scope, a DbContext and a synchronous
/// SaveChanges per event, inline on whichever thread was logging - so a burst of
/// Information-level logging stalled request threads, and every persistence failure was
/// swallowed by an empty catch. The queue is bounded and drops on overflow so that logging
/// can never become a source of unbounded memory growth or back-pressure on requests.
/// </remarks>
public class DatabaseLoggerSink : ILogEventSink, IDisposable
{
    /// <summary>Maximum events held in memory awaiting persistence.</summary>
    private const int QueueCapacity = 10_000;

    /// <summary>Maximum events written per SaveChanges.</summary>
    private const int BatchSize = 200;

    // Use AsyncLocal to track if we're already logging to prevent recursion
    private static readonly AsyncLocal<bool> IsLogging = new AsyncLocal<bool>();

    // Track if we've verified the database table exists
    private static volatile bool _tableExistsVerified;

    private readonly IServiceProvider _serviceProvider;
    private readonly string _connectionString;
    private readonly Channel<SysLog> _queue;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly Task _drainTask;

    private long _droppedEventCount;

    public DatabaseLoggerSink(IServiceProvider serviceProvider, string connectionString)
    {
        _serviceProvider = serviceProvider;
        _connectionString = connectionString;

        _queue = Channel.CreateBounded<SysLog>(new BoundedChannelOptions(QueueCapacity)
        {
            // Never block the caller: logging must not apply back-pressure to a request.
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        });

        _drainTask = Task.Run(() => DrainAsync(_shutdown.Token));
    }

    public void Emit(LogEvent logEvent)
    {
        if (IsLogging.Value)
        {
            return;
        }

        try
        {
            IsLogging.Value = true;

            if (ShouldSkipLogging(logEvent))
            {
                return;
            }

            // Only the (cheap, allocation-only) mapping happens on the logging thread.
            if (!_queue.Writer.TryWrite(CreateSysLog(logEvent)))
            {
                var dropped = Interlocked.Increment(ref _droppedEventCount);
                if (dropped % QueueCapacity == 1)
                {
                    SelfLog.WriteLine("DatabaseLoggerSink queue is full; {0} event(s) dropped so far.", dropped);
                }
            }
        }
        catch (Exception ex)
        {
            // Logging failures must not crash the app, but they are reported through
            // Serilog own diagnostic channel rather than silently discarded.
            SelfLog.WriteLine("DatabaseLoggerSink failed to queue a log event: {0}", ex);
        }
        finally
        {
            IsLogging.Value = false;
        }
    }

    /// <summary>
    /// Drains the queue in batches until shutdown, then flushes what remains.
    /// </summary>
    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        var batch = new List<SysLog>(BatchSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _queue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                while (batch.Count < BatchSize && _queue.Reader.TryRead(out var queued))
                {
                    batch.Add(queued);
                }

                await WriteBatchAsync(batch, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("DatabaseLoggerSink drain loop error: {0}", ex);
            }
            finally
            {
                batch.Clear();
            }
        }

        // Final flush on shutdown so the last events are not lost.
        while (_queue.Reader.TryRead(out var remaining))
        {
            batch.Add(remaining);

            if (batch.Count >= BatchSize)
            {
                await WriteBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await WriteBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task WriteBatchAsync(List<SysLog> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

            if (dbContext == null || !EnsureDatabaseReady(dbContext))
            {
                return;
            }

            dbContext.SysLogs.AddRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Report rather than swallow, and re-probe the schema on the next batch.
            _tableExistsVerified = false;
            SelfLog.WriteLine("DatabaseLoggerSink failed to persist {0} event(s): {1}", batch.Count, ex);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _queue.Writer.TryComplete();
        _shutdown.Cancel();

        try
        {
            _drainTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Shutdown is best effort.
        }

        _shutdown.Dispose();
    }

    private static bool ShouldSkipLogging(LogEvent logEvent)
    {
        // Skip database logging for Entity Framework logs to prevent recursion
        var source = GetStringProperty(logEvent.Properties, "SourceContext");
        return source != null && source.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase);
    }

    private bool EnsureDatabaseReady(ApplicationDbContext dbContext)
    {
        if (_tableExistsVerified)
        {
            return true;
        }

        try
        {
            // First check if we can connect at all
            if (!dbContext.Database.CanConnect())
            {
                return false;
            }

            // Try to query the SysLogs table - if it doesn't exist, this will throw
            _ = dbContext.SysLogs.Take(1).ToList();
            _tableExistsVerified = true;
            return true;
        }
        catch
        {
            // Any error means we can't log yet (expected during startup before migrations)
            return false;
        }
    }

    private static SysLog CreateSysLog(LogEvent logEvent)
    {
        var properties = logEvent.Properties;

        return new SysLog
        {
            Level = MapLogLevel(logEvent.Level),
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
            StackTrace = logEvent.Exception?.StackTrace,
            Source = GetStringProperty(properties, "SourceContext"),
            MachineName = GetStringProperty(properties, "MachineName") ?? Environment.MachineName,
            ProcessName = GetStringProperty(properties, "ProcessName") ?? System.Diagnostics.Process.GetCurrentProcess().ProcessName,
            ThreadId = GetIntProperty(properties, "ThreadId") ?? Environment.CurrentManagedThreadId,
            Properties = SerializeProperties(properties),
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Category = GetStringProperty(properties, "Category"),
            CorrelationId = GetStringProperty(properties, "CorrelationId"),
            UserId = GetStringProperty(properties, "UserId"),
            RequestPath = GetStringProperty(properties, "RequestPath"),
            RequestMethod = GetStringProperty(properties, "RequestMethod"),
        };
    }

    private static string? GetStringProperty(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string key)
    {
        return properties.TryGetValue(key, out var value) ? value.ToString().Trim('"') : null;
    }

    private static int? GetIntProperty(IReadOnlyDictionary<string, LogEventPropertyValue> properties, string key)
    {
        if (properties.TryGetValue(key, out var value) && int.TryParse(value.ToString(), out var result))
        {
            return result;
        }

        return null;
    }

    private static Domain.Entities.LogLevel MapLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => Domain.Entities.LogLevel.Trace,
            LogEventLevel.Debug => Domain.Entities.LogLevel.Debug,
            LogEventLevel.Information => Domain.Entities.LogLevel.Information,
            LogEventLevel.Warning => Domain.Entities.LogLevel.Warning,
            LogEventLevel.Error => Domain.Entities.LogLevel.Error,
            LogEventLevel.Fatal => Domain.Entities.LogLevel.Critical,
            _ => Domain.Entities.LogLevel.Trace,
        };
    }

    private static string? SerializeProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties)
    {
        if (properties == null || properties.Count == 0)
        {
            return null;
        }

        var dict = new Dictionary<string, string>();
        foreach (var prop in properties)
        {
            // Skip properties we're storing in dedicated columns
            if (prop.Key == "SourceContext" || prop.Key == "MachineName" ||
                prop.Key == "ThreadId" || prop.Key == "Category" ||
                prop.Key == "CorrelationId" || prop.Key == "UserId" ||
                prop.Key == "RequestPath" || prop.Key == "RequestMethod" ||
                prop.Key == "ProcessName")
            {
                continue;
            }

            var rendered = prop.Value.ToString();

            // Serilog's ScalarValue.ToString() wraps strings in quotes - strip them
            if (rendered.Length >= 2 && rendered[0] == '"' && rendered[^1] == '"')
            {
                rendered = rendered[1..^1];
            }

            dict[prop.Key] = rendered;
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict) : null;
    }
}
