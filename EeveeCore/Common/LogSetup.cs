using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace EeveeCore.Common;

/// <summary>
///     Provides utility methods for configuring and initializing logging infrastructure.
/// </summary>
public static class LogSetup
{
    /// <summary>
    ///     Creates and configures a Serilog logger with standard settings.
    /// </summary>
    /// <param name="name">The base name used for log files.</param>
    /// <returns>A configured Serilog Logger instance.</returns>
    /// <remarks>
    ///     The logger is configured to:
    ///     - Write information level logs to the console
    ///     - Write information level logs to daily rolling files
    ///     - Override minimum log levels for specific namespaces
    ///     - Retain logs for 7 days with a maximum file size of 50MB
    /// </remarks>
    public static Logger SetupLogger(string name)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("EntityFramework", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                LogEventLevel.Information,
                "[{Timestamp:HH:mm:ss} {Level:u3}] | {Message:lj}{NewLine}{Exception}")
            .WriteTo.File($"logs/{name}-.log",
                LogEventLevel.Information,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 52428800,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}