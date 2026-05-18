using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed;

namespace TradyStrat.Cli.Commands;

/// <summary>
/// Starts the read-only TradyStrat MCP server over stdio. Logs go to stderr
/// so they don't interfere with the JSON-RPC stream on stdout.
/// </summary>
internal sealed class McpCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var builder = Host.CreateApplicationBuilder();

        // Route all log output to stderr so it does not pollute the
        // JSON-RPC stream that the MCP server writes to stdout.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
            .AddFromAssemblyOf<ApplicationAssemblyMarker>()
            .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
                t != typeof(PriceFeedBackgroundInfrastructureModule))
            .AddFromAssemblyOf<CliAssemblyMarker>());

        using var innerHost = builder.Build();
        await innerHost.RunAsync();   // blocks until stdin EOF
        return 0;
    }
}
