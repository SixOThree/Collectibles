using System.Text.Json;
using Collectibles.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Collectibles.Infrastructure.Logging;

public class DatabaseLoggerSink : ILogEventSink
{
    // Use AsyncLocal to track if we're already logging to prevent recursion
    private static readonly AsyncLocal<bool> _isLogging = new AsyncLocal<bool>();

    // Track if we've verified the database table exists
    private static volatile bool _tableExistsVerified;

    private readonly IServiceProvider _serviceProvider;
    private readonly string _connectionString;

    public DatabaseLoggerSink(IServiceProvider serviceProvider, string connectionString)
    {
        _serviceProvider = serviceProvider;
        _connectionString = connectionString;
    }

    public void Emit(LogEvent logEvent)
    {
        if (_isLogging.Value)
        {
            return;
        }

        try
        {
            _isLogging.Value = true;
            ProcessLogEvent(logEvent);
        }
        catch
        {
            // Silently fail - we don't want logging failures to crash the app
            // Reset the flag if we fail after verification, in case of transient issues
            if (_tableExistsVerified)
            {
                _tableExistsVerified = false;
            }
        }
        finally
        {
            _isLogging.Value = false;
        }
    }

    private void ProcessLogEvent(LogEvent logEvent)
    {
        if (ShouldSkipLogging(logEvent))
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext == null || !EnsureDatabaseReady(dbContext))
        {
            return;
        }

        var sysLog = CreateSysLog(logEvent);
        dbContext.SysLogs.Add(sysLog);
        dbContext.SaveChanges();
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
