using Serilog;
using Serilog.Events;
using TheAppManager.Modules;

namespace TradyStrat.Modules;

public sealed class LoggingModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
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
            .WriteTo.Console()
            .WriteTo.File(path, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
#pragma warning restore CA1305

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger, dispose: true);
    }
}
