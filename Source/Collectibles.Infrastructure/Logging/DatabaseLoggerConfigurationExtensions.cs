using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Collectibles.Infrastructure.Logging;

public static class DatabaseLoggerConfigurationExtensions
{
    public static LoggerConfiguration Database(
        this LoggerSinkConfiguration loggerConfiguration,
        IServiceProvider serviceProvider,
        string connectionString,
        LogEventLevel restrictedToMinimumLevel = LogEventLevel.Information)
    {
        return loggerConfiguration.Sink(
            new DatabaseLoggerSink(serviceProvider, connectionString),
            restrictedToMinimumLevel);
    }
}
