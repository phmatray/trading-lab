using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TheAppManager.Modules;

namespace TradyStrat.Infrastructure;

public sealed class LoggingInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var logDir = Environment.GetEnvironmentVariable("LOG_DIR")
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/TradyStrat/logs");
        Directory.CreateDirectory(logDir);

        var path = Path.Combine(logDir, "tradystrat-.log");

#pragma warning disable CA1305 // Serilog sinks do not perform locale-sensitive formatting
        var logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            // Route all console output to stderr so stdout stays clean for the
            // MCP server's JSON-RPC stream. Harmless for the Blazor app and the
            // replay CLI command (which uses AnsiConsole directly for its
            // user-facing output).
            .WriteTo.Console(standardErrorFromLevel: LogEventLevel.Verbose)
            .WriteTo.File(path, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
#pragma warning restore CA1305

        services.AddLogging(b =>
        {
            b.ClearProviders();
            b.AddSerilog(logger, dispose: true);
        });
    }
}
