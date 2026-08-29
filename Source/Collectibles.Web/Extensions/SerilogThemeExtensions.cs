using Collectibles.Domain.Constants;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Collectibles.Web.Extensions;

public static class SerilogThemeExtensions
{
    public static AnsiConsoleTheme GetPowerShellTheme() => new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\x1b[38;5;252m",          // Light gray for normal text
        [ConsoleThemeStyle.SecondaryText] = "\x1b[38;5;246m", // Medium gray for secondary text
        [ConsoleThemeStyle.TertiaryText] = "\x1b[38;5;242m",  // Darker gray for tertiary text
        [ConsoleThemeStyle.Invalid] = "\x1b[38;5;208m",       // Orange for invalid
        [ConsoleThemeStyle.Null] = "\x1b[38;5;243m",          // Dark gray for null
        [ConsoleThemeStyle.Name] = "\x1b[38;5;75m",           // Sky blue for names (complements PS blue)
        [ConsoleThemeStyle.String] = "\x1b[38;5;72m",         // Sea green for strings
        [ConsoleThemeStyle.Number] = "\x1b[38;5;111m",        // Light blue for numbers
        [ConsoleThemeStyle.Boolean] = "\x1b[38;5;111m",       // Light blue for booleans
        [ConsoleThemeStyle.Scalar] = "\x1b[38;5;79m",         // Aqua for scalars
        [ConsoleThemeStyle.LevelVerbose] = "\x1b[38;5;244m",  // Gray for verbose
        [ConsoleThemeStyle.LevelDebug] = "\x1b[38;5;247m",    // Light gray for debug
        [ConsoleThemeStyle.LevelInformation] = "\x1b[38;5;117m", // Light blue for info (complements PS)
        [ConsoleThemeStyle.LevelWarning] = "\x1b[38;5;229m",  // Soft yellow for warnings
        [ConsoleThemeStyle.LevelError] = "\x1b[38;5;197m\x1b[48;5;238m", // Pink on dark gray for errors
        [ConsoleThemeStyle.LevelFatal] = "\x1b[38;5;15m\x1b[48;5;124m",   // White on dark red for fatal
    });

    public static void ConfigureEarlySerilogLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Filter.ByExcluding(logEvent =>
                logEvent.MessageTemplate.Text.Contains("Lucky Penny") ||
                logEvent.MessageTemplate.Text.Contains("valid license key") ||
                logEvent.MessageTemplate.Text.Contains("luckypennysoftware"))
            .WriteTo.Console(theme: GetPowerShellTheme())
            .WriteTo.File(
                path: Path.Combine(ApplicationConstants.Logging.LogDirectory, ApplicationConstants.Logging.MainLogFilePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: ApplicationConstants.TimeOperations.LogFileRetentionDays,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(ApplicationConstants.Logging.LogDirectory, ApplicationConstants.Logging.ErrorLogFilePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: ApplicationConstants.TimeOperations.LogFileRetentionDays,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
