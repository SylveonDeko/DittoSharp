using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace EeveeCore.Common;

/// <summary>
///     Provides utility methods for configuring and initializing logging infrastructure.
/// </summary>
public static class LogSetup
{
    /// <summary>
    ///     Creates and configures a Serilog logger with standard settings and sets it as the global logger.
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
    public static ILogger SetupLogger(string name)
    {
        var logger = Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("EntityFramework", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("LogSource", name)
            .WriteTo.Console(
                LogEventLevel.Information,
                "[{Timestamp:HH:mm:ss} {Level:u3}] | {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();
        
        return logger;
    }
}